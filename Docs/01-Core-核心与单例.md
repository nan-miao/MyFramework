# Core 核心与单例

Core 模块是 MyFramework 的基石，提供三种单例模式基类和一个统一的 MonoBehaviour 生命周期管理器。其他模块几乎都依赖此模块。

**命名空间**: `MyFramework.Core` / `MyFramework.Core.Singleton`

---

## 1. 单例基类

### 1.1 BaseManager\<T\> — 非 MonoBehaviour 单例

适用于纯 C# 类（不需要挂载到 GameObject）。通过反射调用私有无参构造函数创建实例，双重检查锁保证线程安全。

```csharp
using MyFramework.Core.Singleton;

public class GameManager : BaseManager<GameManager>
{
    // 构造函数必须为 private
    private GameManager() { }

    public void DoSomething() { }
}

// 使用（自动创建实例）
GameManager.Instance.DoSomething();
```

### 1.2 SingletonAutoMono\<T\> — 自动创建式 MonoBehaviour 单例

**推荐使用**。首次访问 `Instance` 时自动创建 GameObject 并挂载脚本，自动标记 `DontDestroyOnLoad`。无需手动挂载。

```csharp
using MyFramework.Core.Singleton;

public class AudioManager : SingletonAutoMono<AudioManager>
{
    protected override void OnStart()
    {
        // 初始化逻辑放在这里，代替 Start()
        base.OnStart();
    }
}

// 任何地方直接使用，无需挂载
AudioManager.Instance.PlaySound("bgm");
```

**命名规则**: 自动创建的 GameObject 使用 `typeof(T).ToString()` 命名。

### 1.3 SingletonMono\<T\> — 手动挂载式 MonoBehaviour 单例

需要手动在场景中挂载到 GameObject。通过 `global` 字段控制是否跨场景保留（`DontDestroyOnLoad`）。`Awake()` 中检测重复实例并自动销毁。

```csharp
using MyFramework.Core.Singleton;

public class MonoManager : SingletonMono<MonoManager>
{
    // Inspector 中设置 global = true 则跨场景保留
    public bool global = true;
}
```

### 对比

| 类型 | 创建方式 | 跨场景 | 线程安全 | 适用场景 |
|---|---|---|---|---|
| `BaseManager<T>` | 反射自动创建 | 天然全局 | 双重检查锁 | 纯数据/逻辑管理器 |
| `SingletonAutoMono<T>` | 首次访问自动创建 GameObject | 自动 DontDestroyOnLoad | 需 Unity 主线程 | 需要 MonoBehaviour 生命周期的全局管理器 |
| `SingletonMono<T>` | 手动挂载到场景 | 可选（`global` 字段） | 需 Unity 主线程 | 场景级管理器 / 需在 Inspector 配置 |

---

## 2. MonoManager — 生命周期管理器

将分散在各处的 Update / FixedUpdate / LateUpdate 统一管理，支持优先级排序和执行频率控制，避免每个脚本单独接收 Unity 生命周期回调的 GC 开销。

**类**: `MonoManager`（继承 `SingletonAutoMono`，自动创建）

**核心机制**:
- 使用 `SortedDictionary<int, List<Action>>` 按 order 排序
- 频率控制：`frameCount % frequency == 0` 时执行
- 脏缓存优化：仅在增删时重建 key 列表
- 自动清理：检测到委托 Target 为 null 时自动移除
- `ExecuteActions` 使用 `GetInvocationList()` 处理多播委托，避免迭代中修改集合

### API

```csharp
// 添加监听
// action: 回调方法
// order: 优先级，越小越先执行（默认 0）
// frequency: 每 N 帧执行 1 次（默认 1，每帧执行）
MonoManager.Instance.AddUpdateListener(MyUpdate, order: 0, frequency: 1);
MonoManager.Instance.AddLateUpdateListener(MyLateUpdate, order: 0, frequency: 1);
MonoManager.Instance.AddFixedUpdateListener(MyFixedUpdate, order: 0, frequency: 1);

// 移除监听
MonoManager.Instance.RemoveUpdateListener(MyUpdate);
MonoManager.Instance.RemoveLateUpdateListener(MyLateUpdate);
MonoManager.Instance.RemoveFixedUpdateListener(MyFixedUpdate);

// 清空所有监听
MonoManager.Instance.ClearAll();
```

### 示例

```csharp
using MyFramework.Core;

public class Player : MonoBehaviour
{
    private void Start()
    {
        // Update 每 5 帧执行一次，优先级最高（order=-10 最先执行）
        MonoManager.Instance.AddUpdateListener(Detection, order: -10, frequency: 5);
    }

    private void Detection()
    {
        // 检测逻辑，每 5 帧执行一次
    }

    private void OnDestroy()
    {
        // 场景切换/对象销毁时应主动移除
        MonoManager.Instance?.RemoveUpdateListener(Detection);
    }
}
```

### IMonoOwner 接口

`IMonoOwner` 接口定义在 `MonoManager.cs` 中（注意在命名空间外部），供需要持有 MonoManager 引用的管理器实现。

### 注意事项

- **频率控制**: `frequency=5` 意味着每 5 个 Update 执行一次。使用 `Time.deltaTime` 时也会丢失 4 帧的增量，建议用 `Time.time` 计算绝对时间，或在执行时乘以 frequency（会有一定误差）。
- **优先级**: order 越小越先执行（`SortedDictionary` 排序），可用于保证执行顺序（如 AStarManager=0 先于 AStarMonoManager=1）。
- **独立帧计数器**: Update、LateUpdate、FixedUpdate 各有独立计数器。
- **多播委托**: 移除时对委托的每个 invocation 分别处理。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[02-Broadcast-广播系统]]
- [[03-Timer-计时器]]
