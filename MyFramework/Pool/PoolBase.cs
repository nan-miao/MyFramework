using System;
using UnityEngine;
using Object = UnityEngine.Object;
namespace MyFramework.Pool
{
    //池策略枚举
    public enum PoolStrategy
    {
        AutoRelease, // 自动释放（原方案）
        Preload // 预创建（传统方案）
    }

    // 对象池基类
    public abstract class PoolBase : MonoBehaviour
    {
        public PoolType poolType;
        protected PoolStrategy m_Strategy;
        protected float m_ReleaseTime;
        protected long m_LastReleaseTime = 0;

        public virtual void Init(float time, PoolType type, PoolStrategy strategy)
        {
            m_ReleaseTime = time;
            poolType = type;
            m_Strategy = strategy;
            gameObject.name = $"{type}_{strategy}";
        }

        public abstract Object Spawn(string name);
        public abstract void UnSpawn(string name, Object obj);
        public abstract void Release(bool force = false);

        public virtual void OnUpdate()
        {
            if (m_Strategy == PoolStrategy.AutoRelease && m_ReleaseTime > 0)
            {
                if (DateTime.Now.Ticks - m_LastReleaseTime >= m_ReleaseTime * 10000000)
                {
                    m_LastReleaseTime = DateTime.Now.Ticks;
                    Release();
                }
            }
        }
    }
}