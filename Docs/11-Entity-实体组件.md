# Entity 实体组件

组件化实体架构，通过组合 `IEntityComponent` 组件而非继承来构建实体行为。Entity 作为容器统一驱动各组件生命周期。

**命名空间**: `MyFramework.Entity`

---

## 核心组件

| 组件 | 类型 | 说明 |
|---|---|---|
| `IEntityComponent` | 接口 | 组件标准接口: `OnUpdate`, `OnFixedUpdate`, `OnLateUpdate` |
| `EntityComponentBase` | MonoBehaviour | 组件基类，自动注册到父级 Entity |
| `Entity` | MonoBehaviour | 实体本体，管理组件列表并统一驱动 |

---

## Entity — 实体本体

`Entity` 实现了 `IMonoOwner` 接口，使用 `List<IEntityComponent>` 管理组件。组件添加/移除通过延迟队列避免迭代中修改集合。

```csharp
using MyFramework.Entity;

public class MyEntity : Entity
{
    // 继承 Entity 后自动获得组件管理能力
}
```

### 核心方法

| 方法 | 说明 |
|---|---|
| `AddEntityComponent(component)` | 添加组件（同类型不允许重复） |
| `RemoveEntityComponent(component)` | 移除组件（下一帧 LateUpdate 统一执行） |
| `GetEntityComponent(Type type)` | 获取指定类型的组件 |
| `HasComponentOfType(Type type)` | 检查是否有指定类型组件 |

### 生命周期驱动

```
Update      → 遍历所有组件 → OnUpdate(deltaTime)
FixedUpdate → 遍历所有组件 → OnFixedUpdate(fixedDeltaTime)
LateUpdate  → 遍历所有组件 → OnLateUpdate → 处理待移除队列
```

---

## EntityComponentBase — 组件基类

继承此类可自动注册到父级 Entity。通过 `GetComponentInParent<Entity>()` 查找父级。

```csharp
using MyFramework.Entity;

public class HealthComponent : EntityComponentBase
{
    public float maxHealth = 100f;
    private float currentHealth;

    protected override void Start()
    {
        base.Start(); // 自动调用 AddEntityComponent(this)
        currentHealth = maxHealth;
    }

    protected override void ChildOnUpdate(float deltaTime)
    {
        // 每帧逻辑（deltaTime 由 Entity 传入）
    }

    protected override void ChildOnFixeUpdate(float fixedDeltaTime)
    {
        // 物理更新逻辑
    }

    protected override void ChildOnLateUpdate()
    {
        // 渲染后逻辑
    }

    private void OnDestroy()
    {
        // 从 Entity 中移除
        _entity?.RemoveEntityComponent(this);
    }
}
```

### 自动行为
- `Start()` → 向上查找 Entity → 调用 `AddEntityComponent(this)`
- `OnEnable()` → 重新添加到 Entity
- `OnDisable()` → 从 Entity 中移除

---

## IEntityComponent — 组件接口

不继承 MonoBehaviour 时可手动实现接口：

```csharp
public class AIComponent : IEntityComponent
{
    public void OnUpdate(float deltaTime) { /* AI 逻辑 */ }
    public void OnFixedUpdate(float deltaTime) { }
    public void OnLateUpdate() { }
}

// 手动添加到 Entity
var entity = GetComponent<Entity>();
var ai = new AIComponent();
entity.AddEntityComponent(ai);
```

---

## 完整示例

```csharp
// 实体
public class Enemy : Entity { }

// 组件1: 血量
public class EnemyHealthComponent : EntityComponentBase
{
    public float hp = 100f;

    protected override void ChildOnUpdate(float deltaTime)
    {
        if (hp <= 0)
        {
            _entity.RemoveEntityComponent(this);
            Destroy(gameObject);
        }
    }

    public void TakeDamage(float damage) => hp -= damage;
}

// 组件2: 移动
public class EnemyMoveComponent : EntityComponentBase
{
    public float speed = 3f;

    protected override void ChildOnUpdate(float deltaTime)
    {
        transform.Translate(Vector3.forward * speed * deltaTime);
    }
}
```

---

## 注意事项

- **延迟移除**：组件移除在 LateUpdate 中统一处理，避免遍历中修改集合。
- **同类型唯一**：`AddEntityComponent` 通过 `GetType()` 判断，已有同类型不添加。
- **EntityComponentBase 必须挂载在 Entity 子级**：使用 `GetComponentInParent<Entity>()` 向上查找。
- **OnDisable 自动移除**：组件 Disable 时自动移除，Re-enable 时重新添加。
- **_entity 引用**：`EntityComponentBase._entity` 指向所在 Entity（在 Start 中赋值）。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
