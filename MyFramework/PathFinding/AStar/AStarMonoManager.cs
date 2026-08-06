using System.Collections.Generic;
using System.Diagnostics;
using MyFramework.Core;
using MyFramework.Core.Singleton;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MyFramework.PathFinding.AStar
{
    /// <summary>
    ///     AStarMono 移动管理器。
    ///     管理所有激活的 AStarMono 实例，通过 IJobFor 多线程并行驱动物体沿拐点路径移动。
    ///     继承 SingletonAutoMono 实现自动挂载单例。
    /// </summary>
    public class AStarMonoManager : SingletonAutoMono<AStarMonoManager>
    {
        [Header("Processing")]
        /// <summary>移动更新频率（每 N 帧一次）</summary>
        public int frameInterval = 1;

        /// <summary>到达拐点的距离阈值</summary>
        public float arriveThreshold = 0.1f;

        // ====================================================================
        // 私有数据
        // ====================================================================

        private readonly List<AStarMono> _registeredObjects = new List<AStarMono>();

        /// <summary>每个实体当前正在前往的拐点索引</summary>
        private readonly List<int> _waypointIndices = new List<int>();

        /// <summary>每个实体的拐点缓存（用于检测路径变化）</summary>
        private readonly List<int> _cachedWaypointHash = new List<int>();
        
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
            _waypointIndices.Clear();
            _cachedWaypointHash.Clear();
        }

        private void RegisterUpdateListener()
        {
            if (_updateListenerRegistered) return;
            if (MonoManager.Instance == null)
            {
                Debug.LogWarning("[AStarMonoManager] MonoManager.Instance 为空。");
                return;
            }
            // order=1 确保在 AStarManager(order=0) 之后执行
            MonoManager.Instance?.AddUpdateListener(ProcessMovement, order: 1, frequency: frameInterval);
            _updateListenerRegistered = true;
            _currentRegisteredFrequency = frameInterval;
        }

        private void RemoveUpdateListener()
        {
            if (!_updateListenerRegistered) return;
            MonoManager.Instance?.RemoveUpdateListener(ProcessMovement);
            _updateListenerRegistered = false;
        }

        // ====================================================================
        // 公共方法
        // ====================================================================

        public void Register(AStarMono obj)
        {
            if (obj == null) return;
            if (!_registeredObjects.Contains(obj))
            {
                _registeredObjects.Add(obj);
                _waypointIndices.Add(0);
                _cachedWaypointHash.Add(0);
            }
        }

        public void Unregister(AStarMono obj)
        {
            var idx = _registeredObjects.IndexOf(obj);
            if (idx < 0) return;
            _registeredObjects.RemoveAt(idx);
            _waypointIndices.RemoveAt(idx);
            _cachedWaypointHash.RemoveAt(idx);
        }

        public int RegisteredCount => _registeredObjects.Count;

        /// <summary>
        ///     批量收集指定 AStarMono 的 cellOffset 值到 NativeArray。
        ///     由 AStarManager 在寻路批次中调用。
        /// </summary>
        public void CollectCellOffsets(List<AStarMono> targets, NativeArray<float2> output)
        {
            for (var i = 0; i < targets.Count; i++)
                output[i] = targets[i].cellOffset;
        }

        // ====================================================================
        // 移动处理（由 MonoManager 回调触发）
        // ====================================================================

        private void ProcessMovement()
        {
            // 清理 null 引用
            for (var i = _registeredObjects.Count - 1; i >= 0; i--)
            {
                if (_registeredObjects[i] == null)
                {
                    _registeredObjects.RemoveAt(i);
                    _waypointIndices.RemoveAt(i);
                    _cachedWaypointHash.RemoveAt(i);
                }
            }

            var count = _registeredObjects.Count;
            if (count == 0) return;

            // 收集中需要移动的实体（path 非空）并检测路径是否变更
            var activeIndices = new List<int>(count);
            var activeWaypoints = new List<List<Vector3>>(count);
            for (var i = 0; i < count; i++)
            {
                var obj = _registeredObjects[i];
                var path = obj.path;
                if (path != null && path.Count > 0)
                {
                    var wp = AStarMono.ExtractWaypoints(path);
                    if (wp.Count > 0)
                    {
                        // 路径是否变更（简易哈希检测）
                        var hash = path.Count;
                        if (_cachedWaypointHash[i] != hash)
                        {
                            _waypointIndices[i] = 0;
                            _cachedWaypointHash[i] = hash;
                        }

                        activeIndices.Add(i);
                        activeWaypoints.Add(wp);
                    }
                }
            }

            var activeCount = activeIndices.Count;
            if (activeCount == 0) return;

            // 构建并行 NativeArray
            var positions = new NativeArray<float3>(activeCount, Allocator.TempJob);
            var wpIndices = new NativeArray<int>(activeCount, Allocator.TempJob);
            var wpCounts = new NativeArray<int>(activeCount, Allocator.TempJob);
            var speeds = new NativeArray<float>(activeCount, Allocator.TempJob);

            // 展平所有拐点
            var totalWp = 0;
            for (var i = 0; i < activeCount; i++)
                totalWp += activeWaypoints[i].Count;
            var wpFlat = new NativeArray<float3>(totalWp, Allocator.TempJob);
            var wpStarts = new NativeArray<int>(activeCount, Allocator.TempJob);

            var wpOffset = 0;
            for (var i = 0; i < activeCount; i++)
            {
                var regIdx = activeIndices[i];
                var obj = _registeredObjects[regIdx];
                var wps = activeWaypoints[i];

                positions[i] = obj.currentPos;
                wpIndices[i] = _waypointIndices[regIdx];
                wpCounts[i] = wps.Count;
                wpStarts[i] = wpOffset;
                speeds[i] = obj.moveSpeed;

                for (var j = 0; j < wps.Count; j++)
                    wpFlat[wpOffset + j] = wps[j];
                wpOffset += wps.Count;
            }

            // IJobFor 多线程并行移动
            var moveJob = new MoveJob
            {
                positions = positions,
                waypointIndices = wpIndices,
                waypointCounts = wpCounts,
                waypointStartIndices = wpStarts,
                waypointsFlat = wpFlat,
                speeds = speeds,
                deltaTime = Time.deltaTime,
                arriveThreshold = arriveThreshold,
            };
            moveJob.ScheduleParallel(activeCount, 64, new JobHandle()).Complete();

            // 主线程回写结果
            for (var i = 0; i < activeCount; i++)
            {
                var regIdx = activeIndices[i];
                var obj = _registeredObjects[regIdx];

                obj.ApplyPosition(positions[i]);
                _waypointIndices[regIdx] = wpIndices[i];
            }

            positions.Dispose();
            wpIndices.Dispose();
            wpCounts.Dispose();
            speeds.Dispose();
            wpFlat.Dispose();
            wpStarts.Dispose();
        }

        // ====================================================================
        // MoveJob —— IJobFor 并行移动计算
        // ====================================================================

        [BurstCompile]
        private struct MoveJob : IJobFor
        {
            /// <summary>当前世界坐标（读写：移动后更新）</summary>
            public NativeArray<float3> positions;

            /// <summary>当前前往的拐点索引（读写：到达后 +1）</summary>
            public NativeArray<int> waypointIndices;

            /// <summary>各实体拐点数量（只读）</summary>
            [ReadOnly] public NativeArray<int> waypointCounts;

            /// <summary>各实体拐点在展平数组中的起始索引（只读）</summary>
            [ReadOnly] public NativeArray<int> waypointStartIndices;

            /// <summary>展平拐点坐标（只读）</summary>
            [ReadOnly] public NativeArray<float3> waypointsFlat;

            /// <summary>各实体移动速度（只读）</summary>
            [ReadOnly] public NativeArray<float> speeds;

            public float deltaTime;
            public float arriveThreshold;

            public void Execute(int index)
            {
                var wpIdx = waypointIndices[index];
                var wpCount = waypointCounts[index];

                // 所有拐点已到达
                if (wpIdx >= wpCount) return;

                var pos = positions[index];
                var startIdx = waypointStartIndices[index];
                var target = waypointsFlat[startIdx + wpIdx];
                var speed = speeds[index];

                var toTarget = target - pos;
                var dist = math.length(toTarget);
                var step = speed * deltaTime;

                if (dist <= step || dist <= arriveThreshold)
                {
                    // 到达当前拐点
                    pos = target;
                    wpIdx++;
                }
                else
                {
                    // 向目标移动
                    pos += math.normalize(toTarget) * step;
                }

                positions[index] = pos;
                waypointIndices[index] = wpIdx;
            }
        }
    }
}
