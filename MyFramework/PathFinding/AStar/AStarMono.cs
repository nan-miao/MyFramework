using System.Collections.Generic;
using UnityEngine;

namespace MyFramework.PathFinding.AStar
{
    /// <summary>
    ///     AStar 寻路注册组件。
    ///     挂载到需要寻路的 GameObject 上，由 AStarManager 批量处理寻路请求，
    ///     由 AStarMonoManager 驱动沿路径移动。
    ///     也可通过 AddComponent 动态添加。
    /// </summary>
    public class AStarMono : MonoBehaviour
    {
        // ====================================================================
        // 输入 / 输出
        // ====================================================================

        /// <summary>当前世界坐标（输入）</summary>
        public Vector3 currentPos => transform.position;

        /// <summary>目标世界坐标（输入）</summary>
        public Vector3 endPos;

        /// <summary>寻路结果路径——世界坐标列表（输出，仅 findPath=true 时有效）</summary>
        public List<Vector3> path = new List<Vector3>();

        /// <summary>是否需要寻路计算（外部设置 true，管理器处理后设为 false）</summary>
        public bool needAStar;

        /// <summary>是否找到路径（输出，由管理器设置）</summary>
        public bool findPath;

        // ====================================================================
        // 移动相关
        // ====================================================================

        [Header("Movement")]
        /// <summary>移动速度（单位/秒）</summary>
        public float moveSpeed = 5f;

        /// <summary>是否正在沿路径移动</summary>
        public bool isMoving => path.Count > 0;

        // ====================================================================
        // 网格相关
        // ====================================================================

        [Header("Grid")]
        /// <summary>路径点方格内偏移（x=横向偏移→世界X轴，y=纵向偏移→世界Z轴；0.0=左下角，0.5=中心，1.0=右上角）</summary>
        public Vector2 cellOffset = new Vector2(0.5f, 0.5f);
        public void SetCellOffSet(Vector2 offset) => cellOffset = offset;

        // ====================================================================
        // Gizmos 调试可视化（仅编辑器）
        // ====================================================================

        [Header("Gizmos")]
        /// <summary>是否在 Scene 视图中绘制路径</summary>
        public bool drawGizmos = true;

        /// <summary>路径连线颜色（Inspector 可调）</summary>
        public Color gizmosColor = Color.green;
        
        public bool needMove = true;

        // ====================================================================
        // Unity 生命周期
        // ====================================================================

        protected virtual void Awake()
        {
            if (path == null)
                path = new List<Vector3>();
        }

        protected virtual void OnEnable()
        {
            MoveRegister();
        }

        

        protected virtual void OnDisable()
        {
            MoveUnRegister();
        }
        
        public void MoveRegister()
        {
            if (needMove)
            {
                AStarMonoManager.Instance?.Register(this);
            }
        }

        public void MoveUnRegister()
        {
            if (needMove)
            {
                AStarMonoManager.Instance?.Unregister(this);
            }
        }

        // ====================================================================
        // 公共方法
        // ====================================================================

        /// <summary>
        ///     设置寻路目标并标记需要计算。
        /// </summary>
        public virtual void RequestPath(Vector3 end)
        {
            endPos = end;
            needAStar = true;
            findPath = false;
            path.Clear();
            RegisterToManager();
        }

        /// <summary>
        ///     注册到 AStarManager。
        ///     调用后将由管理器在下一批次自动处理。
        /// </summary>
        public virtual void RegisterToManager()
        {
            AStarManager.Instance?.Register(this);
        }
        
        /// <summary>
        ///     由 AStarMonoManager 调用，设置移动后的新位置。
        /// </summary>
        public void ApplyPosition(Vector3 pos)
        {
            transform.position = pos;
        }

        // ====================================================================
        // ExtractWaypoints —— 从路径点列表中提取拐点
        // ====================================================================

        /// <summary>
        ///     从路径点列表中提取拐点。
        ///     拐点 = 方向向量发生变化的位置（含首尾点）。
        /// </summary>
        public static List<Vector3> ExtractWaypoints(List<Vector3> points)
        {
            if (points.Count <= 2)
                return new List<Vector3>(points);

            var result = new List<Vector3> { points[0] };

            for (var i = 1; i < points.Count - 1; i++)
            {
                var prevDir = (points[i] - points[i - 1]).normalized;
                var nextDir = (points[i + 1] - points[i]).normalized;

                // 方向变化超过阈值则记为拐点
                if (Vector3.Dot(prevDir, nextDir) < 0.9999f)
                {
                    result.Add(points[i]);
                }
            }

            result.Add(points[points.Count - 1]);
            return result;
        }

        // ====================================================================
        // OnDrawGizmos —— 编辑器下绘制路径（拐点连线）
        // ====================================================================

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            if (path == null || path.Count == 0)
                return;

            var waypoints = ExtractWaypoints(path);

            if (waypoints.Count < 2)
                return;

            Gizmos.color = gizmosColor;

            for (var i = 0; i < waypoints.Count - 1; i++)
            {
                Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
            }

            // 在每个拐点处绘制小圆球标记
            for (var i = 0; i < waypoints.Count; i++)
            {
                Gizmos.DrawSphere(waypoints[i], 0.1f);
            }
        }
#endif
    }
}
