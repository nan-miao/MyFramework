# Input 输入系统

MyFramework 提供两套输入方案：**新版 InputSystem**（推荐，基于 Unity Input System Package）和**旧版 InputManager**（基于 Unity 旧 Input API + 广播系统）。

---

## 新版 InputSystem（推荐）

**管理器**: `InputManager`（继承 `SingletonAutoMono`，自动创建）

**命名空间**: `MyFramework.Input.NewSystem`

### 前置准备

1. 安装 Unity Input System Package
2. 创建 `InputActionAsset`（如 `GameInput.inputactions`）并配置 ActionMap 和 InputAction
3. 在枚举中注册对应类型（名称需与 InputActionAsset 中的 Action/ActionMap 一致）：

```csharp
// InputActionType.cs
public enum InputActionType
{
    LeftMouseClick,      // 鼠标左键点击
    MiddleMouseDrag,     // 鼠标中键拖拽
    RotateCamera,        // 旋转相机
}

// InputActionMapType.cs
public enum InputActionMapType
{
    GamePlay,
}
```

4. 确保 InputActionAsset 通过 Addressables 加载（默认 key: `"GameInput"`）

### 生命周期

```
OnStart() → LoadAsset() → RegisterInputAction(..., GamePlay)
                                → Enable ActionMap
                                    → 可接收输入回调

场景切换 → ReinitializeInputSystem() → 禁用所有 Map → 重新 Enable GamePlay

OnDestroy() → 清理所有回调 → Dispose 所有 Action → Release Addressables
```

### 注册输入事件

```csharp
// 注册事件（参数：事件类型, 所属 ActionMap）
// 内部通过枚举名转为字符串查找对应 Action
InputAction moveAction = InputManager.Instance.RegisterInputAction(
    InputActionType.LeftMouseClick, InputActionMapType.GamePlay);

// 获取已注册的事件（不常用）
InputAction action = InputManager.Instance.GetInputAction(InputActionType.LeftMouseClick);
```

### 添加/移除回调

```csharp
// started — 输入开始激活时（如按键按下瞬间）
InputManager.Instance.AddStartInputAction(InputActionType.LeftMouseClick, OnClickStarted);
InputManager.Instance.RemoveStartInputAction(InputActionType.LeftMouseClick, OnClickStarted);

// performed — 输入有效执行时（如按住满足阈值）
InputManager.Instance.AddPreformedInputAction(InputActionType.MiddleMouseDrag, OnDrag);

// canceled — 输入取消/结束时（如按键松开）
InputManager.Instance.AddCancelInputAction(InputActionType.MiddleMouseDrag, OnDragEnded);

void OnClickStarted(InputAction.CallbackContext ctx) { }
void OnDrag(InputAction.CallbackContext ctx) { }
void OnDragEnded(InputAction.CallbackContext ctx) { }
```

内部维护三个回调字典（`_startCallbacks`、`_performedCallbacks`、`_canceledCallbacks`），支持精确添加/移除。

### 切换 ActionMap

常用于打开 UI 时切换到 UI ActionMap，关闭 GamePlay 的输入：

```csharp
InputManager.Instance.SwitchInputActionMap(InputActionMapType.UI);
// 自动禁用其他已 Enable 的 ActionMap
```

### 注销与清理

```csharp
// 仅禁用（方便后续快速重新启用，不释放资源）
InputManager.Instance.UnregisterInputAction(InputActionType.LeftMouseClick, onlyDisable: true);

// 完全注销（清理回调 + Dispose Action + 释放资源）
InputManager.Instance.UnregisterInputAction(InputActionType.LeftMouseClick, onlyDisable: false);

// 重新初始化（场景切换后调用）
InputManager.Instance.ReinitializeInputSystem();

// 清空所有回调（不释放 Action）
InputManager.Instance.ClearAllInputCallbacks();
```

---

## 旧版 InputManager

**管理器**: `InputMgr`（继承 `BaseManager`，纯静态单例）

**命名空间**: `MyFramework.Input.OldSystem`

旧版系统基于 Unity 旧 Input API（`Input.GetKeyDown` 等），通过 `BroadcastCenter` 广播事件。适用于简单场景或旧项目兼容。

### 核心流程

1. 通过 `ChangeKeyboardInfo` / `ChangeMouseInfo` 配置按键映射
2. 通过 `MonoManager.AddUpdateListener` 每帧检测输入
3. 检测到输入后通过 `BroadcastCenter.Broadcast(eventType)` 发送广播

### 示例

```csharp
// 配置键盘按键映射
InputMgr.Instance.ChangeKeyboardInfo(
    BroadcastEventType.EndWave,     // 事件类型
    KeyCode.Space,                   // 按键
    InputInfo.E_InputType.Down      // 输入类型
);

// 配置鼠标按键映射
InputMgr.Instance.ChangeMouseInfo(
    BroadcastEventType.EndAllWave,  // 事件类型
    0,                                // 鼠标按键（0=左键, 1=右键, 2=中键）
    InputInfo.E_InputType.Down
);

// 开启输入检测（注册到 MonoManager）
InputMgr.Instance.StartOrCloseInputMgr(true);

// 在对应处监听广播
BroadcastCenter.AddListener(BroadcastEventType.EndWave, HandleEndWave);
```

### InputInfo 输入类型

```csharp
public enum E_InputType
{
    Down,   // 按下
    Up,     // 抬起
    Always  // 长按（持续检测）
}

public enum E_KeyOrMouse
{
    Key,    // 键盘
    Mouse   // 鼠标
}
```

### 改建支持

```csharp
// 获取下一次按键输入（用于改建 UI）
InputMgr.Instance.GetInputInfo((inputInfo) =>
{
    Debug.Log($"检测到按键: {inputInfo.key}, 类型: {inputInfo.inputType}");
    // 根据获取到的按键信息更新配置
});
```

---

## 新旧系统对比

| 特性 | 新版 InputSystem | 旧版 InputManager |
|---|---|---|
| 依赖 | Unity Input System Package | Unity 旧 Input API |
| 创建方式 | `SingletonAutoMono` 自动创建 | `BaseManager` 纯静态 |
| 配置方式 | InputActionAsset 可视化 | 代码中 ChangeKeyboardInfo/ChangeMouseInfo |
| 多设备支持 | 原生键盘+手柄+触屏 | 仅键盘+鼠标 |
| 改建 | InputActionAsset binding 修改 | `GetInputInfo` + 修改映射 |
| ActionMap 切换 | 内置支持 | 不支持，需手动管理 |
| 资源加载 | Addressables 加载 Asset | 无外部依赖 |
| 推荐度 | 新项目推荐 | 旧项目兼容/简单场景 |

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[02-Broadcast-广播系统]]（旧输入系统通过 Broadcast 发事件）
- [[01-Core-核心与单例]]
