# Save 存档系统

JSON 存档系统，支持无加密/加密两种存储方式，通过泛型 `SaveContainer` 容器管理存档数据，支持挂载到场景 GameObject 上通过 `ISaveData` 接口统管。

**命名空间**: `MyFramework.Save`

---

## 核心架构

```
SaveConfig (ScriptableObject)
    └── List<SaveSetting> (每个定义: 文件名/类型/加密开关/密钥)
         └── SaveContainer<TData, TContainer> (泛型存档容器, 继承 BaseManager)
              └── ISaveData (挂载到场景 GameObject 的接口)
                   └── SaveManager (场景级单例, 统管所有 ISaveData)
```

### 关键类型

| 类型 | 说明 |
|---|---|
| `SaveSetting` | 存档配置结构体: fileName、saveType、useEncryption、encryptionKey |
| `SaveContainerType` | 枚举: `Global`（全局设置） / `Player`（玩家存档） |
| `SaveContainer<TData, TContainer>` | 泛型存档容器基类: 自动从 SaveConfig 同步加密配置 |
| `ISaveContainer` | 容器接口: 定义 SaveData() / LoadData() |
| `ISaveData` | 场景对象接口: 实现此接口会被 `FindObjectsOfType` 自动发现 |
| `SaveManager` | 场景管理器 (`SingletonMono`): 统一保存/加载 |

---

## 使用步骤

### Step 1: 创建 SaveConfig

在 Unity 中右键 `Assets → Create → Data → SO → GlobalConfig → SaveConfig`，配置 `saveSettings` 列表：

```
fileName: "global_save.dat"
saveType: Global
useEncryption: true
encryptionKey: "MySecretKey123!"
```

### Step 2: 创建存档容器

```csharp
using System;
using MyFramework.Save;

public class GlobalSaveContainer : SaveContainer<GlobalSaveContainer.GlobalSaveData, GlobalSaveContainer>, ISaveContainer
{
    [Serializable]
    public struct GlobalSaveData
    {
        public int gold;
        public int highScore;
        public string playerName;
        public DateTime lastLoginTime;
    }
}
```

`Init()` 方法会自动从 `SaveConfig.saveSettings` 中查找对应类型的配置，同步 `fileName`、`useEncryption`、`encryptionKey`。

### Step 3: 挂载 ISaveData 到场景

```csharp
public class PlayerStat : MonoBehaviour, ISaveData
{
    public ISaveContainer SaveDataContainer => GlobalSaveContainer.Instance;

    public int gold = 9999;
    public int highScore = 12345;

    [ContextMenu("修改并保存数据")]
    public void SaveData()
    {
        var saveData = GlobalSaveContainer.Instance.data;
        saveData.gold = gold;
        saveData.highScore = highScore;
        GlobalSaveContainer.Instance.data = saveData;
    }

    [ContextMenu("加载数据")]
    public void LoadData()
    {
        var data = GlobalSaveContainer.Instance.data;
        gold = data.gold;
        highScore = data.highScore;
        Debug.Log($"金币: {data.gold}, 最高分: {data.highScore}");
    }
}
```

### Step 4: 全局保存/加载

```csharp
// 保存所有 ISaveData（遍历场景中所有 ISaveData，逐个调用 SaveData）
SaveManager.Instance.SaveGame();

// 加载所有 ISaveData
SaveManager.Instance.LoadGame();

// 保存指定容器
SaveManager.Instance.SaveAssignGameData(GlobalSaveContainer.Instance);

// 加载指定容器
SaveManager.Instance.LoadAssignGameData(GlobalSaveContainer.Instance);
```

---

## 加密存档

基于 `EncryptionUtil.EncryptString` / `DecryptString` 的 XOR 加密 + Base64 编码。

### 自动加密（SaveContainer 内部）

`SaveContainer` 在 `SaveData()`/`LoadData()` 时自动检查 `useEncryption` 和 `encryptionKey`，加密时调用 `JsonManager` 的加密方法。

### 手动加密（JsonManager）

```csharp
// 加密保存
JsonManager.Instance.CustomSaveDataEncrypted(data, filePath, "MySecretKey");

// 加密加载
var loaded = JsonManager.Instance.CustomLoadDataEncrypted<T>(filePath, "MySecretKey");

// 密钥不匹配 → 解密得乱码 → 反序列化失败 → 返回 new T()
```

---

## 完整工作流程

```
场景启动
    ↓
SaveManager.OnStart()
    ├── FindAllSaveDataOnScene() → 找到所有 ISaveData
    │
    ▼
GlobalSaveContainer.Init()
    ├── 从 SaveConfig.saveSettings 查找 saveType == Global 的配置
    ├── 同步 fileName = "global_save.dat"
    ├── 同步 useEncryption = true
    └── 同步 encryptionKey = "MySecretKey123!"

LoadGame()
    └── 遍历所有 ISaveData → LoadData()
        ├── useEncryption && key 非空?
        │   是 → JsonManager.CustomLoadDataEncrypted<T>(path, key)
        │        └── EncryptionUtil.DecryptString(encryptedJson, key)
        │            └── JsonMapper.ToObject<T>(decryptedJson)
        │   否 → JsonManager.CustomLoadData<T>(path)

SaveGame()
    └── 遍历所有 ISaveData → SaveData()
        ├── useEncryption && key 非空?
        │   是 → JsonMapper.ToJson(data)
        │        └── EncryptionUtil.EncryptString(json, key)
        │            └── File.WriteAllText(path, encryptedStr)
        │   否 → File.WriteAllText(path, json)
```

---

## 存档文件格式对比

```
# 不加密 (player_save.json)
{"gold":9999,"highScore":12345,"playerName":"Player1"}

# 加密后 (global_save.dat) — Base64 编码的密文
WzE3MiwxNzldWzE3NCwxNzddWzE2NS...
```

---

## 注意事项

- **ISaveData 自动发现**: `SaveManager.OnStart()` 通过 `FindObjectsOfType` 查找场景中所有 `ISaveData`，无需手动注册。
- **容器是全局单例**: `SaveContainer` 继承 `BaseManager`，天然全局。
- **存档路径**: `Application.persistentDataPath`。
- **security 提示**: 加密算法为 XOR + Base64，可防普通玩家修改存档但非强加密方案。
- **SaveConfig 可视化配置**: 在 ScriptableObject 上集中管理所有存档配置。
- **多容器支持**: 可创建多个 `SaveContainer` 子类（如玩家存档、系统设置），按 `SaveContainerType` 区分。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[13-Util-工具类]]（JsonManager、EncryptionUtil）
- [[01-Core-核心与单例]]
