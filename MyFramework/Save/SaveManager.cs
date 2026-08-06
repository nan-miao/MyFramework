using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyFramework.Core.Singleton;
using MyFramework.Util;
using MyFramework.Util.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFramework.Save
{
    public interface ISaveContainer
    {
        public void SaveData();
        public void LoadData();
    }

    public class SaveContainer<T, TK> : BaseManager<TK> where TK : SaveContainer<T, TK>, ISaveContainer, new()
        where T : struct
    {
        public T data;
        public string fileName;
        public SaveContainerType saveType;

        protected SaveContainer()
        {
            Init();
        }

        /// <summary>
        ///     是否启用加密
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        public bool UseEncryption { get; private set; }

        /// <summary>
        ///     加密/解密密钥
        /// </summary>
        [ShowInInspector]
        [ReadOnly]
        public string EncryptionKey { get; private set; }

        /// <summary>
        ///     设置默认数值，同步加密配置
        /// </summary>
        protected virtual void Init()
        {
            foreach (var setting in SaveManager.Instance.saveConfig.saveSettings)
                if (setting.saveType == saveType)
                {
                    fileName = setting.fileName;
                    saveType = setting.saveType;
                    UseEncryption = setting.useEncryption;
                    EncryptionKey = setting.encryptionKey;
                }
        }

        public void SaveData()
        {
            var fullPath = Path.Combine(PathUtil.SaveDataPath, fileName);

            if (UseEncryption && !string.IsNullOrEmpty(EncryptionKey))
                JsonManager.Instance.CustomSaveDataEncrypted(data, fullPath, EncryptionKey);
            else
                JsonManager.Instance.CustomSaveData(data, fullPath);
        }

        public void LoadData()
        {
            var fullPath = Path.Combine(PathUtil.SaveDataPath, fileName);

            if (UseEncryption && !string.IsNullOrEmpty(EncryptionKey))
                data = JsonManager.Instance.CustomLoadDataEncrypted<T>(fullPath, EncryptionKey);
            else
                data = JsonManager.Instance.CustomLoadData<T>(fullPath);
        }
    }

    public interface ISaveData
    {
        ISaveContainer SaveDataContainer { get;}
        public void SaveData();
        public void LoadData();
        
    }

    [Serializable]
    public enum SaveContainerType
    {
        Global,
        Player
    }

    [Serializable]
    public struct SaveSetting
    {
        /// <summary>
        ///     存档文件名（含后缀）
        /// </summary>
        public string fileName;

        /// <summary>
        ///     存档类型
        /// </summary>
        public SaveContainerType saveType;

        /// <summary>
        ///     是否启用加密/解密
        /// </summary>
        [ToggleLeft]public bool useEncryption;

        /// <summary>
        ///     加密/解密密钥字符串
        /// </summary>
        [ShowIf("useEncryption")]public string encryptionKey;
    }

    public class SaveManager : SingletonMono<SaveManager>
    {
        private static string saveDataPath;

        [InlineEditor(InlineEditorModes.FullEditor)]
        public SaveConfig saveConfig;

        private bool hasLoad;

        private List<ISaveData> saveDatas;
        public string SaveDataPath => saveDataPath;

        protected override void OnStart()
        {
            base.OnStart();
            saveDataPath = Application.persistentDataPath;
            FindAllSaveDataOnScene();
        }

        public void NewGame()
        {
            //TODO::new出所有SaveContainer
        }

        public void SaveGame()
        {
            foreach (var saveData in saveDatas)
            {
                saveData.SaveData();
            }
            //TODO::所有SaveContainer调用SaveData
        }

        /// <summary>
        ///     存储指定游戏数据
        /// </summary>
        /// <param name="saveContainer">指定的数据容器</param>
        public void SaveAssignGameData(ISaveContainer saveContainer)
        {
            foreach (var saveData in saveDatas)
            {
                if (saveData.SaveDataContainer == saveContainer)
                    saveData.SaveData();
            }

            saveContainer.SaveData();
        }

        /// <summary>
        ///     加载相关数据
        /// </summary>
        /// <param name="forceLoad">每次都加载数据文件</param>
        public void LoadGame(bool forceLoad = false)
        {
            if (!hasLoad || forceLoad)
            {
                //TODO::所有SaveContainer调用LoadData
            }

            foreach (var saveData in saveDatas) saveData.LoadData();

            hasLoad = true;
        }

        /// <summary>
        ///     加载指定游戏数据
        /// </summary>
        /// <param name="saveContainer">指定的数据容器</param>
        public void LoadAssignGameData(ISaveContainer saveContainer)
        {
            saveContainer.LoadData();

            foreach (var saveData in saveDatas)
                if (saveData.SaveDataContainer == saveContainer)
                    saveData.LoadData();
        }

        public void FindAllSaveDataOnScene()
        {
            saveDatas = FindAllSaveData();
        }

        private List<ISaveData> FindAllSaveData()
        {
            var saveManagers = FindObjectsOfType<MonoBehaviour>().OfType<ISaveData>();

            return new List<ISaveData>(saveManagers);
        }

#if UNITY_EDITOR
        [ContextMenu("删除所有存档文件")]
        public void DeleteAllSaveData()
        {
            foreach (var setting in saveConfig.saveSettings)
            {
                var fullPath = Path.Combine(PathUtil.SaveDataPath, setting.fileName);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
        }
#endif
    }
}