# PathFinding A* 寻路系统

基于 Burst Job System + IJobFor 多线程并行的分层 A* 寻路方案。五个关键脚本各司其职：Job 负责纯计算、Manager 负责批量调度、Mono 负责实体挂载、MonoManager 负责移动驱动、PathHelper 负责离线预计算。

**命名空间**: `MyFramework.PathFinding.AStar`

---

## 整体架构

```
                    ┌─────────────────────────────┐
                    │    AStarMonoManager          │  ← 每帧驱动移动 (MoveJob)
                    │    (SingletonAutoMono)        │
                    └──────────────┬──────────────┘
                                   │ Register/Unregister
                    ┌──────────────┴──────────────┐
                    │         AStarMono            │  ← 挂载在实体上
                    │  (currentPos/endPos/path)     │
                    └──────────────┬──────────────┘
                                   │ RequestPath → RegisterToManager
                    ┌──────────────┴──────────────┐
                    │       AStarManager           │  ← 批量调度 (SingletonAutoMono)
                    │  WorldToGridJob→FindPathJob   │
                    └──────────────┬──────────────┘
                                   │ 共用 FindPathJob
                    ┌──────────────┴──────────────┐
                    │      AStarPathHelper         │  ← 离线预计算 (SingletonAutoMono)
                    │  (非实体路径, 可存字典复用)    │
                    └─────────────────────────────┘
```

### 执行顺序

通过 `MonoManager.AddUpdateListener` 的 `order` 参数保证时序：
- `AStarManager`: `order=0`（先执行，计算路径）
- `AStarMonoManager`: `order=1`（后执行，沿路径移动）

---

## 1. AStarFindPathJob — 寻路计算核心

**文件**: `AStarFindPathJob.cs`
**定位**: Burst 编译的纯 Struct Job（`[BurstCompile] IJobFor`），只负责计算，不持有任何状态。

### 关键设计

| 特性 | 实现 |
|---|---|
| 并行方式 | `IJobFor` + `ScheduleParallel(count, 64, ...)` — 每个 index 独立计算一条路径 |
| 方向支持 | `use8Direction`: true=8 向（含对角线），false=4 向 |
| 内存分配 | `Allocator.Temp` — Job 内临时分配，Execute 退出前 Dispose |
| Collections 兼容 | NativeArray + 手动计数（Collections 1.x 无 NativeList 支持） |

### 输入

```
moveStraightCost / moveDiagonalCost     ← 移动代价 (直=10, 斜=14)
gridSize                                ← 网格尺寸
blockedPositions (NativeArray<int2>)    ← 障碍物网格坐标
startPositions / endPositions           ← 起终点 (NativeArray, 长度=请求数)
cellOffsets (NativeArray<float2>)       ← 方格内偏移 (0.0=左下, 0.5=中心)
```

### 输出

```
pathFoundResults[i]    ← 第 i 个请求是否找到路径
worldPathCounts[i]     ← 路径节点数
worldPaths[展平]        ← 二维展平: todos[ index * maxPathLength + j ]
```

### 核心算法

标准 A*：
1. 初始化节点（计算 H = Octile 距离），标记不可行走
2. OpenList + ClosedList（NativeArray + 手动计数）
3. 主循环：取 F 最小节点 → 扩展邻居 → 更新 G/H/F → 找到终点则结束
4. 回溯 `cameFromNodeIndex` 构建路径 → 反转输出为 start→end 顺序
5. `GetWorldPos` 将网格坐标 + cellOffset 转为世界坐标

---

## 2. AStarManager — 寻路调度中心

**文件**: `AStarManager.cs`
**定位**: `SingletonAutoMono`，每隔 N 帧批量处理注册的寻路请求。

### 数据流

```
AStarMono.RequestPath(end)
  → AStarManager.Register(mono)        // 加入 _registeredObjects
  → mono.needAStar = true

每 frameInterval 帧: ProcessBatch()
  ├─ 收集 needAStar==true 的对象 (最多 maxBatchSize)
  ├─ Step A: WorldToGridJob (IJobFor) — 世界坐标 → 网格坐标
  ├─ Step B: FindPathJob (IJobFor)   — 多线程并行 A*
  ├─ Step C: 主线程回写 path/findPath/needAStar 到各 AStarMono
  └─ Step D: BroadcastCenter.Broadcast(FinishAStar)
```

### 配置

```csharp
// AStarManagerConfig (可序列化)
public int2 gridSize;           // 网格尺寸
public float cellSize;          // 单元格大小
public int frameInterval;       // 批次间隔帧数
public int maxBatchSize;        // 每批最大请求数
public float moveStraightCost;  // 直线移动代价
public float moveDiagonalCost;  // 对角线代价
public bool use8Direction;      // 是否 8 向
```

### 障碍物管理

```csharp
AStarManager.Instance.SetBlockedPositions(positions);    // 全量替换
AStarManager.Instance.AddBlockedPosition(pos);            // 单点添加（去重）
AStarManager.Instance.RemoveBlockedPosition(pos);         // 单点移除
AStarManager.Instance.GetBlockedPositions();              // 返回副本
```

### 关键设计
- **配置快照**: Job 调度前复制 config 到栈变量，防止执行期间被修改
- **内存管理**: `Allocator.TempJob` — 生命周期跨越 `ScheduleParallel.Complete()`
- **自适应频率**: `OnValidate` 中检测 `frameInterval` 变化自动重注册 MonoManager 回调

---

## 3. AStarMono — 实体寻路组件

**文件**: `AStarMono.cs`
**定位**: 挂载在需要寻路的 GameObject 上，连接 Manager 和 MonoManager 的桥梁。

### 关键字段

| 字段 | 类型 | 说明 |
|---|---|---|
| `currentPos` | `Vector3` | 当前世界坐标（只读，=transform.position） |
| `endPos` | `Vector3` | 目标世界坐标 |
| `path` | `List<Vector3>` | 寻路结果路径（Manager 回写） |
| `needAStar` | `bool` | 是否需要重新计算（外部设 true，Manager 回写 false） |
| `findPath` | `bool` | 是否找到路径（Manager 回写） |
| `moveSpeed` | `float` | 移动速度 |
| `cellOffset` | `Vector2` | 方格内偏移（默认 0.5,0.5） |
| `needMove` | `bool` | 是否需要移动（true → 注册到 MonoManager） |

### 方法

| 方法 | 说明 |
|---|---|
| `RequestPath(Vector3 end)` | 设置目标 + needAStar=true + 注册到 Manager |
| `RegisterToManager()` | 注册到 AStarManager |
| `ApplyPosition(Vector3 pos)` | 由 AStarMonoManager 调用，修改 transform.position |
| `ExtractWaypoints(List<Vector3>)` | 静态方法：从路径点提取拐点（方向变化处 + 首尾） |

### 拐点提取

```
ExtractWaypoints 逻辑:
  - 遍历路径点
  - 当移动方向变化时 (dot < 0.9999f) → 记录拐点
  - 首尾必须保留
```

---

## 4. AStarMonoManager — 移动驱动器

**文件**: `AStarMonoManager.cs`
**定位**: `SingletonAutoMono`，每 N 帧驱动所有已注册实体的路径移动。

### 数据流

```
每 frameInterval 帧: ProcessMovement()
  ├─ 收集 path 非空的实体
  ├─ ExtractWaypoints(path) 提取拐点
  ├─ 路径变更检测 (简易哈希: path.Count)
  ├─ Step A: 展平所有实体的拐点到 NativeArray
  ├─ Step B: MoveJob (IJobFor) — 并行向目标拐点移动
  └─ Step C: 主线程回写 position + waypointIndex
```

### MoveJob 逻辑

```
for each entity (并行):
    wpIdx = 当前拐点索引
    if wpIdx >= 总拐点数: return   // 已到达终点

    target = waypointsFlat[wpIdx]
    dist = |target - currentPos|
    step = speed * deltaTime

    if dist <= step || dist <= arriveThreshold:
        pos = target       // 到达拐点
        wpIdx++            // 前进到下个拐点
    else:
        pos += normalize(target - pos) * step  // 向目标移动
```

---

## 5. AStarPathHelper — 离线路径预计算

**文件**: `AStarPathHelper.cs`
**定位**: `SingletonAutoMono`，不绑定实体。用于预计算多组起终点路径。

### 使用场景
- 开战前预计算所有远程单位攻击路径
- 批量验证多组坐标可达性
- 生成导航网格可达性缓存

### API

```csharp
// 添加路径信息，返回自增 Key
int key = AStarPathHelper.Instance.AddPathInfo(pathInfo);

// 按 Key 计算
AStarPathHelper.Instance.CalculatePath(key, extraBlockPos, callBack);

// 直接计算传入的 PathInfo
AStarPathHelper.Instance.CalculatePath(ref pathInfo, callBack, extraBlockPos,
    recordPath: true, requireAllPaths: false);
```

### PathInfo 结构体

```csharp
public struct PathInfo
{
    public List<Vector3> startPos;   // 起点列表
    public List<Vector3> endPos;     // 终点列表 (一一对应)
    public Dictionary<Vector3, List<Vector3>> paths; // 结果字典

    public bool HavePath(); // 是否存在至少一条可行路径
}
```

### 额外障碍物
`extraBlockPos` 参数可与全局障碍物合并计算，重复位置自动去重 + 警告日志。

---

## 使用示例

### 实体寻路 + 自动移动

```csharp
// 1. 获取组件
var mono = GetComponent<AStarMono>();
mono.moveSpeed = 5f;
mono.needMove = true;         // 启用自动移动
mono.SetCellOffSet(new Vector2(0.5f, 0.5f)); // 方格中心

// 2. 请求路径
mono.RequestPath(targetPosition);
// 3. AStarManager 自动在下一批次计算路径
// 4. AStarMonoManager 自动驱动物体沿路径移动
// 5. 到达终点后 path 被清空
```

### 离线预计算

```csharp
var info = new PathInfo
{
    startPos = new List<Vector3> { pos1, pos2 },
    endPos   = new List<Vector3> { target1, target2 },
};

bool success = AStarPathHelper.Instance.CalculatePath(ref info);

if (success)
{
    // info.paths[target1] = pos1 到 target1 的路径点列表
}
```

---

## 与旧 Pathfinding.cs 的关系

`Pathfinding.cs` 是原始 A* 实现（单线程、托管内存），现在仅保留作为参考。新架构的五脚本完全替代其功能，提供 Burst 加速 + 多线程并行 + 批量处理。

---

## 注意事项

- **帧间隔不可太小**: frameInterval 过小会导致每帧调度 Job（overhead > 收益）。
- **内存分配**: 所有 Job NativeArray 使用 `Allocator.TempJob`，生命周期跨越 Complete()，主线程手动 Dispose。
- **批量大小**: maxBatchSize 控制每批最多处理的请求数，超出部分下批处理。
- **障碍物更新**: Add/Remove 方法内部去重，但频繁操作建议批量 SetBlockedPositions。
- **Gizmos 可视化**: AStarMono 在 Editor 中绘制路径拐点（彩色线条 + 球标记）。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
- [[02-Broadcast-广播系统]]（FinishAStar 广播）
