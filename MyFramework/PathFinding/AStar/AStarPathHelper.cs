using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MyFramework.Core.Singleton;
using MyFramework.Debuger;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyFramework.PathFinding.AStar
{
    /// <summary>
    ///     路径预计算信息结构体。
    ///     包含多组起点+终点，以及批量寻路后的结果字典（key=endPos, value=从各 start 到该 endPos 的路径）。
    /// </summary>
    [Serializable]
    public struct PathInfo
    {
        /// <summary>起点世界坐标列表</summary>
        public List<Vector3> startPos;

        /// <summary>终点世界坐标列表</summary>
        public List<Vector3> endPos;

        /// <summary>
        ///     寻路结果字典。
        ///     Key = endPos（世界坐标），Value = 从各 startPos 到该 endPos 的路径点列表。
        ///     Value 为 null 或 count==0 表示该终点不可达。
        /// </summary>
        public Dictionary<Vector3, List<Vector3>> paths;

        /// <summary>
        ///     遍历 paths 字典，判断是否存在至少一条可行路径（非 null 且 count > 0）。
        /// </summary>
        public bool HavePath()
        {
            if (paths == null) return false;
            foreach (var kvp in paths)
            {
                if (kvp.Value != null && kvp.Value.Count > 0)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    ///     批量路径预计算单例。
    ///     继承 SingletonAutoMono，对外提供同步 A* 批量寻路能力。
    ///     适用于"开战前预计算所有单位的攻击路径"等离线预计算场景。
    ///     内部复用 FindPathJob + WorldToGrid 坐标转换，不重复实现 A* 算法。
    /// </summary>
    public class AStarPathHelper : SingletonAutoMono<AStarPathHelper>
    {
        // ====================================================================
        // Key 管理
        // ====================================================================

        private static int _nextKey = 1;

        /// <summary>当前下一个可用 Key（只读，调用 AddPathInfo 后自增）</summary>
        public static int Key => _nextKey;

        // ====================================================================
        // 私有数据
        // ====================================================================

        /// <summary>PathInfo 字典（Key = 自增 id，Value = PathInfo）</summary>
        private readonly Dictionary<int, PathInfo> _pathInfos = new Dictionary<int, PathInfo>();

        // ====================================================================
        // 公共方法
        // ====================================================================

        /// <summary>
        ///     添加一个 PathInfo 到字典中，返回自增 Key（Key++）。
        /// </summary>
        public int AddPathInfo(PathInfo info)
        {
            var key = _nextKey++;
            _pathInfos[key] = info;
            return key;
        }

        /// <summary>
        ///     根据 id 从字典中取出 PathInfo 执行批量寻路，结果回填到 PathInfo.paths 中。
        /// </summary>
        /// <param name="id">AddPathInfo 时返回的自增 Key</param>
        /// <param name="extraBlockPos">额外阻塞点（可选），传入后与全局 blockedList 合并传入 FindPathJob。同一位置已存在时输出警告日志，不阻断运行。</param>
        /// <returns>HavePath() 的结果（是否存在至少一条可行路径）</returns>
        public bool CalculatePath(int id,[CanBeNull] Action callBack ,List<int2> extraBlockPos = null)
        {
            if (!_pathInfos.TryGetValue(id, out var info)) return false;

            var result = CalculatePath(ref info, callBack,extraBlockPos);

            // 回写（struct 是值拷贝）
            _pathInfos[id] = info;

            return result;
        }

        /// <summary>
        ///     直接对传入的 PathInfo 执行批量寻路（不存入字典），结果回填到 info.paths 中。
        ///     startPos/endPos 数量一致：第 i 个 startPos → 第 i 个 endPos。
        /// </summary>
        /// <param name="info">PathInfo 引用（结果回填到 info.paths）</param>
        /// <param name="extraBlockPos">额外阻塞点（可选），传入后与全局 blockedList 合并传入 FindPathJob。同一位置已存在时输出警告日志，不阻断运行。</param>
        /// <param name="recordPath">是否记录路径结果到 info.paths</param>
        /// <param name="requireAllPaths">true=全部起终点对都通才算成功；false=至少一条通即可（与 HavePath 一致）</param>
        /// <returns>本次寻路是否满足 requireAllPaths / 至少一条可达</returns>
        public bool CalculatePath(ref PathInfo info, [CanBeNull] Action callBack = null,
            List<int2> extraBlockPos = null, bool recordPath = true, bool requireAllPaths = false)
        {
            if (info.startPos == null || info.startPos.Count == 0) return false;
            if (info.endPos == null || info.endPos.Count == 0) return false;

            var manager = AStarManager.Instance;
            if (manager == null)
            {
                DebugLogger.LogWarning("[AStarPathHelper] AStarManager.Instance 为空，无法计算路径。");
                return false;
            }

            var count = info.startPos.Count;

            // === 快照 AStarManager 配置 ===
            var snapGridSize = manager.gridSize;
            var snapCellSize = manager.cellSize;
            var snapOrigin = manager.origin;
            var snapStraight = manager.moveStraightCost;
            var snapDiagonal = manager.moveDiagonalCost;
            var snap8Direction = manager.is8Direction;
            var snapMaxPath = manager.maxPathLength;

            // === Step A: 世界坐标 → 网格坐标 ===
            var worldStarts = new NativeArray<float3>(count, Allocator.TempJob);
            var worldEnds = new NativeArray<float3>(count, Allocator.TempJob);
            var gridStarts = new NativeArray<int2>(count, Allocator.TempJob);
            var gridEnds = new NativeArray<int2>(count, Allocator.TempJob);

            for (var i = 0; i < count; i++)
            {
                worldStarts[i] = info.startPos[i];
                worldEnds[i] = info.endPos[i];
            }

            var toGridJob = new WorldToGridHelperJob
            {
                worldStarts = worldStarts,
                worldEnds = worldEnds,
                gridStarts = gridStarts,
                gridEnds = gridEnds,
                cellSize = snapCellSize,
                origin = snapOrigin,
            };
            toGridJob.ScheduleParallel(count, 64, new JobHandle()).Complete();

            // === Step B: A* 批量寻路 ===
            List<int2> blockedList = new List<int2>(manager.GetBlockedPositions());

            // 合并额外阻塞点，检测重复并输出警告，不阻断
            if (extraBlockPos is { Count: > 0 })
            {
                foreach (var bp in extraBlockPos)
                {
                    if (blockedList.Contains(bp))
                        Debug.LogWarning($"[AStarPathHelper] extraBlockPos 中存在已阻塞点 {bp}，跳过重复添加。");
                    else
                        blockedList.Add(bp);
                }
            }

            var blockedArray = new NativeArray<int2>(blockedList.ToArray(), Allocator.TempJob);

            var pathFoundResults = new NativeArray<bool>(count, Allocator.TempJob);
            var worldPathCounts = new NativeArray<int>(count, Allocator.TempJob);
            var worldPaths = new NativeArray<float3>(count * snapMaxPath, Allocator.TempJob);

            // cellOffset 固定 (0f, 0f)（方格左下角）
            var cellOffsets = new NativeArray<float2>(count, Allocator.TempJob);
            for (var i = 0; i < count; i++)
                cellOffsets[i] = new float2(0f, 0f);

            var findPathJob = new FindPathJob
            {
                moveStraightCost = snapStraight,
                moveDiagonalCost = snapDiagonal,
                gridSize = snapGridSize,
                use8Direction = snap8Direction,
                blockedPositions = blockedArray,
                cellSize = snapCellSize,
                origin = snapOrigin,
                maxPathLength = snapMaxPath,
                startPositions = gridStarts,
                endPositions = gridEnds,
                cellOffsets = cellOffsets,
                pathFoundResults = pathFoundResults,
                worldPathCounts = worldPathCounts,
                worldPaths = worldPaths,
            };
            findPathJob.ScheduleParallel(count, 64, new JobHandle()).Complete();

            // === Step C: 回填 PathInfo.paths（仅 recordPath=true）===
            if (recordPath)
            {
                if (info.paths == null)
                    info.paths = new SerializedDictionary<Vector3, List<Vector3>>();
                else
                    info.paths.Clear();

                for (var i = 0; i < count; i++)
                {
                    var end = info.endPos[i];

                    if (!info.paths.TryGetValue(end, out var pathList))
                    {
                        pathList = new List<Vector3>();
                        info.paths[end] = pathList;
                    }
                    else
                    {
                        pathList.Clear();
                    }

                    if (pathFoundResults[i])
                    {
                        var baseIdx = i * snapMaxPath;
                        var cnt = worldPathCounts[i];
                        for (var j = 0; j < cnt; j++)
                            pathList.Add(worldPaths[baseIdx + j]);
                    }
                }
            }

            // === 返回值必须基于本次 pathFoundResults，不能在 recordPath=false 时读旧 paths ===
            bool success;
            if (requireAllPaths)
            {
                success = true;
                for (var i = 0; i < count; i++)
                {
                    if (!pathFoundResults[i])
                    {
                        success = false;
                        break;
                    }
                }
            }
            else
            {
                success = false;
                for (var i = 0; i < count; i++)
                {
                    if (pathFoundResults[i])
                    {
                        success = true;
                        break;
                    }
                }
            }

            // === 清理 ===
            pathFoundResults.Dispose();
            worldPathCounts.Dispose();
            worldPaths.Dispose();
            cellOffsets.Dispose();
            blockedArray.Dispose();
            worldStarts.Dispose();
            worldEnds.Dispose();
            gridStarts.Dispose();
            gridEnds.Dispose();

            callBack?.Invoke();
            return success;
        }

        // ====================================================================
        // WorldToGridHelperJob —— 与 AStarManager.WorldToGridJob 逻辑一致
        //   这里独立定义一份以避免依赖 AStarManager 的 private nested struct
        // ====================================================================

        [Unity.Burst.BurstCompile]
        private struct WorldToGridHelperJob : IJobFor
        {
            public NativeArray<float3> worldStarts;
            public NativeArray<float3> worldEnds;
            public NativeArray<int2> gridStarts;
            public NativeArray<int2> gridEnds;
            public float cellSize;
            public float3 origin;

            public void Execute(int index)
            {
                gridStarts[index] = ToGrid(worldStarts[index]);
                gridEnds[index] = ToGrid(worldEnds[index]);
            }

            private int2 ToGrid(float3 p)
            {
                return new int2(
                    (int)math.floor((p.x - origin.x) / cellSize ),
                    (int)math.floor((p.z - origin.z) / cellSize ));
            }
        }
    }
}
