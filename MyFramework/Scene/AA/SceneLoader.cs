using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MyFramework.Core.Singleton;
using MyFramework.Debuger;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace MyFramework.Scene.AA
{
    public class SceneLoader : BaseManager<SceneLoader>
    {
        private readonly Dictionary<string, AsyncOperationHandle> loadedSceneDic = new();
        private readonly Dictionary<string, AsyncOperationHandle> needUnloadSceneDic = new();

        private SceneLoader()
        {
            
        }
        
        /// <summary>
        ///     异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="loadSceneMode">加载模式</param>
        /// <param name="callBack">加载完成回调</param>
        public void LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, Action<AsyncOperationHandle> callBack)
        {
            //场景已加载
            if (loadedSceneDic.ContainsKey(sceneName)) return;

            //场景在待卸载队列中
            if (needUnloadSceneDic.ContainsKey(sceneName))
            {
                loadedSceneDic.Add(sceneName, needUnloadSceneDic[sceneName]);
                needUnloadSceneDic.Remove(sceneName);
                return;
            }

            //对传入的 场景名：资源名|场景编号 进行处理
            var assetName = sceneName.Split('|')[0];
            AsyncOperationHandle handle = Addressables.LoadSceneAsync(assetName, loadSceneMode);
            handle.Completed += obj =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    if (callBack != null)
                        callBack(obj);
                }
                else
                {
                    Debug.LogWarning(sceneName + "资源加载失败");
                    if (loadedSceneDic.ContainsKey(sceneName))
                        loadedSceneDic.Remove(sceneName);
                }
            };
            loadedSceneDic.Add(sceneName, handle);
        }

        public void UnLoadSceneAsync(string sceneName, [CanBeNull] Action<AsyncOperationHandle> callBack,
            bool unloadImmediately = true)
        {
            AsyncOperationHandle handle;
            //立刻卸载场景
            if (unloadImmediately)
            {
                if (loadedSceneDic.TryGetValue(sceneName, out handle))
                {
                    Addressables.UnloadSceneAsync(handle);
                    loadedSceneDic.Remove(sceneName);
                    handle.Completed += obj =>
                    {
                        if (callBack != null)
                            callBack(obj);
                    };
                    return;
                }

                if (needUnloadSceneDic.TryGetValue(sceneName, out handle))
                {
                    Addressables.UnloadSceneAsync(handle);
                    needUnloadSceneDic.Remove(sceneName);
                    handle.Completed += obj =>
                    {
                        if (callBack != null)
                            callBack(obj);
                    };
                    return;
                }

                DebugLogger.Log($"已激活场景中不存在{sceneName}却尝试卸载");
            }
            //将场景放入待卸载字典中
            else
            {
                if (loadedSceneDic.Remove(sceneName, out handle))
                {
                    needUnloadSceneDic.Add(sceneName, handle);
                    handle.Completed += obj =>
                    {
                        if (callBack != null)
                            callBack(obj);
                    };
                    return;
                }

                DebugLogger.Log($"激活场景中未为找到{sceneName},场景可能已在待卸载场景中");
            }
        }

        public void ReleaseNeedUnloadScene()
        {
            foreach (var handle in needUnloadSceneDic) Addressables.UnloadSceneAsync(handle.Value);
            needUnloadSceneDic.Clear();
        }

        public void AutoUnloadScene(List<string> sceneNames, Action<AsyncOperationHandle> unloadCallBack)
        {
            var needUnloadScenes = new List<string>();

            //已激活场景中获取需要卸载的场景
            foreach (var loadedScene in loadedSceneDic)
                if (!sceneNames.Contains(loadedScene.Key))
                    needUnloadScenes.Add(loadedScene.Key);

            foreach (var unloadScene in needUnloadScenes) UnLoadSceneAsync(unloadScene, unloadCallBack);
        }


        public void SwitchSceneAsync(LoadSceneSetting newSceneSettings)
        {
            foreach (var value in needUnloadSceneDic)
                UnLoadSceneAsync(value.Key, obj => { });

            foreach (var value in loadedSceneDic)
                UnLoadSceneAsync(value.Key, obj => { });

            foreach (var name in newSceneSettings.LoadSceneNames)
                LoadSceneAsync(name, newSceneSettings.loadMode, obj => { });
        }

        public float GetLoadProgress(string sceneName)
        {
            if (loadedSceneDic.TryGetValue(sceneName, out var handle)) return handle.GetDownloadStatus().Percent;

            DebugLogger.Log("该场景不在加载队列中");
            return 0;
        }

        public void Reset()
        {
            loadedSceneDic.Clear();
            needUnloadSceneDic.Clear();
        }
    }
}