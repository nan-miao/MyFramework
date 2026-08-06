# Scene 场景管理

基于 Addressables 的异步场景加载系统，支持场景加载/卸载、批量切换和进度查询。

**管理器**: `SceneLoader`（继承 `BaseManager`，纯静态单例）

**命名空间**: `MyFramework.Scene.AA`

---

## API

### 异步加载场景

```csharp
using MyFramework.Scene.AA;
using UnityEngine.SceneManagement;

// 加载场景（场景名支持格式: "资源名|场景编号"）
SceneLoader.Instance.LoadSceneAsync("MainScene", LoadSceneMode.Single, (handle) =>
{
    if (handle.Status == AsyncOperationStatus.Succeeded)
    {
        Debug.Log("场景加载完成");
    }
});

// Additive 模式
SceneLoader.Instance.LoadSceneAsync("UIScene", LoadSceneMode.Additive, null);
```

> 场景名格式 `"资源名|场景编号"` 按 `|` 分割取资源名。

### 卸载场景

```csharp
// 立即卸载
SceneLoader.Instance.UnLoadSceneAsync("UIScene", (handle) =>
{
    Debug.Log("已卸载");
}, unloadImmediately: true);

// 延迟卸载（放入待卸载队列）
SceneLoader.Instance.UnLoadSceneAsync("UIScene", null, unloadImmediately: false);

// 统一卸载待卸载队列中所有场景
SceneLoader.Instance.ReleaseNeedUnloadScene();
```

### 批量操作

```csharp
// 自动卸载不在 keepScenes 列表中的场景
var keepScenes = new List<string> { "MainScene", "UIScene" };
SceneLoader.Instance.AutoUnloadScene(keepScenes, (handle) => { });

// 切换场景（卸载所有当前场景 + 加载新场景列表）
var newSetting = new LoadSceneSetting { /* 配置 */ };
SceneLoader.Instance.SwitchSceneAsync(newSetting);

// 重置（清空已加载和待卸载记录）
SceneLoader.Instance.Reset();
```

### 加载进度

```csharp
float progress = SceneLoader.Instance.GetLoadProgress("MainScene");
// 返回 0~1 的下载进度
```

---

## SceneName — 场景选择器

`SceneName` 是一个 Unity 序列化友好的结构体，在 Inspector 中自动列出所有 Build Settings 中的场景：

```csharp
using MyFramework.Scene;

[SerializeField] private SceneName mainScene; // Inspector 下拉选择

// 隐式转换
string sceneStr = mainScene;   // SceneName → string
SceneName sn = "MainScene";    // string → SceneName
```

---

## SceneLoadTrigger2D — 2D 场景触发器

挂载在 Collider2D 上，玩家进入触发器时自动加载对应场景：

```csharp
// 通过 LevelType 和 SceneNumber 指定目标场景
// 支持 5 个章节级别
```

---

## 内部状态管理

```
loadedSceneDic:      已加载的场景列表（避免重复加载）
needUnloadSceneDic:  待卸载场景队列（延迟卸载）
```

- 已加载过的场景不会重复加载
- 待卸载队列中的场景可以重新激活（移回 loadedSceneDic）
- 场景卸载通过 Addressables 完成，不销毁已实例化的 GameObject（引用计数 -1）

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[08-AssetLoad-资源加载]]
- [[01-Core-核心与单例]]
