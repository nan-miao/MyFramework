# Debuger 日志系统

条件编译日志系统，支持带颜色标签的输出、日志文件存储（后台线程写入）、通过 `OPEN_LOG` 宏控制编译剔除。

**命名空间**: `MyFramework.Debuger`

---

## 核心组件

| 组件 | 类型 | 说明 |
|---|---|---|
| `DebugLogger` | 静态类 | 日志输出 API，所有方法用 `[Conditional("OPEN_LOG")]` 标记 |
| `LogConfig` | ScriptableObject | 配置文件（`CreateAssetMenu: Data/Debuger/LogConfig`） |
| `LogSystem` | `SingletonAutoMono` | 初始化日志系统，控制 Unity Log 开关 |
| `LogColor` | 枚举 | 日志颜色枚举 |
| `UnityLogHelper` | 静态类 | 后台线程日志文件写入（`ConcurrentQueue` + `ManualResetEvent`） |

---

## 初始化

```csharp
// 使用默认配置
DebugLogger.InitLog();

// 使用自定义配置
var cfg = Resources.Load<LogConfig>("MyLogConfig");
DebugLogger.InitLog(cfg);
```

`LogSystem` 挂载后在 `Awake()` 中自动调用 `InitLog()`。若未定义 `OPEN_LOG`，会执行 `Debug.unityLogger.logEnabled = false` 关闭 Unity 日志。

---

## API

### 普通日志

```csharp
DebugLogger.Log("Hello World!");
DebugLogger.Log("血量: {0}, 等级: {1}", 100, 5);
DebugLogger.LogWarning("警告信息");
DebugLogger.LogError("错误信息");
```

### 颜色日志

```csharp
// 通用方法
DebugLogger.LogColor(LogColor.Red, "红色日志");

// 快捷方法
DebugLogger.LogGreen("绿色");
DebugLogger.LogYellow("黄色");
DebugLogger.LogRed("红色");
DebugLogger.LogOrange("橙色");
DebugLogger.LogBlue("蓝色");
DebugLogger.LogMagenta("洋红色");
DebugLogger.LogCyan("青色");
DebugLogger.LogDarkBlue("深蓝色");
DebugLogger.LogGrey("灰色");
```

### LogColor 枚举

```csharp
public enum LogColor
{
    None, Blue, Cyan, Darkblue, Green, Grey,
    Orange, Red, Yellow, Magenta
}
```

---

## 配置说明

LogConfig（ScriptableObject）支持以下配置：

| 配置项 | 说明 | 默认值 |
|---|---|---|
| `openLog` | 是否开启日志输出 | true |
| `logHeadFix` | 日志前缀 | "###" |
| `openTime` | 是否显示时间戳 | true |
| `showThreadID` | 是否显示线程 ID | false |
| `logSave` | 是否存储日志文件 | true |
| `showColorName` | 是否在日志中显示颜色名称 | false |

**日志文件存储路径**: `Application.persistentDataPath/Log/<产品名> yyyy-MM-dd HH-mm.log`

---

## 编译剔除

所有 `DebugLogger` 方法都使用 `[Conditional("OPEN_LOG")]` 标记：

- **定义 `OPEN_LOG`** → 日志生效（开发/测试阶段）
- **未定义 `OPEN_LOG`** → 所有日志调用代码在编译时完全剔除，零性能损耗 + 零包体增加

在 `ScriptingDefineSymbols` 中通过编辑器菜单"打开日志系统"配置。

---

## 日志文件写入（UnityLogHelper）

使用**后台线程**写入，避免主线程 IO 阻塞：

- `ConcurrentQueue<LogData>` 缓存日志
- `ManualResetEvent` 通知后台线程写入
- 支持的日志类型：Log、Warning、Error（含调用栈）

---

## 注意事项

- **新建场景需初始化**：确保 `LogSystem` 已挂载或手动调用 `DebugLogger.InitLog()`。
- **文件存储路径权限**：确保 `Application.persistentDataPath/Log/` 目录可写入。
- **编辑器菜单**：「打开日志系统」在场景中创建 Reporter 并设置 LogConfig。

---

## 相关文档

- [[MyFramework-总览|返回总览]]
- [[01-Core-核心与单例]]
- [[初始化与配置指南]]
