using MyFramework.Core.Singleton;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFramework.Debuger
{
    public class LogSystem : SingletonAutoMono<LogSystem>
    {
        [LabelText("Log日志配置文件")]public LogConfig cfg;

        protected void Awake()
        {
#if OPEN_LOG
        DebugLogger.InitLog(cfg);
#else
            Debug.unityLogger.logEnabled=false; //NOTE::关闭Unity的Log系统
#endif
        }
    }
}
