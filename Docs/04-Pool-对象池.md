# Pool 对象池

对象池模块提供 GameObject 池（自动释放 + 预创建）和泛型数据池两套机制，减少 GC 压力和 `Instantiate`/`Destroy` 开销。

**管理器**: `PoolManager`（继承 `SingletonAutoMono`，自动创建）

**命名空间**: `MyFramework.Pool`

---

## 架构

```
PoolManager (SingletonAutoMono)
├── GameObject 池字典: Dictionary<PoolType, PoolBase>
│   ├── AutoReleasePool — 对象外部创建，放入池中，超时自动销毁
│   └── PreloadPool — 预创建指定数量，Spawn 时从栈中取出
└── 数据池字典: Dictionary<Type, IDataPool>
    └── DataPool<T> — 泛型对象复用（Queue 实现，最大 100）
```

### PoolType 枚举

```csharp
public enum PoolType
{
    UI, Monster, Enemy, GameObject, EnemySpawnPoint,
    DropItem, Asset, Timer, Bullet, Effect
}
```

### PoolStrategy 枚举

```csharp
public enum PoolStrategy
{
    AutoRelease,  // 自动释放：超过 releaseTime 未使用则 Destroy
    Preload       // 预创建：初始化时预创建对象
}
```

### 默认自动创建的池

`PoolManager.OnStart()` 自动创建以下 AutoRelease 池：

| 池 | 释放时间 | 用途 |
|---|---|---|
| Effect | 400s | 特效 |
| Enemy | 600s | 敌人 |
| Bullet | 200s | 子弹 |
| DropItem | 600s | 掉落物 |

---

## GameObject 池 — AutoRelease 方案

对象在外部创建，使用完后放入对象池；一段时间内可重复使用，超时自动销毁。**池本身不创建对象**，Spawn 取不到时返回 null。

### 使用

```csharp
// 对象池索引名
string effectPoolName = "FireExplosion";
GameObject effectPrefab; // 备用预制体

// 从池中取出
GameObject effect = PoolManager.Instance.Spawn(PoolType.Effect, effectPoolName) as GameObject;

if (effect == null)
{
    // 池中无可用对象，用预制体创建
    effect = Instantiate(effectPrefab);
}
else
{
    // 从池中取出，重新激活
    effect.SetActive(true);
}

// 使用完毕后放入池中（自动 SetActive(false)）
PoolManager.Instance.UnSpawn(PoolType.Effect, effectPoolName, effect);
```

---

## GameObject 池 — Preload 方案（传统对象池）

初始化时预创建指定数量的对象，Spawn 时从 Stack 中弹出。栈空且未达上限时返回 null，已达上限时重用最早使用的对象。

```csharp
// 创建预创建池：最多 10 个，预创建 5 个
PoolManager.Instance.CreatePreloadPool(PoolType.UI, 10, uiPrefab, 5);

// 从池中取出
GameObject panel = PoolManager.Instance.Spawn(PoolType.UI, "PanelName") as GameObject;

// 放回池中
PoolManager.Instance.UnSpawn(PoolType.UI, "PanelName", panel);
```

---

## 数据池（泛型）

适用于纯 C# 类对象的复用，完全独立于 Unity 生命周期。使用 `Queue<T>` 实现，最大缓存 100 个，超出后不再入池（交由 GC）。

### 实现 IPoolObject 接口

```csharp
public class PlayerData : IPoolObject
{
    public int Health;
    public int Level;

    public void ResetInfo()
    {
        Health = 100;
        Level = 1;
    }
}
```

### 使用

```csharp
// 获取数据（首次自动创建数据池）
PlayerData playerData = PoolManager.Instance.GetData<PlayerData>();
playerData.Health = 80;

// 归还数据（自动调用 ResetInfo）
PoolManager.Instance.ReturnData(playerData);

// 检查池是否存在
if (PoolManager.Instance.HasDataPool<PlayerData>())
    Debug.Log("PlayerData 池已存在");

// 清空指定池
PoolManager.Instance.ClearDataPool<PlayerData>();
```

---

## 全局操作

```csharp
// 释放所有池
// force=false: AutoRelease 池行为不变; Preload 池仅回收
// force=true: 强制立即销毁所有池对象
PoolManager.Instance.ReleaseAll(force: true);
```

---

## 注意事项

- **AutoRelease 池不在池内创建对象**：对象由外部 `Instantiate` 后放入，Spawn 取不到返回 null。
- **Preload 池上限**：Spawn 时栈空且未达上限返回 null；达到上限复用最早使用的对象。
- **UnSpawn 自动 SetActive(false)**：确保放回池的对象不可见。
- **数据池限制**：最大 100 个缓存，超出后直接丢弃（交由 GC）。
- **AutoRelease 时间基准**：使用 `DateTime.Now.Ticks` 比较，不受 timeScale 影响。
- **池 GameObject 层级**：自动创建为 PoolManager 的子对象，保持场景整洁。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
- [[03-Timer-计时器]]（TimerItem 使用数据池）
