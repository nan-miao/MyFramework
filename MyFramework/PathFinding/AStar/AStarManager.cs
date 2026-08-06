using System;
using System.Collections.Generic;
using System.Diagnostics;
using MyFramework.Broadcast;
using MyFramework.Core;
using MyFramework.Core.Singleton;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MyFramework.PathFinding.AStar
{
    [Serializable]
    public struct AStarManagerConfig
    {
        [Title("Grid Config")]
        [LabelText("网格范围")] public int2 gridSize;
        public float cellSize;
        [LabelText("原点")] public float3 origin;

        [Title("Processing")]
        [LabelText("计算频率/帧")] public int frameInterval;
        [LabelText("每次处理上限")] public int maxBatchSize;

        [Title("Path Costs")]
        [LabelText("直线移动Cost")] public int moveStraightCost;
        [LabelText("对角线移动Cost")] public int moveDiagonalCost;

        [Title("Search Mode")]
        [LabelText("八向寻路")] public bool is8Direction;
    }

    /// <summary>
    ///     AStar 寻路管理器（Unity 2022.3 / Collections 1.x 兼容）。
    ///     继承 SingletonAutoMono 实现自动挂载的单例，通过 MonoManager 每隔 N 帧批量处理注册的寻路请求。
    /// </summary>
    public class AStarManager : SingletonAutoMono<AStarManager>
    {
        // ====================================================================
        // ★ 外部可动态修改的关键配置
        // ====================================================================

        [Header("Grid Config")]
        public int2 gridSize = new int2(100, 100);
        public float cellSize = 1f;
        public float3 origin = float3.zero;

        [Header("Processing")]
        public int frameInterval = 20;
        [LabelText("每次处理上限")]
        public int maxBatchSize = 10;

        [Header("Path Costs")]
        public int moveStraightCost = 10;
        public int moveDiagonalCost = 14;

        [Header("Search Mode")]
        [LabelText("八向寻路")]
        public bool is8Direction = true;

        /// <summary>单条路径最大节点数（用作 NativeArray 预分配长度）</summary>
        public int maxPathLength = 1024;

        public void SetConfig(AStarManagerConfig config)
        {
            gridSize = config.gridSize;
            cellSize = config.cellSize;
            origin = config.origin;
            frameInterval = config.frameInterval;
            maxBatchSize = config.maxBatchSize;
            moveStraightCost = config.moveStraightCost;
            moveDiagonalCost = config.moveDiagonalCost;
            is8Direction = config.is8Direction;
        }

        // ====================================================================
        // 私有数据
        // ====================================================================

        /// <summary>不可行走的网格坐标列表（托管 List，Collections 1.x 兼容）</summary>
        [SerializeField]private List<int2> _blockedGridPositions = new List<int2>();

        /// <summary>注册的寻路对象列表</summary>
        private readonly List<AStarMono> _registeredObjects = new List<AStarMono>();

        /// <summary>批次计时器</summary>
        private readonly Stopwatch _batchStopwatch = new Stopwatch();

        /// <summary>累计已完成寻路对象数</summary>
        private int _totalCompletedCount;

        /// <summary>累计已执行批次数</summary>
        private int _totalBatchCount;

        private bool _updateListenerRegistered;
        private int _currentRegisteredFrequency;

        // ====================================================================
        // Unity 生命周期
        // ====================================================================

        protected override void OnStart()
        {
            RegisterUpdateListener();
        }

        private void OnDestroy()
        {
            RemoveUpdateListener();
            _registeredObjects.Clear();
            _blockedGridPositions.Clear();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _updateListenerRegistered && _currentRegisteredFrequency != frameInterval)
            {
                RemoveUpdateListener();
                RegisterUpdateListener();
            }
        }

        private void RegisterUpdateListener()
        {
            if (_updateListenerRegistered) return;
            if (MonoManager.Instance == null)
            {
                Debug.LogWarning("[AStarManager] MonoManager.Instance 为空，延迟注册 Update 回调。");
                return;
            }
            MonoManager.Instance?.AddUpdateListener(ProcessBatch, frequency: frameInterval);
            _updateListenerRegistered = true;
            _currentRegisteredFrequency = frameInterval;
        }

        private void RemoveUpdateListener()
        {
            if (!_updateListenerRegistered) return;
            MonoManager.Instance?.RemoveUpdateListener(ProcessBatch);
            _updateListenerRegistered = false;
        }

        // ====================================================================
        // 公共方法
        // ====================================================================

        public void Register(AStarMono obj)
        {
            if (obj == null) return;
            if (!_updateListenerRegistered) RegisterUpdateListener();
            _registeredObjects.Add(obj);
        }

        public void Unregister(AStarMono obj)
        {
            _registeredObjects.Remove(obj);
        }

        public void SetBlockedPositions(List<int2> positions)
        {
            _blockedGridPositions.Clear();
            if (positions != null)
                _blockedGridPositions.AddRange(positions);
        }

        public void AddBlockedPosition(int2 pos)
        {
            if (!_blockedGridPositions.Contains(pos))
                _blockedGridPositions.Add(pos);
        }

        public void RemoveBlockedPosition(int2 pos)
        {
            _blockedGridPositions.Remove(pos);
        }

        /// <summary>获取当前不可行走网格坐标的副本。</summary>
        public List<int2> GetBlockedPositions()
        {
            return new List<int2>(_blockedGridPositions);
        }

        public void SetGridSize(int2 size) => gridSize = size;
        public int RegisteredCount => _registeredObjects.Count;

        // ====================================================================
        // 批量处理（由 MonoManager 回调触发）
        // ====================================================================

        private void ProcessBatch()
        {
            // 1. 收集所有需要寻路的对象
            var toProcess = new List<AStarMono>();
            for (var i = _registeredObjects.Count - 1; i >= 0; i--)
            {
                var obj = _registeredObjects[i];
                if (obj == null)
                {
                    _registeredObjects.RemoveAt(i);
                    continue;
                }
                if (obj.needAStar)
                    toProcess.Add(obj);
            }

            var totalPending = toProcess.Count;
            if (totalPending == 0) return;

            // 2. 限制每次处理数量
            var count = Mathf.Min(totalPending, maxBatchSize);

            _batchStopwatch.Restart();

            // 3. 世界坐标 → 网格坐标 输入数组（按实际处理数量分配）
            var worldCurrents = new NativeArray<float3>(count, Allocator.TempJob);
            var worldEnds = new NativeArray<float3>(count, Allocator.TempJob);
            var gridStarts = new NativeArray<int2>(count, Allocator.TempJob);
            var gridEnds = new NativeArray<int2>(count, Allocator.TempJob);

            for (var i = 0; i < count; i++)
            {
                worldCurrents[i] = toProcess[i].currentPos;
                worldEnds[i] = toProcess[i].endPos;
            }

            // Step A: 世界坐标 → 网格坐标 批量转换 Job
            var worldToGridJob = new WorldToGridJob
            {
                worldCurrents = worldCurrents,
                worldEnds = worldEnds,
                gridStarts = gridStarts,
                gridEnds = gridEnds,
                cellSize = cellSize,
                origin = origin,
            };
            worldToGridJob.ScheduleParallel(count, 64, new JobHandle()).Complete();

            // 4. 保存配置快照（防止 Job 执行期间被外部修改）
            var snapGridSize = gridSize;
            var snapStraight = moveStraightCost;
            var snapDiagonal = moveDiagonalCost;
            var snap8Direction = is8Direction;
            var snapCellSize = cellSize;
            var snapOrigin = origin;
            var snapMaxPath = maxPathLength;

            // blocked positions 从托管 List 复制为 NativeArray（一次性，复用）
            var blockedArray = new NativeArray<int2>(
                _blockedGridPositions.ToArray(), Allocator.TempJob);

            // ★ 并行输出数组：一次分配，多线程并发写入
            var pathFoundResults = new NativeArray<bool>(count, Allocator.TempJob);
            var worldPathCounts = new NativeArray<int>(count, Allocator.TempJob);
            var worldPaths = new NativeArray<float3>(count * snapMaxPath, Allocator.TempJob);

            // per-request cellOffset（由 AStarMonoManager 批量收集）
            var cellOffsets = new NativeArray<float2>(count, Allocator.TempJob);
            AStarMonoManager.Instance.CollectCellOffsets(toProcess, cellOffsets);

            // Step B: IJobFor 多线程并行寻路（一次 ScheduleParallel）
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

            // Step C: 回写结果到各 AStarMono（主线程）
            for (var i = 0; i < count; i++)
            {
                var obj = toProcess[i];

                obj.needAStar = false;
                obj.findPath = pathFoundResults[i];
                obj.path.Clear();
                if (obj.findPath)
                {
                    var baseIdx = i * snapMaxPath;
                    var cnt = worldPathCounts[i];
                    for (var j = 0; j < cnt; j++)
                        obj.path.Add(worldPaths[baseIdx + j]);
                }

                // 从注册列表中移除已处理对象
                _registeredObjects.Remove(obj);
            }

            pathFoundResults.Dispose();
            worldPathCounts.Dispose();
            worldPaths.Dispose();
            cellOffsets.Dispose();
            blockedArray.Dispose();
            worldCurrents.Dispose();
            worldEnds.Dispose();
            gridStarts.Dispose();
            gridEnds.Dispose();

            // 统计
            _totalCompletedCount += count;
            _totalBatchCount++;
            _batchStopwatch.Stop();
            Debug.Log($"[AStarManager] 批次 #{_totalBatchCount} 完成 (ScheduleParallel): 处理 {count} 个对象 (队列 {totalPending}), 耗时 {_batchStopwatch.Elapsed.TotalMilliseconds:F2}ms, 累计 {_totalCompletedCount} 个");

            // Step D: 广播
            BroadcastCenter.Broadcast(BroadcastEventType.FinishAStar);
        }

        // ====================================================================
        // WorldToGridJob —— 世界坐标 → 网格坐标 批量转换
        // ====================================================================

        [BurstCompile]
        private struct WorldToGridJob : IJobFor
        {
            public NativeArray<float3> worldCurrents;
            public NativeArray<float3> worldEnds;
            public NativeArray<int2> gridStarts;
            public NativeArray<int2> gridEnds;
            public float cellSize;
            public float3 origin;

            public void Execute(int index)
            {
                gridStarts[index] = ToGrid(worldCurrents[index]);
                gridEnds[index] = ToGrid(worldEnds[index]);
            }

            private int2 ToGrid(float3 p)
            {
                return new int2(
                    (int)math.floor((p.x - origin.x) / cellSize),
                    (int)math.floor((p.z - origin.z) / cellSize));
            }
        }
    }
}
