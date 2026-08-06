using System;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MyFramework.Util
{
    /// <summary>
    ///     加密工具类 主要提供加密需求
    /// </summary>
    public class EncryptionUtil
    {
        //1.获取随机密钥
        public static int GetRandomKey()
        {
            return Random.Range(1, 10000) + 5;
        }

        //2.加密数据（数值型）
        public static int LockValue(int value, int key)
        {
            //主要采用异或加密
            value = value ^ (key % 9);
            value = value ^ 0xADAD;
            value = value ^ (1 << 5);
            value += key;
            return value;
        }

        public static long LockValue(long value, int key)
        {
            //主要采用异或加密
            value = value ^ (key % 9);
            value = value ^ 0xADAD;
            value = value ^ (1 << 5);
            value += key;
            return value;
        }

        //3.解密数据（数值型）
        public static int UnlockValue(int value, int key)
        {
            //有可能还没有加密过 没有初始化过的数据 直接想要获取 那么就不用解密了
            //这种时候数值肯定是0
            if (value == 0)
                return value;
            value -= key;
            value = value ^ (key % 9);
            value = value ^ 0xADAD;
            value = value ^ (1 << 5);
            return value;
        }

        public static long UnlockValue(long value, int key)
        {
            //有可能还没有加密过 没有初始化过的数据 直接想要获取 那么就不用解密了
            //这种时候数值肯定是0
            if (value == 0)
                return value;
            value -= key;
            value = value ^ (key % 9);
            value = value ^ 0xADAD;
            value = value ^ (1 << 5);
            return value;
        }

        //4.字符串加密（用于JSON等文本数据）
        /// <summary>
        ///     使用密钥对字符串进行异或加密，返回Base64编码的密文
        /// </summary>
        /// <param name="text">明文字符串</param>
        /// <param name="key">密钥字符串</param>
        /// <returns>Base64编码的密文</returns>
        public static string EncryptString(string text, string key)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (string.IsNullOrEmpty(key))
                return text;

            var sb = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++) sb.Append((char)(text[i] ^ key[i % key.Length]));

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        /// <summary>
        ///     使用密钥对Base64密文进行解密，返回明文字符串
        /// </summary>
        /// <param name="encryptedText">Base64编码的密文</param>
        /// <param name="key">密钥字符串</param>
        /// <returns>明文字符串</returns>
        public static string DecryptString(string encryptedText, string key)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            if (string.IsNullOrEmpty(key))
                return encryptedText;

            var data = Convert.FromBase64String(encryptedText);
            var text = Encoding.UTF8.GetString(data);
            var sb = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++) sb.Append((char)(text[i] ^ key[i % key.Length]));

            return sb.ToString();
        }

        /// <summary>
        ///     根据字符串密钥生成一个int型密钥（兼容旧的数值型LockValue/UnlockValue）
        /// </summary>
        /// <param name="key">密钥字符串</param>
        /// <returns>int型密钥</returns>
        public static int GenerateIntKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return 0;

            var hash = 0;
            foreach (var c in key) hash = hash * 31 + c;
            return Mathf.Abs(hash % 10000) + 5;
        }
    }
}