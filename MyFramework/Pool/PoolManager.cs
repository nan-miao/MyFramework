using System;
using System.Collections.Generic;
using MyFramework.Core;
using MyFramework.Core.Singleton;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyFramework.Pool
{
    public class PoolManager : SingletonAutoMono<PoolManager>
    {
        [SerializeField] private Transform m_PoolParent;
        
        // GameObject池字典
        private Dictionary<PoolType, PoolBase> m_GameObjectPools = new Dictionary<PoolType, PoolBase>();
        
        // 数据池字典（按类型存储）
        private Dictionary<Type, IDataPool> m_DataPools = new Dictionary<Type, IDataPool>();

        protected override void OnStart()
        {
            base.OnStart();
            m_PoolParent = transform;
            // 自动释放池（原方案）
            CreatePool<AutoReleasePool>(PoolType.Effect, 400f, PoolStrategy.AutoRelease);
            CreatePool<AutoReleasePool>(PoolType.Enemy, 600f, PoolStrategy.AutoRelease);
            CreatePool<AutoReleasePool>(PoolType.Bullet, 200f, PoolStrategy.AutoRelease);
            CreatePool<AutoReleasePool>(PoolType.DropItem, 600f, PoolStrategy.AutoRelease);
        }

        #region GameObject池管理
        private void CreatePool<T>(PoolType type, float releaseTime, PoolStrategy strategy) 
            where T : PoolBase
        {
            if (m_GameObjectPools.ContainsKey(type)) return;

            var go = new GameObject();
            go.transform.SetParent(m_PoolParent);
            var pool = go.AddComponent<T>();
            pool.Init(releaseTime, type, strategy);
            MonoManager.Instance?.AddUpdateListener(pool.OnUpdate);
            m_GameObjectPools.Add(type, pool);
        }

        public void CreatePreloadPool(PoolType type, int maxNum, GameObject prefab, int preloadCount)
        {
            if (m_GameObjectPools.ContainsKey(type)) return;

            var go = new GameObject();
            go.transform.SetParent(m_PoolParent);
            var pool = go.AddComponent<PreloadPool>();
            pool.Init(0, type, PoolStrategy.Preload);
            ((PreloadPool)pool).InitPreload(maxNum, prefab, preloadCount);
            MonoManager.Instance?.AddUpdateListener(pool.OnUpdate);
            m_GameObjectPools.Add(type, pool);
        }

        public Object Spawn(PoolType type, string name)
        {
            return m_GameObjectPools.TryGetValue(type, out var pool) ? pool.Spawn(name) : null;
        }

        public void UnSpawn(PoolType type, string name, Object obj)
        {
            if (m_GameObjectPools.TryGetValue(type, out var pool))
                pool.UnSpawn(name, obj);
        }
        #endregion

        #region 数据池管理
        /// <summary>
        /// 创建数据池
        /// </summary>
        public void CreateDataPool<T>() where T : class, IPoolObject, new()
        {
            Type dataType = typeof(T);
            if (m_DataPools.ContainsKey(dataType)) return;
            
            m_DataPools[dataType] = new DataPool<T>();
        }

        /// <summary>
        /// 获取数据（泛型版本，推荐）
        /// </summary>
        public T GetData<T>() where T : class, IPoolObject, new()
        {
            Type dataType = typeof(T);
            if (m_DataPools.TryGetValue(dataType, out var pool))
            {
                return ((DataPool<T>)pool).Get();
            }
            
            // 如果不存在则自动创建
            CreateDataPool<T>();
            return ((DataPool<T>)m_DataPools[dataType]).Get();
        }

        /// <summary>
        /// 归还数据（泛型版本，推荐）
        /// </summary>
        public void ReturnData<T>(T obj) where T : class, IPoolObject, new()
        {
            Type dataType = typeof(T);
            if (m_DataPools.TryGetValue(dataType, out var pool))
            {
                ((DataPool<T>)pool).Return(obj);
            }
        }

        /// <summary>
        /// 清空指定类型的数据池
        /// </summary>
        public void ClearDataPool<T>() where T : class, IPoolObject, new()
        {
            Type dataType = typeof(T);
            if (m_DataPools.TryGetValue(dataType, out var pool))
            {
                pool.Clear();
            }
        }

        /// <summary>
        /// 检查指定类型的数据池是否存在
        /// </summary>
        public bool HasDataPool<T>() where T : class, IPoolObject, new()
        {
            return m_DataPools.ContainsKey(typeof(T));
        }
        #endregion

        #region 统一释放
        public void ReleaseAll(bool force = false)
        {
            // 释放GameObject池
            foreach (var pool in m_GameObjectPools.Values)
                pool.Release(force);

            // 释放数据池
            if (force)
            {
                foreach (var pool in m_DataPools.Values)
                    pool.Clear();
            }
        }
        #endregion
    }
}