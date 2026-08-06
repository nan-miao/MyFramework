# MyFramework

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-9.0-blue?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

**MyFramework** 是一个 Unity 游戏开发框架，由个人项目实践积累的 C# 代码演化而来。模块化设计、低耦合、开箱即用，覆盖 AI、寻路、资源管理、事件系统、存档等 18 个核心模块。

---

## 功能模块

| # | 模块 | 简介 |
|---|---|---|
| 1 | **Core** | MonoManager 生命周期管理 + 三种单例基类 |
| 2 | **Broadcast** | 全局事件广播/监听，支持 0~4 个泛型参数 |
| 3 | **Timer** | 计时器：暂停/重置/移除 + 间隔回调 + HitStop |
| 4 | **Pool** | 对象池：GameObject 自动释放池 + 预创建池 + 泛型数据池 |
| 5 | **Debuger** | 条件编译日志：颜色标签 + 文件存储 + 线程安全写入 |
| 6 | **Input** | 输入系统：新版 InputSystem + 旧版 InputManager 双方案 |
| 7 | **Save** | JSON 存档：加密存储 + SaveContainer 泛型容器 + ISaveData 接口 |
| 8 | **AssetLoad** | 资源加载：Addressables / AssetBundle / Resources / Editor / UWQ 五大方案 |
| 9 | **Scene** | 场景管理：Addressables 异步加载 + 批量卸载 + 进度查询 |
| 10 | **Stat** | 数值系统：叠加区/加乘区/累乘区三层修正计算 |
| 11 | **Entity** | 组件化实体：组合模式 + 统一生命周期驱动 |
| 12 | **UI** | UI 基类：BasePanel 自动控件绑定 + 事件分发 |
| 13 | **Util** | 工具集：JSON/加密/路径/随机/数学/文本/DOTween/Binary |
| 14 | **GOAP AI** | 目标导向行动规划：逆向搜索 + 状态机 + 可视化编辑器 |
| 15 | **Camera** | 相机管理：Cinemachine 虚拟相机 Priority 切换 |
| 16 | **CustomPhysics** | 2D 自定义物理：Platform/Player 分离 + MonoManager 驱动 |
| 17 | **A\* PathFinding** | Burst Job 多线程并行 A* 寻路 + 拐点移动 + 离线预计算 |

---

## 架构概览

```
                    ┌──────────────┐
                    │  MonoManager │  ← 统一生命周期管理
                    │  BaseManager │  ← 非 Mono 单例
                    │SingletonMono │  ← Mono 单例
                    └──────┬───────┘
           ┌───────────────┼───────────────┐
           │               │               │
    ┌──────▼──────┐ ┌──────▼──────┐ ┌──────▼──────┐
    │ Broadcast   │ │  TimerMgr   │ │  PoolMgr    │
    │ (事件总线)   │ │  (计时器)   │ │  (对象池)   │
    └─────────────┘ └─────────────┘ └─────────────┘
           │               │               │
    ┌──────▼──────┐ ┌──────▼──────┐ ┌──────▼──────┐
    │ InputMgr    │ │  SaveMgr    │ │  AssetLoad  │
    │ (输入系统)   │ │  (存档)     │ │  (资源加载)  │
    └─────────────┘ └─────────────┘ └─────────────┘
           │               │               │
    ┌──────▼──────┐ ┌──────▼──────┐ ┌──────▼──────┐
    │ GOAP AI     │ │  AStar      │ │  CustomPhys │
    │ (AI决策)     │ │  (寻路)     │ │  (自定义物理) │
    └─────────────┘ └─────────────┘ └─────────────┘
```

> **Core 模块是框架基石**：几乎所有模块都依赖 `BaseManager<T>`、`SingletonAutoMono<T>`、`SingletonMono<T>` 和 `MonoManager`。其他模块间耦合极少，可按需取用。

---

## 快速开始

### 1. 导入框架

将 `MyFramework/` 文件夹放入 Unity 项目的 `Assets/` 目录。

### 2. 基础单例

```csharp
// 非 Mono 单例 — 自动创建，无需挂载
public class GameManager : BaseManager<GameManager> { }
GameManager.Instance.DoSomething();

// Mono 单例 — 首次访问自动创建 GameObject
public class AudioManager : SingletonAutoMono<AudioManager> { }
AudioManager.Instance.PlaySound("bgm");
```

### 3. 创建启动场景

挂载必要的 Manager（详见 [初始化与配置指南](Docs/初始化与配置指南.md)）：

- **MonoManager**（手动挂载 `SingletonMono`）
- **PoolManager**、**TimerManager**、**InputManager**（自动创建）

### 4. 模块使用

```csharp
// 广播
BroadcastCenter.AddListener(MyEvent, OnMyEvent);
BroadcastCenter.Broadcast(MyEvent);

// 计时器
int id = TimerManager.Instance.CreateTimer(false, 3f, () => Debug.Log("Done"));

// 对象池
var obj = PoolManager.Instance.Spawn(PoolType.Effect, "Fire") as GameObject;

// 寻路
astarMono.RequestPath(targetPosition);
```

---

## 文档导航

| 文档 | 说明 |
|---|---|
| [框架总览](Docs/MyFramework-总览.md) | 完整的模块列表、Mermaid 依赖关系图、命名空间表 |
| [01~17 模块文档](Docs/) | 每个模块的详细文档：API 参考、代码示例、注意事项 |
| [API 快速参考](Docs/API-调用快速参考.md) | 所有模块最常用 API 的一页速查表 |
| [初始化与配置指南](Docs/初始化与配置指南.md) | 场景搭建、Manager 初始化顺序、宏配置、编辑器工具 |

---

## 项目结构

```
MyFramework/
├── AI/GOAP/           # GOAP AI 系统（15 文件）
├── AssetLoad/         # 资源加载（AA/AB/Resources/Editor/UWQ）
├── Attribute/         # 编辑器属性（SceneName 选择器）
├── Broadcast/         # 广播系统（事件总线）
├── Camera/            # 相机管理（Cinemachine）
├── Core/              # 核心：单例基类 + MonoManager
├── CustomPhysics/2D/  # 自定义 2D 物理模拟
├── Debuger/           # 条件编译日志系统
├── Entity/            # 组件化实体
├── Input/             # 输入系统（新版+旧版）
├── PathFinding/AStar/ # Burst Job A* 寻路
├── Pool/              # 对象池（自动释放+预创建+数据池）
├── Save/              # JSON 加密存档
├── Scene/             # Addressables 场景管理
├── Stat/              # 数值系统（叠加/加乘/累乘）
├── Timer/             # 计时器管理
├── UI/                # UI 基类（自动绑定+事件分发）
└── Util/              # 工具类（JSON/加密/路径/随机/数学/文本/DOTween）
```

---

## 依赖

| 依赖 | 说明 |
|---|---|
| **Unity 2021.3+** | 推荐版本 |
| **Unity Input System** | 新版输入系统使用 |
| **Addressables** | 资源加载和场景管理使用 |
| **Cinemachine** | 相机管理使用 |
| **Burst** | A* 寻路 Job 编译加速使用 |
| **Collections** | A* 寻路 NativeArray 使用 |
| **DOTween** | DOTWeenUtil 使用（可选） |
| **Odin Inspector** | 部分模块使用 Odin 属性美化 Inspector（可选） |

---

## License

MIT License
