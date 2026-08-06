using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MyFramework.Debuger
{
    public class DebugLogger : MonoBehaviour
    {
        public static LogConfig cfg;
    
        [Conditional("OPEN_LOG")]
        public static void InitLog(LogConfig _cfg = null)
        {
            if (_cfg==null)
            {
                cfg = new LogConfig();
            }
            else
            {
                cfg = _cfg;
            }

            if (cfg.logSave)
            {
                GameObject logObj = new GameObject("LogHelper");
                DontDestroyOnLoad(logObj);
                UnityLogHelper unityLogHelper = logObj.AddComponent<UnityLogHelper>();
                unityLogHelper.InitLogFileModule(LogConfig.logFileSavePath,cfg.logFileName);
            }
        
        }

        #region 普通日志

        [Conditional("OPEN_LOG")]
        public static void Log(object obj)
        {
            if (!cfg.openLog)
            {
                return;
            }
            string log= GenerateLog(obj.ToString());
            Debug.Log(log);
        }
    
        [Conditional("OPEN_LOG")]
        public static void Log(object obj,params object[] args)
        {
            if (!cfg.openLog)
            {
                return;
            }
            string conent =string.Empty;
            if (args != null)
            {
                foreach (var item in args)
                {
                    conent += item;
                }
            }
            string log= GenerateLog(obj+conent);
            Debug.Log(log);
        }
    
        [Conditional("OPEN_LOG")]
        public static void LogWarning(object obj)
        {
            if (!cfg.openLog)
            {
                return;
            }
            string log= GenerateLog(obj.ToString());
            Debug.LogWarning(log);
        }
    
        [Conditional("OPEN_LOG")]
        public static void LogWarning(object obj,params object[] args)
        {
            if (!cfg.openLog)
            {
                return;
            }
            string conent =string.Empty;
            if (args != null)
            {
                foreach (var item in args)
                {
                    conent += item;
                }
            }
            string log= GenerateLog(obj+conent);
            Debug.LogWarning(log);
        }
    
        [Conditional("OPEN_LOG")]
        public static void LogError(object obj)
        {
            if (!cfg.openLog)
            {
                return;
            }
            string log= GenerateLog(obj.ToString());
            Debug.LogError(log);
        }
    
        [Conditional("OPEN_LOG")]
        public static void LogError(object obj,params object[] args)
        {
            if (!cfg.openLog)
            {
                return;
            }
            string conent =string.Empty;
            if (args != null)
            {
                foreach (var item in args)
                {
                    conent += item;
                }
            }
            string log= GenerateLog(obj+conent);
            Debug.LogError(log);
        }

        #endregion

        #region 颜色日志打印
        [Conditional("OPEN_LOG")]
        public static void LogColor(LogColor color,object obj)
        {
            if (!cfg.openLog)
            {
                return;
            }
            string log= GenerateLog(obj.ToString(),color);
            log=GetUnityColor(log, color);
            Debug.Log(log);
        }
        [Conditional("OPEN_LOG")]
        public static void LogGreen(object msg)
        {
            LogColor(global::MyFramework.Debuger.LogColor.Green, msg);
        }
        [Conditional("OPEN_LOG")]
        public static void LogYellow(object msg)
        {
            LogColor(global::MyFramework.Debuger.LogColor.Yellow, msg);
        }
        [Conditional("OPEN_LOG")]
        public static void LogRed(object msg)
        {
            LogColor(global::MyFramework.Debuger.LogColor.Red, msg);
        }
        [Conditional("OPEN_LOG")]
        public static void LogOrange(object msg)
        {
            LogColor(global::MyFramework.Debuger.LogColor.Orange, msg);
        }
        [Conditional("OPEN_LOG")]
        public static void LogBlue(object msg)
        {
            LogColor(global::MyFramework.Debuger.LogColor.Blue, msg);
        }
        [Conditional("OPEN_LOG")]
        public static void LogMagenta(object msg)
        {
            LogColor(global::MyFramework.Debuger.LogColor.Magenta, msg);
        }
        #endregion
    
        public static string GenerateLog(string log,LogColor color = global::MyFramework.Debuger.LogColor.None)
        {
            StringBuilder stringBuilder = new StringBuilder(cfg.logHeadFix,100);
            if (cfg.openTime)
            {
                stringBuilder.AppendFormat("{0}  ",DateTime.Now.ToString("hh:mm:ss-fff"));
            }

            if (cfg.showThreadID)
            {
                stringBuilder.AppendFormat("ThreadID:{0}  ",Thread.CurrentThread.ManagedThreadId);
            }

            if (cfg.showColorName)
            {
                stringBuilder.AppendFormat("{0}  ",color.ToString());
            }
            stringBuilder.AppendFormat("{0}",log);
            return stringBuilder.ToString();
        }

        public static string GetUnityColor(string msg, LogColor color)
        {
            if (color == global::MyFramework.Debuger.LogColor.None)
            {
                return msg;
            }
        
            switch (color)
            {
                case global::MyFramework.Debuger.LogColor.Blue:
                    msg = $"<color=#0000FF>{msg}</color>";
                    break;
                case global::MyFramework.Debuger.LogColor.Cyan:
                    msg = $"<color=#00FFFF>{msg}</color>";
                    break;
                case global::MyFramework.Debuger.LogColor.Darkblue:
                    msg = $"<color=#8FBC8F>{msg}</color>";
                    break;
                case global::MyFramework.Debuger.LogColor.Green:
                    msg = $"<color=#00FF00>{msg}</color>";
                    break;
                case global::MyFramework.Debuger.LogColor.Orange:
                    msg = $"<color=#FFA500>{msg}</color>";
                    break;
                case global::MyFramework.Debuger.LogColor.Red:
                    msg = $"<color=#FF0000>{msg}</color>";
                    break;
                case global::MyFramework.Debuger.LogColor.Yellow:
                    msg = $"<color=#FFFF00>{msg}</color>";
                    break;
                case global::MyFramework.Debuger.LogColor.Magenta:
                    msg = $"<color=#FF00FF>{msg}</color>";
                    break;
            }
            return msg;
        }
    }
}
