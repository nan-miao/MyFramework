# AssetLoad 资源加载

MyFramework 提供五种资源加载方案，覆盖开发期与发布期的不同场景。所有加载器均使用引用计数管理资源生命周期。

**命名空间**: `MyFramework.AssetLoad` / `MyFramework.AssetLoad.AA` / `MyFramework.AssetLoad.AB`

---

## 方案对比

| 方案 | 管理器 | 创建方式 | 适用阶段 | 加载来源 |
|---|---|---|---|---|
| Addressables | `AddressablesManager` | `BaseManager` | 发布期（推荐） | Addressables |
| AssetBundle | `ABManager` / `ABResManager` | `SingletonAutoMono` | 发布期（传统） | 打包 AB 文件 |
| Resources | `ResManager` | `BaseManager` | 开发/发布 | Resources 目录 |
| Editor | `EditorResManager` | `BaseManager` | 仅 Editor | `Assets/Editor/ArtRes/` |
| UnityWebRequest | `UWQResManager` | `SingletonAutoMono` | 特殊用途 | 远程 URL/本地文件 |

---

## 1. Addressables（推荐）

**管理器**: `AddressablesManager`（继承 `BaseManager`，纯静态单例）

### 单资源加载

```csharp
using MyFramework.AssetLoad.AA;

// 通过资源名称异步加载
AddressablesManager.Instance.LoadAssetAsync<GameObject>("MyPrefab", (handle) =>
{
    if (handle.Status == AsyncOperationStatus.Succeeded)
    {
        GameObject prefab = handle.Result;
        Instantiate(prefab);
    }
});

// 通过 GUID + AssetReference 加载
AddressablesManager.Instance.LoadAssetAsync<Texture2D>(guid, assetRef, (handle) =>
{
    texture = handle.Result;
});

// 释放（引用计数 -1，归零时才真正释放）
AddressablesManager.Instance.Release<GameObject>("MyPrefab");

// 强制释放（无视引用计数）
AddressablesManager.Instance.Release<GameObject>("MyPrefab", force: true);
```

### 批量加载

```csharp
// MergeMode.Union = 并集，Intersection = 交集
AddressablesManager.Instance.LoadAssetAsync<GameObject>(
    Addressables.MergeMode.Union,
    (obj) => { /* 每加载一个调用一次 */ },
    "Enemies", "Players"
);

// 批量释放
AddressablesManager.Instance.Release<GameObject>("Enemies", "Players");
```

### 注意事项
- **引用计数**: 每次 Load +1，Release -1，归零才真正释放。
- **材质引用**: 释放可能导致非实例化材质显示异常。
- **场景资源**: 场景资源也可释放，不影响已加载场景（场景本质是配置文件）。

---

## 2. AssetBundle

**管理器**: `ABManager`（继承 `SingletonAutoMono`）

```csharp
using MyFramework.AssetLoad.AB;

// 同步加载
GameObject obj = ABManager.Instance.LoadRes<GameObject>("ui", "MainPanel");

// 异步加载（泛型）
ABManager.Instance.LoadResAsync<GameObject>("ui", "MainPanel", (res) =>
{
    Instantiate(res);
}, isSync: false);

// 异步加载（Type）
ABManager.Instance.LoadResAsync("ui", "MainPanel", typeof(GameObject), (res) => { });

// 卸载 AB 包
ABManager.Instance.UnLoadAB("ui", (success) =>
{
    Debug.Log(success ? "卸载成功" : "正在加载中，暂不可卸载");
});

// 清空所有 AB
ABManager.Instance.ClearAB();
```

支持 AssetBundleManifest 依赖解析，平台特定主包命名（Windows/Android/iOS）。

### ABResManager — 开发/发布切换

```csharp
// isDebug=true 时走 EditorResManager，发布后走 ABManager
ABResManager.Instance.LoadResAsync<GameObject>("ui", "MainPanel", callback);
```

---

## 3. Resources

**管理器**: `ResManager`（继承 `BaseManager`）

```csharp
using MyFramework.AssetLoad;

// 同步加载
var prefab = ResManager.Instance.Load<GameObject>("Prefabs/Player");

// 异步加载
ResManager.Instance.LoadAsync<GameObject>("Prefabs/Player", (res) =>
{
    Instantiate(res);
});

// 卸载（isDel=true 才真正卸载）
ResManager.Instance.UnloadAsset<GameObject>("Prefabs/Player", isDel: true);

// 卸载未使用资源
ResManager.Instance.UnloadUnusedAssets(() => Debug.Log("卸载完成"));

// 引用计数查询
int count = ResManager.Instance.GetRefCount<GameObject>("Prefabs/Player");
```

引用计数管理：`Load` +1，`UnloadAsset` -1，`isDel=true` 且归零时 `Resources.UnloadAsset`。

---

## 4. Editor（仅开发期）

**管理器**: `EditorResManager`（继承 `BaseManager`）

```csharp
#if UNITY_EDITOR
using MyFramework.AssetLoad;

// 从 Assets/Editor/ArtRes/ 加载，自动添加后缀
// GameObject→.prefab, Material→.mat, Texture→.png, AudioClip→.mp3
var prefab = EditorResManager.Instance.LoadEditorRes<GameObject>("ui/MainPanel");

// 加载图集
var sprites = EditorResManager.Instance.LoadSprites("ui/Icons");
#endif
```

---

## 5. UnityWebRequest

**管理器**: `UWQResManager`（继承 `SingletonAutoMono`）

```csharp
// 支持类型: string, byte[], Texture, AssetBundle
UWQResManager.Instance.LoadRes<Texture>(
    "https://example.com/texture.png",
    (texture) => { /* 成功 */ },
    () => { Debug.LogError("加载失败"); }
);
```

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
- [[09-Scene-场景管理]]
