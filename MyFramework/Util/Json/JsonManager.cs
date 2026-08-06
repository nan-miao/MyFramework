using System;
using System.IO;
using LitJson;
using UnityEngine;

namespace MyFramework.Util.Json
{
    /// <summary>
    ///     序列化和反序列化Json时  使用的是哪种方案
    /// </summary>
    public enum JsonType
    {
        JsonUtlity,
        LitJson
    }

    /// <summary>
    ///     Json数据管理类 主要用于进行 Json的序列化存储到硬盘 和 反序列化从硬盘中读取到内存中
    /// </summary>
    public class JsonManager
    {
        private JsonManager()
        {
        }

        public static JsonManager Instance { get; } = new();

        //存储Json数据 序列化
        public void SaveData(object data, string fileName, JsonType type = JsonType.LitJson)
        {
            //确定存储路径
            var path = Application.persistentDataPath + "/" + fileName + ".json";
            //序列化 得到Json字符串
            var jsonStr = SerializeToString(data, type);

            //把序列化的Json字符串 存储到指定路径的文件中
            File.WriteAllText(path, jsonStr);
        }

        //传入整个路径包含文件后缀 以实现自定义后缀的json文件
        public void CustomSaveData(object data, string filePath, JsonType type = JsonType.LitJson)
        {
            //序列化 得到Json字符串
            var jsonStr = SerializeToString(data, type);

            //验证并确保目录存在
            ValidateAndEnsureDirectory(filePath);

            //WriteAllText完全覆盖写入
            //把序列化的Json字符串 存储到指定路径的文件中
            File.WriteAllText(filePath, jsonStr);
        }

        /// <summary>
        ///     存储加密的Json数据
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <param name="filePath">完整文件路径</param>
        /// <param name="encryptionKey">加密密钥</param>
        /// <param name="type">Json序列化方案</param>
        public void CustomSaveDataEncrypted(object data, string filePath, string encryptionKey,
            JsonType type = JsonType.LitJson)
        {
            //序列化 得到Json字符串
            var jsonStr = SerializeToString(data, type);

            //加密Json字符串
            var encryptedStr = EncryptionUtil.EncryptString(jsonStr, encryptionKey);

            //验证并确保目录存在
            ValidateAndEnsureDirectory(filePath);

            //把加密后的字符串 存储到指定路径的文件中
            File.WriteAllText(filePath, encryptedStr);
        }

        //读取指定文件中的 Json数据 反序列化
        public T LoadData<T>(string fileName, JsonType type = JsonType.LitJson) where T : new()
        {
            //确定从哪个路径读取
            //首先先判断 默认数据文件夹中是否有我们想要的数据 如果有 就从中获取
            var path = Application.streamingAssetsPath + "/" + fileName + ".json";
            //先判断 是否存在这个文件
            //如果不存在默认文件 就从 读写文件夹中去寻找
            if (!File.Exists(path))
                path = Application.persistentDataPath + "/" + fileName + ".json";
            //如果读写文件夹中都还没有 那就返回一个默认对象
            if (!File.Exists(path))
                return new T();

            //进行反序列化
            var jsonStr = File.ReadAllText(path);
            //把对象返回出去
            return DeserializeFromString<T>(jsonStr, type);
        }

        public T CustomLoadData<T>(string filePath, JsonType type = JsonType.LitJson) where T : new()
        {
            var path = filePath;
            //如果文件不存在 那就返回一个默认对象
            if (!File.Exists(path))
                return new T();

            //进行反序列化
            var jsonStr = File.ReadAllText(path);
            //把对象返回出去
            return DeserializeFromString<T>(jsonStr, type);
        }

        /// <summary>
        ///     读取加密的Json数据并反序列化
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="filePath">完整文件路径</param>
        /// <param name="encryptionKey">解密密钥</param>
        /// <param name="type">Json序列化方案</param>
        /// <returns>反序列化后的对象</returns>
        public T CustomLoadDataEncrypted<T>(string filePath, string encryptionKey, JsonType type = JsonType.LitJson)
            where T : new()
        {
            var path = filePath;
            //如果文件不存在 那就返回一个默认对象
            if (!File.Exists(path))
                return new T();

            //读取加密字符串
            var encryptedStr = File.ReadAllText(path);

            //解密得到Json字符串
            var jsonStr = EncryptionUtil.DecryptString(encryptedStr, encryptionKey);

            //反序列化并返回
            return DeserializeFromString<T>(jsonStr, type);
        }

        #region Private Helpers

        /// <summary>
        ///     将对象序列化为JSON字符串
        /// </summary>
        private static string SerializeToString(object data, JsonType type)
        {
            return type switch
            {
                JsonType.JsonUtlity => JsonUtility.ToJson(data),
                JsonType.LitJson => JsonMapper.ToJson(data),
                _ => JsonMapper.ToJson(data)
            };
        }

        /// <summary>
        ///     将JSON字符串反序列化为对象
        /// </summary>
        private static T DeserializeFromString<T>(string jsonStr, JsonType type)
        {
            return type switch
            {
                JsonType.JsonUtlity => JsonUtility.FromJson<T>(jsonStr),
                JsonType.LitJson => JsonMapper.ToObject<T>(jsonStr),
                _ => JsonMapper.ToObject<T>(jsonStr)
            };
        }

        /// <summary>
        ///     验证文件路径并确保目录存在
        /// </summary>
        private static void ValidateAndEnsureDirectory(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径无效", nameof(filePath));

            // 检查文件路径是否有效
            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new ArgumentException("文件路径包含非法字符", nameof(filePath));

            // 获取目录路径
            var directory = Path.GetDirectoryName(filePath);

            // 只有目录路径不为空才创建
            if (!string.IsNullOrEmpty(directory))
                try
                {
                    // 使用更安全的创建方式
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                }
                catch (UnauthorizedAccessException)
                {
                    throw new UnauthorizedAccessException($"没有权限创建目录: {directory}");
                }
                catch (IOException ex)
                {
                    throw new IOException($"无法创建目录 {directory}: {ex.Message}", ex);
                }
        }

        #endregion
    }
}