using System;
using System.IO;
using UnityEngine;

namespace MyFramework.Debuger
{
    [CreateAssetMenu(fileName = "LogConfig", menuName = "Data/Debuger/LogConfig")]
    public class LogConfig :ScriptableObject
    {
        public bool openLog = true; //NOTE::是否开启日志系统

        public string logHeadFix = "###"; //NOTE::日志前缀

        public bool openTime = true;//NOTE::是否显示时间

        public bool showThreadID = false;//NOTE::是否显示线程id

        public bool logSave = true;//NOTE::日志文件储存开关

        public bool showColorName = false;//NOTE::是否显示颜色名称
    
        public static string logFileSavePath{get{string path = Path.Combine(Application.persistentDataPath, "Log");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path + "/";}}//NOTE::文件储存路径
   
        //NOTE::日志文件名称
        public string logFileName
        {
            get { return Application.productName + " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm") + ".log"; }
        }
    }
}