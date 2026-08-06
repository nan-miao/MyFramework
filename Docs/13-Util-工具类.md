# Util 工具类

框架辅助工具集，包含 JSON 序列化、加密、路径处理、随机数、数学工具、文本处理、DOTween 辅助、二进制数据管理和 LitJson 库。

**命名空间**: `MyFramework.Util` / `MyFramework.Util.Json` / `MyFramework.Util.Binary`

---

## 1. JsonManager — JSON 序列化管理器

单例 `JsonManager.Instance`，内部封装 LitJson 和 JsonUtility 两种方案，支持加密存储。

```csharp
using MyFramework.Util.Json;

// 简单保存/加载（自动拼接路径）
JsonManager.Instance.SaveData(myData, "player");
var data = JsonManager.Instance.LoadData<PlayerData>("player");

// 指定完整路径
JsonManager.Instance.CustomSaveData(myData, Application.persistentDataPath + "/player.sav");
var data = JsonManager.Instance.CustomLoadData<PlayerData>(path);

// 加密保存/加载
JsonManager.Instance.CustomSaveDataEncrypted(myData, path, "MySecretKey");
var data = JsonManager.Instance.CustomLoadDataEncrypted<PlayerData>(path, "MySecretKey");
```

---

## 2. EncryptionUtil — 加密工具

### 字符串加密（用于 JSON 存档）

XOR + Base64 编码：

```csharp
string encrypted = EncryptionUtil.EncryptString("原始数据", "MySecretKey");
string decrypted = EncryptionUtil.DecryptString(encrypted, "MySecretKey");
```

### 数值加密（内存防作弊）

基于随机密钥的 int/long 数值加密：

```csharp
int key = EncryptionUtil.GetRandomKey();

int lockedGold = EncryptionUtil.LockValue(1000, key);        // 加密
int unlockedGold = EncryptionUtil.UnlockValue(lockedGold, key); // 解密 → 1000

long locked = EncryptionUtil.LockValue(999999L, key);
long unlocked = EncryptionUtil.UnlockValue(locked, key);
```

### 密钥生成

```csharp
int intKey = EncryptionUtil.GenerateIntKey("MySecretKey"); // 字符串 → 数值密钥
```

---

## 3. PathUtil — 路径工具

```csharp
using MyFramework.Util;

// 预定义路径常量（避免 Application.xxxPath 每次产生 GC）
string assetsPath = PathUtil.AssetsPath;
string buildResPath = PathUtil.BuildResourcesPath;
string bundleOutPath = PathUtil.BundleOutPath;
string saveDataPath = PathUtil.SaveDataPath;

// 获取 Unity 相对路径
string relative = PathUtil.GetUnityPath(@"C:\Project\Assets\Prefabs\Player.prefab");
// → "Assets/Prefabs/Player.prefab"

// 标准化路径（统一 / 分隔符）
string standard = PathUtil.GetStandardPath(@"Assets\Prefabs\Player.prefab");
// → "Assets/Prefabs/Player.prefab"
```

---

## 4. RandomUtility — 随机工具

按模块分离的确定性随机数系统，支持状态保存和恢复。

```csharp
// 按模块获取 RNG 实例（模块独立，互不影响）
var gameplayRng = RandomUtility.Gameplay;
var procGenRng = RandomUtility.ProcGen;
var dropRng = RandomUtility.Drop;
var combatRng = RandomUtility.Combat;
var aiRng = RandomUtility.AI;
var visualRng = RandomUtility.Visual;
var mapRng = RandomUtility.Map;

// 返回值 + 调用计数
// 支持保存和恢复随机状态
```

---

## 5. MathUtil — 数学工具

```csharp
// 角度/弧度转换
// XZ 平面 / XY 平面距离计算
// 屏幕可见性检测
// 扇形范围检测
// 射线检测和重叠检测
```

---

## 6. TextUtil — 文本工具

```csharp
// 键值连接/分割（7 种分隔符类型）
// 数字格式化
// 时间转换：秒数 → H:M:S
```

---

## 7. DOTWeenUtil — DOTween 辅助

```csharp
// Scale 动画（放大/缩小）
// 旋转动画
// 清理方法
```

---

## 8. BinaryDataManager — 二进制数据管理

适用于非 JSON 格式的数据存储和 Excel 导出的数据表。

```csharp
// 二进制文件读写
// BinaryFormatter 序列化
// BitConverter 手动解析数据表
```

---

## 9. LitJson 库

完整的 JSON 库实现（9 个文件），提供 JsonReader/JsonWriter/JsonMapper/JsonData 等全套功能。

**文件**: `Util/Json/LitJson/*.cs`

---

## 10. SOUtil（Editor）

编辑器工具，查找所有 ScriptableObject 实例：

```csharp
#if UNITY_EDITOR
// 在 AssetDatabase 中查找指定类型的所有 SO 实例
#endif
```

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[07-Save-存档系统]]
