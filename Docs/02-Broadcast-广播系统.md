# Broadcast 广播系统

全局事件广播系统，用于模块间解耦通信。支持 0~4 个泛型参数的回调，通过 `BroadcastEventType` 枚举区分事件类型。

**命名空间**: `MyFramework.Broadcast`

---

## 核心概念

广播系统采用"基站-电话"模型：
- **BroadcastCenter**（静态类）是全局事件调度中心
- **BroadcastEventType** 枚举作为事件标识（"电话号码"）
- **CallBack** 委托系列作为回调方法（"接听方"）

内部使用 `Dictionary<BroadcastEventType, Delegate>` 存储所有监听。添加/广播/移除时进行类型检查，委托类型不匹配时抛出异常。

---

## 事件类型配置

在 `BroadcastEventType.cs` 的枚举中定义事件类型：

```csharp
public enum BroadcastEventType
{
    FinishAStar,          // A* 寻路批次完成
    AddCubeToMap,         // 地图添加方块
    RemoveCubeFromMap,    // 地图移除方块
    EndWave,              // 波次结束
    EndAllWave,           // 所有波次结束
    // 根据需要添加...
}
```

---

## API 使用

### 无参广播

```csharp
// 添加监听
BroadcastCenter.AddListener(BroadcastEventType.EndWave, OnEndWave);
// 发送广播
BroadcastCenter.Broadcast(BroadcastEventType.EndWave);
// 移除监听
BroadcastCenter.RemoveListener(BroadcastEventType.EndWave, OnEndWave);

void OnEndWave()
{
    Debug.Log("波次结束");
}
```

### 单参数泛型广播

```csharp
BroadcastCenter.AddListener<int>(BroadcastEventType.OnHealthChanged, OnHealthChanged);
BroadcastCenter.Broadcast(BroadcastEventType.OnHealthChanged, 80);
BroadcastCenter.RemoveListener<int>(BroadcastEventType.OnHealthChanged, OnHealthChanged);

void OnHealthChanged(int newHealth)
{
    Debug.Log($"血量变化: {newHealth}");
}
```

### 多参数泛型广播（2~4 参数）

```csharp
// 双参数
BroadcastCenter.AddListener<string, int>(BroadcastEventType.OnScoreUpdated, OnScoreUpdated);
BroadcastCenter.Broadcast(BroadcastEventType.OnScoreUpdated, "Player1", 999);

// 三参数
BroadcastCenter.AddListener<float, float, string>(eventType, callback);

// 四参数
BroadcastCenter.AddListener<int, string, bool, Vector3>(eventType, callback);
```

---

## 委托类型

```csharp
public delegate void CallBack();
public delegate void CallBack<T>(T arg);
public delegate void CallBack<T, X>(T arg1, X arg2);
public delegate void CallBack<T, X, Y>(T arg1, X arg2, Y arg3);
public delegate void CallBack<T, X, Y, Z>(T arg1, X arg2, Y arg3, Z arg4);
```

---

## 注意事项

- **类型匹配**：同一个 `BroadcastEventType` 下所有监听和广播必须使用相同参数数量的委托类型，否则抛出异常。
- **移除时机**：对象销毁时应主动调用 `RemoveListener`，避免内存泄漏。
- **静态全局**：`BroadcastCenter` 是纯静态类，无需实例化，任何地方都可直接调用。
- **AStar 集成**：`AStarManager` 在每批次寻路完成后自动广播 `BroadcastEventType.FinishAStar`。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
- [[06-Input-输入系统]]（旧输入系统通过 Broadcast 发送事件）
- [[17-PathFinding-AStar-寻路系统]]
