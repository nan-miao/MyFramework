# Stat 数值系统

非泛型数值计算框架，支持三层数值修正：线性叠加区、加乘区、累乘区。适用于 RPG 角色属性、伤害计算等场景。

**命名空间**: `MyFramework.Stat`

---

## 计算公式

```
最终值 = (基础值 + 叠加区总和) × (1 + 加乘区总和) × ∏(1 + 每个累乘系数)
```

---

## 三种数值类型

```csharp
FloatStat maxHealth;  // 浮点数值（直接继承 Stat）
IntStat attack;       // 整数数值（提供 int 重载的 AddModifier/SetDefaultValue）
UintStat level;       // 无符号整数数值（提供 uint 重载）
```

**内部全 float 计算**：IntStat/UintStat 的输入在存储时自动转为 float，`GetValue()` 始终返回 float。子类仅做类型适配重载，不修改计算逻辑。

---

## 文件结构

```
MyFramework/Stat/
├── Stat.cs       # 非泛型基类，全 float 存储 + 惰性计算
├── FloatStat.cs  # FloatStat : Stat（构造器 float 参数化 + [LabelText]）
├── IntStat.cs    # IntStat : Stat（AddModifier/SetDefaultValue/RemoveModifier int 重载）
└── UintStat.cs   # UintStat : Stat（AddModifier/SetDefaultValue/RemoveModifier uint 重载）
```

---

## API

### 创建与基础值

```csharp
FloatStat maxHealth = new FloatStat(100f);
IntStat attack = new IntStat(10);
UintStat level = new UintStat(1u);

// 修改基础值
maxHealth.SetDefaultValue(100f);
attack.SetDefaultValue(20);      // int 重载
level.SetDefaultValue(5u);       // uint 重载
```

### 三层修正

```csharp
FloatStat health = new FloatStat(100f);

// 1. 叠加区（加固定数值）
health.AddModifier(50f);          // 100 + 50 = 150
health.RemoveModifier(50f);       // 移除该修正

// 2. 加乘区（百分比加成，多项求和）
health.AddAddPercentModifier(0.2f);  // +20%
health.AddAddPercentModifier(0.3f);  // +30%
// 当前 = (100 + 0) × (1 + 0.2 + 0.3) = 150

// 3. 累乘区（乘法堆叠，基于当前值逐个乘）
health.AddMultiplyPercentModifier(0.5f);  // ×1.5
health.AddMultiplyPercentModifier(0.2f);  // ×1.2
// 最终 = 150 × 1.5 × 1.2 = 270

// 获取最终值（始终返回 float）
float finalHealth = health.GetValue();

// 获取加乘区总百分比
float totalPercent = health.GetAddPercentTotal();
```

### IntStat 类型适配

```csharp
IntStat attack = new IntStat(10);

// int 重载（内部自动转 float）
attack.AddModifier(5);
attack.RemoveModifier(5);
attack.SetDefaultValue(20);

// float 方法同样可用（不建议混用）
attack.AddModifier(5.5f);
float value = attack.GetValue(); // 如 25.5
```

### UintStat 类型适配

```csharp
UintStat level = new UintStat(1u);
level.AddModifier(2u);
level.RemoveModifier(1u);
level.SetDefaultValue(10u);
```

### 重置

```csharp
health.Reset(); // 清除所有修正，重置为默认值
```

---

## 惰性计算机制

- **needCalculate 标记**: 修改修正值后标记 `needCalculate = true`
- **GetValue()**: needCalculate 为 true 时重新计算并缓存；否则返回上次计算结果
- **性能**: 频繁获取值时零计算，修改时仅标记 dirty

---

## 完整示例

```csharp
using MyFramework.Stat;

public class PlayerAttributes : MonoBehaviour
{
    public FloatStat maxHealth = new FloatStat(100f);
    public IntStat attack = new IntStat(10);
    public FloatStat moveSpeed = new FloatStat(5f);

    void Start()
    {
        // 装备加成（叠加区）
        maxHealth.AddModifier(50f);
        attack.AddModifier(5);

        // 被动技能（加乘区，可叠加）
        maxHealth.AddAddPercentModifier(0.15f);  // +15%
        attack.AddAddPercentModifier(0.1f);       // +10%

        // Buff（累乘区，乘法堆叠）
        attack.AddMultiplyPercentModifier(0.5f);
        moveSpeed.AddMultiplyPercentModifier(0.3f);

        Debug.Log($"最终生命: {maxHealth.GetValue()}");   // (100+50)×(1+0.15) = 172.5
        Debug.Log($"最终攻击: {attack.GetValue()}");       // (10+5)×(1+0.1)×1.5 = 24.75
        Debug.Log($"最终移速: {moveSpeed.GetValue()}");    // 5×1.3 = 6.5
    }

    public void RemoveAttackBuff()
    {
        attack.RemoveMultiplyPercentModifier(0.5f);
    }
}
```

---

## 注意事项

- **GetValue() 返回 float**：IntStat/UintStat 也返回 float，如需整数可 `(int)stat.GetValue()`。
- **加乘区基于基础值 + 叠加区**：`finalValue *= (1f + addPercentTotal)`。
- **累乘区基于当前值**：每个累乘系数依次乘上一步结果。
- **Odin Inspector 集成**：Stat 字段支持 `[LabelText]` 等 Odin 属性，可在 Inspector 中直观查看各层修正值。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
