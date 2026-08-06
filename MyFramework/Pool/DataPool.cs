using System.Collections.Generic;

namespace MyFramework.Pool
{
    public interface IPoolObject
    {
        void ResetInfo();
    }

    public interface IDataPool
    {
        object Get();
        void Return(object obj);
        void Clear();
    }

    public class DataPool<T> : IDataPool where T : class, IPoolObject, new()
    {
        private Queue<T> m_Pool = new Queue<T>();
        private int m_MaxSize = 100;

        // 泛型版本（推荐用法）
        public T Get()
        {
            return m_Pool.Count > 0 ? m_Pool.Dequeue() : new T();
        }

        // 显式实现接口方法
        object IDataPool.Get()
        {
            return Get();
        }

        // 泛型版本（推荐用法）
        public void Return(T obj)
        {
            obj.ResetInfo();
            if (m_Pool.Count < m_MaxSize)
                m_Pool.Enqueue(obj);
        }

        // 显式实现接口方法
        void IDataPool.Return(object obj)
        {
            if (obj is T typedObj)
            {
                Return(typedObj);
            }
        }

        public void Clear()
        {
            m_Pool.Clear();
        }
    }
}