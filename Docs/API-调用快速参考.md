# API 调用快速参考

每个模块最常用 API 的一页速查表。详细说明请参见各模块文档。

---

## Core — 单例与生命周期

```csharp
// 非 Mono 单例
public class GameManager : BaseManager<GameManager> { }

// Mono 单例（自动创建）
public class AudioManager : SingletonAutoMono<AudioManager> { }
protected override void OnStart() { base.OnStart(); }

// Mono 单例（手动挂载）
public class MyManager : SingletonMono<MyManager> { }

// 生命周期
MonoManager.Instance.AddUpdateListener(callback, order, frequency);
MonoManager.Instance.AddFixedUpdateListener(callback, order, frequency);
MonoManager.Instance.AddLateUpdateListener(callback, order, frequency);
MonoManager.Instance.RemoveUpdateListener(callback);
MonoManager.Instance.ClearAll();
```

---

## Broadcast — 广播系统

```csharp
BroadcastCenter.AddListener(eventType, callback);                 // 无参
BroadcastCenter.AddListener<T>(eventType, callback);              // 1 参
BroadcastCenter.AddListener<T,X>(eventType, callback);            // 2 参
BroadcastCenter.AddListener<T,X,Y>(eventType, callback);          // 3 参
BroadcastCenter.AddListener<T,X,Y,Z>(eventType, callback);        // 4 参

BroadcastCenter.Broadcast(eventType, args...);
BroadcastCenter.RemoveListener(eventType, callback);
```

---

## Timer — 计时器

```csharp
int id = TimerManager.Instance.CreateTimer(isRealTime, allTime, overCallBack,
                                            intervalTime, callBack);
TimerManager.Instance.StopTimer(id);
TimerManager.Instance.StartTimer(id);
TimerManager.Instance.ResetTimer(id);
TimerManager.Instance.RemoveTimer(id);
TimerManager.Instance.PauseAllTimers();
TimerManager.Instance.ResumeAllTimers();
TimerManager.Instance.ClearAllTimersSafe();
TimerManager.Instance.TriggerHitStop(duration, timeScale);
TimerManager.Instance.KillGameObject(go, delay);
```

---

## Pool — 对象池

```csharp
// GameObject 池
PoolManager.Instance.CreatePreloadPool(type, max, prefab, preloadCount);
GameObject obj = PoolManager.Instance.Spawn(type, name) as GameObject;
PoolManager.Instance.UnSpawn(type, name, obj);

// 数据池
T data = PoolManager.Instance.GetData<T>();
PoolManager.Instance.ReturnData(data);
PoolManager.Instance.HasDataPool<T>();
PoolManager.Instance.ClearDataPool<T>();

// 全局
PoolManager.Instance.ReleaseAll(force);
```

---

## Debuger — 日志系统

```csharp
DebugLogger.Log("msg");
DebugLogger.LogWarning("msg");
DebugLogger.LogError("msg");
DebugLogger.LogColor(LogColor.Red, "msg");
DebugLogger.LogGreen("msg");  // 及 Yellow/Red/Orange/Blue/Magenta/Cyan/DarkBlue/Grey
DebugLogger.InitLog(config);
```

---

## Input — 输入系统

```csharp
// 新版
InputAction action = InputManager.Instance.RegisterInputAction(type, mapType);
InputManager.Instance.AddStartInputAction(type, callback);
InputManager.Instance.AddPreformedInputAction(type, callback);
InputManager.Instance.AddCancelInputAction(type, callback);
InputManager.Instance.RemoveStartInputAction(type, callback);
InputManager.Instance.SwitchInputActionMap(mapType);
InputManager.Instance.ReinitializeInputSystem();

// 旧版
InputMgr.Instance.ChangeKeyboardInfo(eventType, keyCode, inputType);
InputMgr.Instance.ChangeMouseInfo(eventType, mouseBtn, inputType);
InputMgr.Instance.StartOrCloseInputMgr(true);
InputMgr.Instance.GetInputInfo(callback);
```

---

## Save — 存档系统

```csharp
// 容器定义
public class MyContainer : SaveContainer<MyContainer.Data, MyContainer>, ISaveContainer { }

// 场景接口
public class MySaver : MonoBehaviour, ISaveData { }

// 全局操作
SaveManager.Instance.SaveGame();
SaveManager.Instance.LoadGame();
SaveManager.Instance.SaveAssignGameData(container);
SaveManager.Instance.LoadAssignGameData(container);
```

---

## AssetLoad — 资源加载

```csharp
// Addressables
AddressablesManager.Instance.LoadAssetAsync<T>(name, callback);
AddressablesManager.Instance.Release<T>(name);
AddressablesManager.Instance.LoadAssetAsync<T>(MergeMode.Union, callback, tags...);

// AssetBundle
ABManager.Instance.LoadRes<T>(bundle, asset);
ABManager.Instance.LoadResAsync<T>(bundle, asset, callback, isSync: false);
ABManager.Instance.UnLoadAB(bundle, callback);

// Resources
ResManager.Instance.Load<T>(path);
ResManager.Instance.LoadAsync<T>(path, callback);
ResManager.Instance.UnloadAsset<T>(path, isDel: true);

// Editor (仅 UNITY_EDITOR)
EditorResManager.Instance.LoadEditorRes<T>(path);
```

---

## Scene — 场景管理

```csharp
SceneLoader.Instance.LoadSceneAsync(name, mode, callback);
SceneLoader.Instance.UnLoadSceneAsync(name, callback, unloadImmediately: true);
SceneLoader.Instance.AutoUnloadScene(keepList, callback);
SceneLoader.Instance.SwitchSceneAsync(setting);
float progress = SceneLoader.Instance.GetLoadProgress(name);
```

---

## Stat — 数值系统

```csharp
FloatStat hp = new FloatStat(100f);
hp.AddModifier(50f);
hp.RemoveModifier(50f);
hp.AddAddPercentModifier(0.2f);
hp.AddMultiplyPercentModifier(0.1f);
hp.RemoveMultiplyPercentModifier(0.1f);
float val = hp.GetValue();
hp.Reset();
hp.SetDefaultValue(100f);
```

---

## Entity — 实体组件

```csharp
// 实体
public class MyEntity : Entity { }

// 组件
public class MyComp : EntityComponentBase
{
    protected override void ChildOnUpdate(float dt) { }
    protected override void ChildOnFixeUpdate(float dt) { }
    protected override void ChildOnLateUpdate() { }
}

// 组件管理
entity.AddEntityComponent(comp);
entity.RemoveEntityComponent(comp);
entity.GetEntityComponent(type);
entity.HasComponentOfType(type);
```

---

## UI — 界面基类

```csharp
public class MyPanel : BasePanel
{
    public override void ShowMe() { }
    public override void HideMe() { }
    protected override void ClickBtn(string name) { }
    protected override void SliderValueChange(string name, float val) { }
    protected override void ToggleValueChange(string name, bool val) { }
    T GetControl<T>(string name) where T : UIBehaviour;
}
```

---

## GOAP — AI 系统

```csharp
GOAPAgent agent;
agent.Init(owner);
agent.OnUpdate();
agent.StopPlan();
agent.ResetStates();
```

---

## A* — 寻路系统

```csharp
// 实体寻路
AStarMono mono;
mono.RequestPath(target);
mono.moveSpeed = 5f;
mono.needMove = true;

// 障碍物
AStarManager.Instance.AddBlockedPosition(pos);
AStarManager.Instance.RemoveBlockedPosition(pos);
AStarManager.Instance.SetBlockedPositions(list);
AStarManager.Instance.SetConfig(cfg);

// 离线计算
AStarPathHelper.Instance.CalculatePath(ref pathInfo, callback);
```

---

## Camera — 相机管理

```csharp
CameraManager.Instance.SwapToCamera(cam);
CameraManager.Instance.ResetCameraToMap();
```

---

## CustomPhysics — 自定义物理

```csharp
PhysicsSimulator2D.Instance.AddPlatform(obj);
PhysicsSimulator2D.Instance.AddPlayer(obj);
PhysicsSimulator2D.Instance.RemovePlatform(obj);
PhysicsSimulator2D.Instance.RemovePlayer(obj);
```

---

## Util — 工具类

```csharp
// JSON
JsonManager.Instance.SaveData(data, name);
JsonManager.Instance.LoadData<T>(name);
JsonManager.Instance.CustomSaveDataEncrypted(data, path, key);
JsonManager.Instance.CustomLoadDataEncrypted<T>(path, key);

// 加密
EncryptionUtil.EncryptString(plain, key);
EncryptionUtil.DecryptString(encrypted, key);
EncryptionUtil.LockValue(value, key);
EncryptionUtil.UnlockValue(locked, key);

// 路径
PathUtil.AssetsPath / BuildResourcesPath / BundleOutPath / SaveDataPath;
PathUtil.GetUnityPath(absPath);
PathUtil.GetStandardPath(path);

// 随机
var rng = RandomUtility.Gameplay; // 等模块
```

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[初始化与配置指南]]
