# AI GOAP 目标导向行动规划

Goal-Oriented Action Planning（GOAP）是一个基于逆向搜索的 AI 决策系统。AI Agent 根据世界状态（World State）和目标优先级，通过逆向链式搜索（从目标效果反查动作的前提条件）生成动作序列，执行 Plan 直到目标达成或被打断。

**命名空间**: `MyFramework.AI.GOAP`

---

## 核心架构

```
GOAPAgent (决策循环核心, MonoBehaviour)
├── GOAPStates (世界状态: Dictionary<string, GOAPStateBase>)
├── GOAPGoals  (目标管理器, 按优先级排序)
│   └── Goal (目标状态 + 优先级 + 可打断标记 + IGOAPGoalChecker)
├── GOAPActions (动作库, 按 Effect 建索引)
│   └── GOAPActionBase (动作基类: Preconditions + Effects + Cost/Value/Priority)
└── GOAPPlan (当前执行计划)
    └── GOAPPlanNode (计划节点树: 动作 + 父子关系)
        └── GOAPRunState { Running, Succeed, Failed }
```

---

## 子系统文件清单

| 子系统 | 文件 | 职责 |
|---|---|---|
| **Agent** | `GOAPAgent.cs` | 决策循环: 目标排序 → 逆向搜索 → 执行/替换 Plan |
| | `IGOAPOwner.cs` | 空接口，标记 Agent 的所有者 |
| | `GOAPGlobal.cs` | 全局共享状态（场景单例） |
| | `GOAPObjectPool.cs` | GOAP 专用对象池（Node/临时集合复用） |
| | `GOAPEditorUtility.cs` | Editor 工具：追踪选中 Agent 和 GOAPGlobal |
| **Action** | `GOAPActionBase.cs` | 动作基类: 前提条件/效果/成本/优先级/冷却/状态机 |
| | `GOAPActions.cs` | 动作库: Effect→Action 反向索引，加速逆向搜索 |
| **Goal** | `GOAPGoals.cs` | 目标列表: 排序、优先级更新、打断检查 |
| | `IGOAPGoalChecker.cs` | 目标优先级动态评估接口 |
| **Plan** | `GOAPPlan.cs` | 计划执行器: 从最深叶节点开始执行 |
| | `GOAPPlanNode.cs` | 计划节点: 链接 Action + Parent + Children |
| | `GOAPRunState.cs` | 枚举: `Running / Succeed / Failed` |
| **State** | `GOAPStateBase.cs` | 状态抽象 + CRTP 泛型辅助（类型安全比较/设置/复制） |
| | `GOAPStates.cs` | 状态字典: 添加/检查前提/检查效果/应用效果 |
| | `GOAPStateType.cs` | 状态下拉选择 struct（Odin ValueDropdown） |
| | `GOAPStateComparer.cs` | 比较器抽象 + CRTP 辅助 |
| | `BoolState.cs` | 布尔状态: 是/否 |
| | `FloatState.cs` | 浮点状态: 大于/小于/大于等于/小于等于/提升即可/下降即可/等于 |
| | `IntState.cs` | 整数状态（同 FloatState 符号体系） |
| | `UnityObjectState.cs` | Unity Object 引用状态 |
| **Editor** | `GOAPAgentEditor.cs` | GOAPAgent 的 Odin Inspector 自定义编辑器 |
| | `GOAPPlanWindow.cs` | Editor 窗口: Plan 树可视化 |

---

## 决策循环

```
每帧 GOAPAgent.OnUpdate()
    ↓
1. GOAPGoals.UpdateGoals()
   ├── 遍历 Goal, 调用 IGOAPGoalChecker.Update() 更新 runtimePriority
   └── 按 Priority 降序排列
    ↓
2. 遍历目标（从高优先级开始）
   ├── 检查目标状态是否已满足? → 跳过，尝试下个目标
   └── 逆向搜索:
       a. 找出所有能产生目标状态的 Action（通过 GOAPActions 索引）
       b. 对每个候选 Action，检查 Preconditions
       c. 如果 Precondition 当前不满足 → 递归搜索能满足该状态的 Action
       d. 当所有叶子 Precondition 满足 → 搜索成功 → 构建 Plan
    ↓
3. 新 Plan 优先级 > 当前 Plan? → 打断旧 Plan，启动新 Plan
    ↓
4. GOAPPlan.OnUpdate() — 从最深叶节点到根顺序执行
   ├── 叶节点 Action.OnUpdate() → 完成? → 向父节点推进
   └── 全部完成? → 应用 Effects → Plan.Succeed
```

---

## GOAPAgent — 核心 Agent

```csharp
// 关键字段
public GOAPGoals goals;        // 目标管理器
public GOAPStates states;       // 自身状态
public GOAPActions actions;     // 动作库
public GOAPPlan plan;           // 当前计划

// 关键方法
public void Init(IGOAPOwner owner);
public void OnUpdate();                              // 每帧决策
public void ApplyEffect(GOAPTypeAndComparer effect); // 应用效果到状态
public bool CheckStateForPrecondition(GOAPStateType, GOAPStateComparer);
public void StopPlan();
public void ResetStates();                           // 对象池回收重置
```

---

## GOAPActionBase — 动作基类

```csharp
// 配置字段
public string actionName;
public float cost;            // 动作成本（越低越优先）
public float priority;        // 动作优先级
public bool canInterrupt;     // 是否可被更高优先级打断
public float cooldown;        // 冷却时间（秒）

// 前提条件与效果
public List<GOAPTypeAndComparer> preconditions;
public List<GOAPTypeAndComparer> effects;

// 状态机
public void StartRun();
public virtual void OnStart();
public virtual void OnUpdate();
public virtual void OnStop();
public void ApplyEffect();    // 执行成功后应用效果

// 冷却
public void ApplyCoolDownOnActive();
```

---

## Plan 执行流程

```
Plan 树结构:
  Root (Action 产生 Goal 状态)
   ├── Child 1 (Action 满足 Root 的 Precondition)
   │    └── Leaf (Action 满足 Child 1 的 Precondition)
   └── Child 2 (Action 满足 Root 的另一个 Precondition)

执行顺序:
  1. 从最深叶节点开始执行 (Leaf)
  2. Leaf 完成 → 触发 Parent 继续 → ... → Root 完成
  3. Root 完成 → 应用所有 Effects → Plan 终止
```

---

## 状态系统

### 状态类型

| 类型 | 枚举值 | 说明 |
|---|---|---|
| BoolState | 是/否 | 布尔状态 |
| FloatState | 大于/小于/大于等于/小于等于/提升即可/下降即可/等于 | 带比较符号的浮点值 |
| IntState | 同上 | 带比较符号的整数值 |
| UnityObjectState | 是/否/为空/不为空 | Object 引用状态 |

### CRTP 泛型辅助

每个状态类型使用 `GOAPStateBase<T, V, C>` 基类，实现类型安全的比较和复制：

```csharp
// BoolState = GOAPStateBase<BoolState, bool, BoolStateComparer>
// FloatState = GOAPStateBase<FloatState, float, FloatStateComparer>
// IntState = GOAPStateBase<IntState, int, IntStateComparer>
// UnityObjectState = GOAPStateBase<UnityObjectState, Object, UnityObjectStateComparer>
```

### 全局状态（GOAPGlobal）

场景级单例，持有跨 Agent 共享的状态（如"是否夜幕"、"天气"等）。Agent 检查 Precondition 时优先查找 GlobalStates。

---

## Editor 工具

### GOAPAgentEditor
Odin Inspector 自定义编辑器，选中 Agent 时自动更新 `GOAPEditorUtility.agent` 静态引用。

### GOAPPlanWindow
菜单: `Tools → GOAP → GOAPPlanWindow`，显示选中 Agent 的 Plan 树，当前执行节点红色高亮，其余节点黄色。

---

## GOAPObjectPool — 专用对象池

独立于 `PoolManager` 的轻量对象池，使用 `Dictionary<Type, Stack<object>>` 存储，主要用于复用 PlanNode 和临时 SortedSet：

```csharp
var node = GOAPObjectPool.Get<GOAPPlanNode>();
GOAPObjectPool.Recycle(node);
```

---

## 注意事项

- **状态的 name 是唯一键**: `GOAPStates` 使用 `stateType.name` 作为字典 key。
- **效果查找索引**: `GOAPActions.Init()` 会构建 `Effect → List<Action>` 的反向索引，加速逆向搜索。
- **冷却系统**: Action 的冷却通过 `TimerManager` 计时，冷却中不会被执行。
- **状态备份**: Agent 有 `BackupDefaultStates()` 机制，对象池回收时通过 `ResetStates()` 恢复。
- **优先排序**: Equal priority 的两个目标不会去重（`SortedGoalComparer` 对相等优先级的比较返回 -1）。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
- [[03-Timer-计时器]]
- [[04-Pool-对象池]]
