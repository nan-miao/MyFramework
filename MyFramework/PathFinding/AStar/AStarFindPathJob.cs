using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MyFramework.PathFinding.AStar
{
    /// <summary>
    ///     A* 寻路 Job（Burst 编译，IJobFor 多线程并行，Unity 2022.3 / Collections 1.x 兼容）。
    ///     ★ 从 IJob 改造为 IJobFor，支持 ScheduleParallel 多线程并发。
    ///     ★ 所有 NativeList&lt;T&gt; 已替换为 NativeArray&lt;T&gt; + 手动计数。
    /// </summary>
    [BurstCompile]
    public struct FindPathJob : IJobFor
    {
        // ===== 共享输入（所有请求复用） =====
        public int moveStraightCost;
        public int moveDiagonalCost;
        public int2 gridSize;

        /// <summary>true=8方向（含对角线），false=4方向（仅上下左右）</summary>
        public bool use8Direction;

        [ReadOnly] public NativeArray<int2> blockedPositions;
        public float cellSize;
        public float3 origin;

        /// <summary>单条路径最大节点数（用于 worldPaths 二维索引）</summary>
        public int maxPathLength;

        // ===== 并行输入（每个请求一项） =====
        [ReadOnly] public NativeArray<int2> startPositions;
        [ReadOnly] public NativeArray<int2> endPositions;

        /// <summary>每个请求的方格内偏移（per-request，x=横向→世界X，y=纵向→世界Z；0.0=左下角，0.5=中心，1.0=右上角）</summary>
        [ReadOnly] public NativeArray<float2> cellOffsets;

        // ===== 并行输出（每个请求一项） =====
        /// <summary>pathFoundResults[i] 表示第 i 个请求是否找到路径</summary>
        public NativeArray<bool> pathFoundResults;

        /// <summary>worldPathCounts[i] 表示第 i 个请求的路径节点数</summary>
        public NativeArray<int> worldPathCounts;

        /// <summary>二维展平：worldPaths[i * maxPathLength + j] = 第 i 个请求的第 j 个路径点</summary>
        /// <remarks>禁用并行限制：每个 index 写入自己独占的 [index*maxPathLength .. (index+1)*maxPathLength-1] 段，不与其他线程重叠。</remarks>
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> worldPaths;

        public void Execute(int index)
        {
            pathFoundResults[index] = false;
            worldPathCounts[index] = 0;

            var startPos = startPositions[index];
            var endPos = endPositions[index];

            var totalNodes = gridSize.x * gridSize.y;
            var pathNodeArray = new NativeArray<PathNode>(totalNodes, Allocator.Temp);

            // 初始化所有节点
            for (var x = 0; x < gridSize.x; x++)
            for (var y = 0; y < gridSize.y; y++)
            {
                var node = new PathNode
                {
                    x = x,
                    y = y,
                    index = CalcIndex(x, y, gridSize.x),
                    gCost = int.MaxValue,
                    hCost = CalcDist(new int2(x, y), endPos),
                    isWalkable = true,
                    cameFromNodeIndex = -1,
                };
                node.CalcFCost();
                pathNodeArray[node.index] = node;
            }

            // 设置不可行走节点
            for (var i = 0; i < blockedPositions.Length; i++)
            {
                var bp = blockedPositions[i];
                if (IsInside(bp, gridSize))
                {
                    var idx = CalcIndex(bp.x, bp.y, gridSize.x);
                    var node = pathNodeArray[idx];
                    node.isWalkable = false;
                    pathNodeArray[idx] = node;
                }
            }

            // 邻居偏移（根据 use8Direction 决定 4 向或 8 向）
            int neighbourCount;
            NativeArray<int2> neighbours;

            if (use8Direction)
            {
                neighbourCount = 8;
                neighbours = new NativeArray<int2>(8, Allocator.Temp);
                neighbours[0] = new int2(-1, 0);
                neighbours[1] = new int2(1, 0);
                neighbours[2] = new int2(0, 1);
                neighbours[3] = new int2(0, -1);
                neighbours[4] = new int2(-1, -1);
                neighbours[5] = new int2(-1, 1);
                neighbours[6] = new int2(1, -1);
                neighbours[7] = new int2(1, 1);
            }
            else
            {
                neighbourCount = 4;
                neighbours = new NativeArray<int2>(4, Allocator.Temp);
                neighbours[0] = new int2(-1, 0);
                neighbours[1] = new int2(1, 0);
                neighbours[2] = new int2(0, 1);
                neighbours[3] = new int2(0, -1);
            }

            var endIdx = CalcIndex(endPos.x, endPos.y, gridSize.x);
            var startIdx = CalcIndex(startPos.x, startPos.y, gridSize.x);
            var startNode = pathNodeArray[startIdx];

            // 起点不可行走 → 直接返回
            if (!startNode.isWalkable)
            {
                pathNodeArray.Dispose();
                neighbours.Dispose();
                return;
            }

            startNode.gCost = 0;
            startNode.CalcFCost();
            pathNodeArray[startIdx] = startNode;

            // openList / closedList: NativeArray + 手动计数
            var openList = new NativeArray<int>(totalNodes, Allocator.Temp);
            var openCount = 0;
            var closedList = new NativeArray<int>(totalNodes, Allocator.Temp);
            var closedCount = 0;

            openList[openCount++] = startIdx;

            // A* 主循环
            while (openCount > 0)
            {
                var curIdx = GetLowestF(openList, openCount, pathNodeArray);
                var curNode = pathNodeArray[curIdx];

                if (curIdx == endIdx) break;

                // RemoveAtSwapBack(curIdx)
                for (var i = 0; i < openCount; i++)
                {
                    if (openList[i] == curIdx)
                    {
                        openList[i] = openList[--openCount];
                        break;
                    }
                }

                closedList[closedCount++] = curIdx;

                for (var i = 0; i < neighbourCount; i++)
                {
                    var nb = neighbours[i];
                    var nbPos = new int2(curNode.x + nb.x, curNode.y + nb.y);
                    if (!IsInside(nbPos, gridSize)) continue;

                    var nbIdx = CalcIndex(nbPos.x, nbPos.y, gridSize.x);

                    if (Contains(closedList, closedCount, nbIdx)) continue;

                    var nbNode = pathNodeArray[nbIdx];
                    if (!nbNode.isWalkable) continue;

                    var tentativeG = curNode.gCost +
                                     CalcDist(new int2(curNode.x, curNode.y), nbPos);
                    if (tentativeG < nbNode.gCost)
                    {
                        nbNode.cameFromNodeIndex = curIdx;
                        nbNode.gCost = tentativeG;
                        nbNode.CalcFCost();
                        pathNodeArray[nbIdx] = nbNode;

                        if (!Contains(openList, openCount, nbIdx))
                            openList[openCount++] = nbIdx;
                    }
                }
            }

            // 结果处理
            var endNode = pathNodeArray[endIdx];
            if (endNode.cameFromNodeIndex == -1)
            {
                pathFoundResults[index] = false;
            }
            else
            {
                var pathTemp = new NativeArray<int2>(totalNodes, Allocator.Temp);
                var pathCount = 0;

                pathTemp[pathCount++] = new int2(endNode.x, endNode.y);

                var cur = endNode;
                while (cur.cameFromNodeIndex != -1)
                {
                    var prev = pathNodeArray[cur.cameFromNodeIndex];
                    pathTemp[pathCount++] = new int2(prev.x, prev.y);
                    cur = prev;
                }

                pathFoundResults[index] = true;
                var pathLen = math.min(pathCount, maxPathLength);
                worldPathCounts[index] = pathLen;

                // 写入二维展平数组：worldPaths[index * maxPathLength + j]
                var baseIdx = index * maxPathLength;
                // 反转写入：pathTemp[0]=终点, pathTemp[pathLen-1]=起点 → 输出 start→end
                for (var i = 0; i < pathLen; i++)
                {
                    var gp = pathTemp[pathLen - 1 - i];
                    var ni = CalcIndex(gp.x, gp.y, gridSize.x);
                    var node = pathNodeArray[ni];
                    worldPaths[baseIdx + i] = GetWorldPos(node, cellSize, origin, cellOffsets[index]);
                }

                pathTemp.Dispose();
            }

            pathNodeArray.Dispose();
            neighbours.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }

        // === 辅助方法 ===

        private static int CalcIndex(int x, int y, int gridWidth) => x + y * gridWidth;

        private int CalcDist(int2 a, int2 b)
        {
            var xDis = math.abs(b.x - a.x);
            var yDis = math.abs(b.y - a.y);
            var remain = math.abs(xDis - yDis);
            return moveDiagonalCost * math.min(xDis, yDis) + moveStraightCost * remain;
        }

        private static int GetLowestF(NativeArray<int> openList, int openCount,
            NativeArray<PathNode> pathNodeArray)
        {
            var lowest = pathNodeArray[openList[0]];
            for (var i = 1; i < openCount; i++)
            {
                var test = pathNodeArray[openList[i]];
                if (test.fCost < lowest.fCost)
                {
                    lowest = test;
                }
                else if (test.fCost == lowest.fCost)
                {
                    if (test.hCost < lowest.hCost)
                        lowest = test;
                }
            }

            return lowest.index;
        }

        private static bool Contains(NativeArray<int> arr, int count, int value)
        {
            for (var i = 0; i < count; i++)
                if (arr[i] == value)
                    return true;
            return false;
        }

        private static bool IsInside(int2 pos, int2 size)
        {
            return pos.x >= 0 && pos.x < size.x
                              && pos.y >= 0 && pos.y < size.y;
        }

        private static float3 GetWorldPos(PathNode node, float cs, float3 o, float2 offset)
        {
            // 方格内偏移：offset=0.0→左下角，offset=0.5→中心，offset=1.0→右上角
            // node.x → 世界 X，node.y → 世界 Z
            return o + new float3((node.x + offset.x) * cs, 0, (node.y + offset.y) * cs);
        }
    }

    /// <summary>
    ///     寻路节点。与 Pathfinding.PathNode 功能一致，独立定义便于外部 Job 使用。
    /// </summary>
    public struct PathNode
    {
        public int x;
        public int y;
        public int index;
        public int gCost;
        public int hCost;
        public int fCost;
        public bool isWalkable;
        public int cameFromNodeIndex;

        public void CalcFCost()
        {
            fCost = gCost + hCost;
        }
    }
}
