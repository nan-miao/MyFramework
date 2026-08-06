# CustomPhysics 自定义物理

2D 自定义物理模拟器，手动驱动 `IPhysicsObject` 实例（Platform 和 Player 分类管理），通过 `MonoManager` 的生命周期回调实现物理 tick。

**管理器**: `PhysicsSimulator2D`（继承 `SingletonAutoMono`，自动创建）

**命名空间**: 全局命名空间（无显式命名空间）

---

## 架构

```
PhysicsSimulator2D (SingletonAutoMono)
├── 接口定义
│   ├── IPhysicsObject    ← 物理对象接口
│   └── IPhysicsMover     ← 运动器接口
├── 对象管理
│   ├── List<IPhysicsObject> platforms → 平台/静态物理体
│   └── List<IPhysicsObject> players   → 玩家/动态物理体
├── 延迟操作队列
│   ├── DeferredAddPlatform/Player
│   └── DeferredRemovePlatform/Player
└── 生命周期驱动 (MonoManager)
    ├── Update      → Player Update
    ├── FixedUpdate → Player FixedUpdate
    └── LateUpdate  → 处理延迟队列
```

---

## API

```csharp
// 注册/注销
public void AddPlatform(IPhysicsObject obj);
public void AddPlayer(IPhysicsObject obj);
public void RemovePlatform(IPhysicsObject obj);
public void RemovePlayer(IPhysicsObject obj);

// 生命周期控制
public void RegisterUpdateListeners();    // 注册到 MonoManager
public void UnregisterUpdateListeners();  // 从 MonoManager 移除
```

---

## 接口定义

所有接口定义在 `PhysicsSimulator2D.cs` 文件中：

```csharp
// 物理对象接口
public interface IPhysicsObject
{
    // 定义物理对象的基本行为
}

// 运动器接口
public interface IPhysicsMover
{
    // 定义运动相关行为
}

// Mono 所有者接口（定义在 MonoManager.cs 中）
public interface IMonoOwner { }
```

---

## 执行时序

```
MonoManager Update      → PhysicsSimulator2D.OnUpdate()
                        → 遍历 players: IPhysicsObject.Update()
MonoManager FixedUpdate → PhysicsSimulator2D.OnFixedUpdate()
                        → 遍历 players: IPhysicsObject.FixedUpdate()
MonoManager LateUpdate  → PhysicsSimulator2D.OnLateUpdate()
                        → 处理 DeferredAdd/Remove 队列
                        → 累加 _time
```

---

## 设计要点

- **Platform 和 Player 分离**: 分别存储在独立的 List 中，支持不同的物理行为。
- **延迟操作队列**: 在 `OnLateUpdate()` 中统一处理增删，避免遍历中修改集合。
- **IMonoOwner 接口**: `PhysicsSimulator2D` 实现此接口，作为 MonoManager 的宿主标识。
- **时间累积**: `_time` 字段持续累加 total elapsed time，用于物理计算。

---

## 注意事项

- **全局命名空间**: 此模块在全局命名空间（无 `namespace`），直接使用类名即可。
- **接口定义位置**: `IPhysicsObject` 和 `IPhysicsMover` 在同一文件中定义。
- **SingletonAutoMono**: 首次访问自动创建，DontDestroyOnLoad。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
