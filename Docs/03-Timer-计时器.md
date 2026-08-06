# Timer 计时器

计时器管理器，支持开始/暂停/重置/移除控制，支持间隔回调与总时间结束回调，可选择受 `Time.timeScale` 影响或使用 realtime。

**管理器**: `TimerManager`（继承 `SingletonAutoMono`，自动创建）

**命名空间**: `MyFramework.Timer`

---

## 架构

`TimerManager` 在 `OnStart()` 时通过 `MonoManager` 启动两个协程分别处理：
- **普通计时器**：受 `Time.timeScale` 影响（`WaitForSeconds(INTERVAL)`）
- **realTime 计时器**：不受 `timeScale` 影响（`WaitForSecondsRealtime(INTERVAL)`）

每个计时器使用 `TimerItem`（实现 `IPoolObject` 接口）存储数据，通过 `PoolManager` 的数据池复用，内部精度 20ms。

---

## API

### 创建计时器

两个重载，分别接受**毫秒(int)**和**秒(float)**：

```csharp
// 毫秒版（推荐精确控制）
int timeID = TimerManager.Instance.CreateTimer(
    isRealTime: false,          // false = 受 timeScale 影响，true = 不受
    allTime: 3000,              // 总时间 3000ms = 3s
    overCallBack: () => Debug.Log("计时结束"),
    intervalTime: 500,           // 每 500ms 执行一次间隔回调
    callBack: () => Debug.Log("每 0.5s")
);

// 秒版
int timeID2 = TimerManager.Instance.CreateTimer(
    isRealTime: false,
    allTime: 3.0f,
    overCallBack: EndAction,
    interval: 1.0f,
    callBack: IntervalAction
);
```

### 控制方法

```csharp
// 暂停
TimerManager.Instance.StopTimer(timeID);

// 恢复
TimerManager.Instance.StartTimer(timeID);

// 重置（重新开始计时）
TimerManager.Instance.ResetTimer(timeID);

// 移除（先暂停再移除）
TimerManager.Instance.RemoveTimer(timeID);
```

### 全局操作

```csharp
// 暂停所有计时器
TimerManager.Instance.PauseAllTimers();

// 恢复所有计时器
TimerManager.Instance.ResumeAllTimers();

// 安全清空所有计时器
TimerManager.Instance.ClearAllTimersSafe();

// 定时销毁物体
TimerManager.Instance.KillGameObject(gameObject, delaySeconds);
```

### HitStop（卡肉效果）

用于战斗中的"命中停顿"效果——冻结普通计时器一段时间，仅 realTime 计时器继续运转。

```csharp
// duration: 冻结持续时间（秒）
// timeScale: 冻结期间的 timeScale（0 = 完全暂停，0.1 = 慢速）
TimerManager.Instance.TriggerHitStop(duration: 0.1f, timeScale: 0f);

// 重置 HitStop 状态
TimerManager.Instance.ResetHitStop();
```

### TimerItem — 计时器数据

```csharp
public class TimerItem : IPoolObject
{
    public int id;                // 唯一 ID
    public float allTime;         // 总时间
    public float currentTime;     // 当前已过时间
    public bool isRealTime;       // 是否 realTime
    public bool isRunning;        // 是否运行中
    public bool forever;          // 是否永久循环
    public UnityAction overCallBack;    // 完成回调
    public UnityAction intervalCallBack; // 间隔回调
    public float intervalTime;    // 间隔时间

    public void ResetInfo(); // 对象池回收时重置
}
```

---

## 示例

```csharp
using MyFramework.Timer;

public class SkillCD : MonoBehaviour
{
    private int cdTimerID;
    private int buffTimerID;

    public void StartCooldown(float cdSeconds)
    {
        cdTimerID = TimerManager.Instance.CreateTimer(
            false, cdSeconds,
            overCallBack: () => Debug.Log("技能冷却完毕"),
            interval: 0.5f,
            callBack: () => Debug.Log("冷却中...")
        );
    }

    public void ApplyBuff(float duration)
    {
        // realTime 计时器：暂停菜单中也能正常衰减
        buffTimerID = TimerManager.Instance.CreateTimer(
            true, duration,
            overCallBack: () => Debug.Log("增益效果结束")
        );
    }

    public void PauseCD() => TimerManager.Instance.StopTimer(cdTimerID);
    public void ResumeCD() => TimerManager.Instance.StartTimer(cdTimerID);

    private void OnDestroy()
    {
        TimerManager.Instance?.RemoveTimer(cdTimerID);
        TimerManager.Instance?.RemoveTimer(buffTimerID);
    }
}
```

---

## 注意事项

- **计时完成自动销毁**：到达总时间后计时器自动移除并回收到数据池。
- **手动移除前建议先暂停**：确保回调不会被意外触发。
- **内部精度 20ms**：协程检查间隔为 20ms，间隔回调精度 ±20ms。
- **realTime 用途**：`timeScale=0`（如暂停菜单）时仍需要计时的场景（HitStop 恢复、UI 动画、Buff 衰减）。
- **数据池依赖**：`TimerItem` 通过 `PoolManager.DataPool` 复用，减少 GC。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
- [[04-Pool-对象池]]（TimerItem 使用数据池）
