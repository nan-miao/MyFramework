using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace MyFramework.Scene.AA
{
    public enum LevelType
    {
        [LabelText("第一章")] Level1,
        [LabelText("第二章")] Level2,
        [LabelText("第三章")] Level3,
        [LabelText("第四章")] Level4,
        [LabelText("第五章")] Level5
    }

    [Serializable]
    public struct SceneNumber
    {
        [LabelText("章节")] public LevelType levelType;
        [LabelText("序号")] public int levelNumber;
    }

    [Serializable]
    public class LoadSceneSetting
    {
        [TableList(AlwaysExpanded = true)] [LabelText("需要加载的场景名列表")]
        public List<SceneNumber> needLoadSceneNames = new();

        [HideInInspector] public List<string> LoadSceneNames;
        [LabelText("加载模式(默认为叠加)")] public LoadSceneMode loadMode = LoadSceneMode.Additive;
        [LabelText("加载完后激活场景")] public bool setActive = true;
    }

    [RequireComponent(typeof(BoxCollider2D))]
    public class SceneLoadTrigger2D : MonoBehaviour
    {
        public string currentSceneName;
        public LoadSceneSetting setting;

        private void Start()
        {
            setting.LoadSceneNames = new List<string>(
                setting.needLoadSceneNames.Select(sceneName => $"{sceneName.levelType}_{sceneName.levelNumber}")
            );
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                foreach (var sceneName in setting.LoadSceneNames)
                    SceneLoader.Instance.LoadSceneAsync(sceneName, setting.loadMode, LoadCallBack);

                SceneLoader.Instance.AutoUnloadScene(setting.LoadSceneNames, UnloadCallBack);
            }
        }

        protected virtual void LoadCallBack(AsyncOperationHandle handle)
        {
        }

        protected virtual void UnloadCallBack(AsyncOperationHandle handle)
        {
        }
    }
}