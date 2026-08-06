using UnityEngine;

namespace MyFramework.Util
{
    public class PathUtil
    {
        //Application访问路径时会产生GC，这样把目录定义出来可以减少GC  
        //根目录  
        public static readonly string AssetsPath = Application.dataPath;

        //需要打bundle的目录  
        public static readonly string BuildResourcesPath = Application.dataPath + "/BuildResources/";

        //bundle输出目录  
        public static readonly string BundleOutPath = Application.streamingAssetsPath;

        public static readonly string SaveDataPath = Application.persistentDataPath;

        /// <summary>
        ///     获取Unity的相对路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetUnityPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return path.Substring(path.IndexOf("Assets")); //一条完整的磁盘绝对路径裁剪成 Unity 工程里的“相对路径”。找到第一次出现 "Assets" 的位置后截取  
        }

        /// <summary>
        ///     获取标准路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetStandardPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return path.Trim().Replace("\\", "/");
        }
    }
}