using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MyFramework.Core.Singleton;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace MyFramework.AssetLoad.AA
{
    using Object = Object;

//可寻址资源 信息  
    public class AddressablesInfo
    {
        //记录 引用计数  
        public uint count;

        //记录 异步操作句柄  
        public AsyncOperationHandle handle;

        public AddressablesInfo(AsyncOperationHandle handle)
        {
            this.handle = handle;
            count += 1;
        }
    }

    public class AddressablesManager : BaseManager<AddressablesManager>
    {
        private AddressablesManager() { }
        //有一个容器 帮助我们存储 异步加载的返回值  
        public Dictionary<string, AddressablesInfo> resDic = new();
        
        #region 指定 动态加载/释放 单个资源

        /// <summary>
        ///     指定加载单个资源
        /// </summary>
        /// <param name="callBack">完成后的回调</param>
        /// <typeparam name="T"></typeparam>
        public void LoadAssetAsync<T>(string GUID, AssetReference assetReference,
            Action<AsyncOperationHandle<T>> callBack) where T : Object
        {
            AsyncOperationHandle<T> handle;
            if (resDic.ContainsKey(GUID))
            {
                //获取异步加载返回的操作内容  
                handle = resDic[GUID].handle.Convert<T>();
                //要使用资源了 那么引用计数+1  
                resDic[GUID].count += 1;
                //判断 这个异步加载是否结束  
                if (handle.IsDone)
                    //如果成功 就不需要异步了 直接相当于同步调用了 这个委托函数 传入对应的返回值  
                    callBack(handle);
                //还没有加载完成  
                else
                    //如果这个时候 还没有异步加载完成 那么我们只需要 告诉它 完成时做什么就行了  
                    handle.Completed += obj =>
                    {
                        if (obj.Status == AsyncOperationStatus.Succeeded)
                            callBack(obj);
                    };
                return;
            }

            //如果没有加载过该资源  
            //直接进行异步加载 并且记录  
            handle = assetReference.LoadAssetAsync<T>();
            handle.Completed += obj =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    callBack(obj);
                }
                else
                {
                    Debug.LogWarning(GUID + "资源加载失败");
                    if (resDic.ContainsKey(GUID))
                        resDic.Remove(GUID);
                }
            };
            var info = new AddressablesInfo(handle);
            resDic.Add(GUID, info);
        }

        /// <summary>
        ///     异步加载单个资源的方法
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="callBack">加载完后的回调</param>
        /// <typeparam name="T">资源类型</typeparam>
        public AddressablesInfo LoadAssetAsync<T>(string name, [CanBeNull] Action<AsyncOperationHandle<T>> callBack)
        {
            //由于存在同名 不同类型资源的区分加载  
            //所以我们通过名字和类型拼接作为 key        
            var keyName = name + "_" + typeof(T).Name;
            AsyncOperationHandle<T> handle;
            //如果已经加载过该资源  
            if (resDic.ContainsKey(keyName))
            {
                //获取异步加载返回的操作内容  
                handle = resDic[keyName].handle.Convert<T>();
                //要使用资源了 那么引用计数+1  
                resDic[keyName].count += 1;
                //判断 这个异步加载是否结束  
                if (handle.IsDone)
                {
                    //如果成功 就不需要异步了 直接相当于同步调用了 这个委托函数 传入对应的返回值  
                    if (callBack != null) callBack(handle);
                }
                //还没有加载完成  
                else
                {
                    //如果这个时候 还没有异步加载完成 那么我们只需要 告诉它 完成时做什么就行了  
                    handle.Completed += obj =>
                    {
                        if (obj.Status == AsyncOperationStatus.Succeeded)
                            callBack(obj);
                    };
                }

                return resDic[keyName];
            }

            //如果没有加载过该资源  
            //直接进行异步加载 并且记录  
            handle = Addressables.LoadAssetAsync<T>(name);
            handle.Completed += obj =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    callBack(obj);
                }
                else
                {
                    Debug.LogWarning(keyName + "资源加载失败");
                    if (resDic.ContainsKey(keyName))
                        resDic.Remove(keyName);
                }
            };
            var info = new AddressablesInfo(handle);
            resDic.Add(keyName, info);
            return info;
        }

        /// <summary>
        ///     释放单个资源
        /// </summary>
        /// <param name="name">资源名称/GUID</param>
        /// <param name="isSpecified">该资源是否为指定加载</param>
        /// <param name="force">强制释放</param>
        /// <typeparam name="T">资源类型</typeparam>
        public void Release<T>(string name, bool isSpecified = true, bool force = false)
        {
            if (isSpecified)
            {
                if (resDic.ContainsKey(name))
                {
                    //释放时 引用计数-1  
                    resDic[name].count -= 1;
                    //如果引用计数为0  才真正的释放  
                    if (resDic[name].count == 0)
                    {
                        //取出对象 移除资源 并且从字典里面移除  
                        var handle = resDic[name].handle.Convert<T>();
                        Addressables.Release(handle);
                        resDic.Remove(name);
                    }
                }
            }
            else
            {
                //由于存在同名 不同类型资源的区分加载  
                //所以我们通过名字和类型拼接作为 key        
                var keyName = name + "_" + typeof(T).Name;
                if (resDic.ContainsKey(keyName))
                {
                    if (force)
                        resDic[keyName].count = 0;
                    else
                        //释放时 引用计数-1  
                        resDic[keyName].count -= 1;

                    //如果引用计数为0  才真正的释放  
                    if (resDic[keyName].count == 0)
                    {
                        //取出对象 移除资源 并且从字典里面移除  
                        var handle = resDic[keyName].handle.Convert<T>();
                        Addressables.Release(handle);
                        resDic.Remove(keyName);
                    }
                }
            }
        }

        #endregion

        #region 动态加载/释放 多个资源

        /// <summary>
        ///     异步加载多个资源 或者 根据标签加载指定资源
        /// </summary>
        /// <param name="mode">加载模式 Union并集 Intersection交集</param>
        /// <param name="callBack">加载完后的回调</param>
        /// <param name="keys">资源名称 以及 标签</param>
        /// <typeparam name="T">资源类型</typeparam>
        public void LoadAssetAsync<T>(Addressables.MergeMode mode, Action<T> callBack, params string[] keys)
        {
            //1.构建一个keyName  之后用于存入到字典中  
            var list = new List<string>(keys);
            var keyName = "";
            foreach (var key in list)
                keyName += key + "_";
            keyName += typeof(T).Name;
            //2.判断是否存在已经加载过的内容   
            //存在做什么  
            AsyncOperationHandle<IList<T>> handle;
            if (resDic.ContainsKey(keyName))
            {
                handle = resDic[keyName].handle.Convert<IList<T>>();
                //要使用资源了 那么引用计数+1  
                resDic[keyName].count += 1;
                //异步加载是否结束  
                if (handle.IsDone)
                    foreach (var item in handle.Result)
                        callBack(item);
                else
                    handle.Completed += obj =>
                    {
                        //加载成功才调用外部传入的委托函数  
                        if (obj.Status == AsyncOperationStatus.Succeeded)
                            foreach (var item in handle.Result)
                                callBack(item);
                    };
                return;
            }

            //不存在做什么  
            handle = Addressables.LoadAssetsAsync(list, callBack, mode);
            handle.Completed += obj =>
            {
                if (obj.Status == AsyncOperationStatus.Failed)
                {
                    Debug.LogError("资源加载失败" + keyName);
                    if (resDic.ContainsKey(keyName))
                        resDic.Remove(keyName);
                }
            };
            var info = new AddressablesInfo(handle);
            resDic.Add(keyName, info);
        }

        /// <summary>
        ///     根据资源名称/标签释放资源
        /// </summary>
        /// <param name="keys"></param>
        /// <typeparam name="T"></typeparam>
        public void Release<T>(params string[] keys)
        {
            //1.构建一个keyName  之后用于存入到字典中  
            var list = new List<string>(keys);
            var keyName = "";
            foreach (var key in list)
                keyName += key + "_";
            keyName += typeof(T).Name;
            if (resDic.ContainsKey(keyName))
            {
                resDic[keyName].count -= 1;

                if (resDic[keyName].count == 0)
                {
                    //取出字典里面的对象  
                    var handle = resDic[keyName].handle.Convert<IList<T>>();
                    Addressables.Release(handle);
                    resDic.Remove(keyName);
                }
            }
        }

        #endregion


        /*public void LoadAssetAsync<T>(Addressables.MergeMode mode, Action<AsyncOperationHandle<IList<T>>> callBack, params string[] keys)
       {
       }  */
    }
}