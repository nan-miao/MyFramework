using System;
using System.Collections.Generic;

namespace MyFramework.AI.GOAP
{
    public static class GOAPObjectPool
    {
        private static Dictionary<Type,Stack<object>> pools = new Dictionary<Type, Stack<object>>();

        public static T Get<T>()
        {
            if (pools.TryGetValue(typeof(T), out Stack<object> objects) &&  objects.Count > 0)
            {
                return (T)objects.Pop();
            }
            return default;
        }

        public static T GetOrNew<T>() where T : new()
        {
            T obj = Get<T>();
            if (obj == null)
                obj = new T();
            return obj;
        }

        public static void Recycle(object obj)
        {
            Type t = obj.GetType();
            if (!pools.TryGetValue(t, out Stack<object> objects))
            {
                objects = new Stack<object>();
                pools.Add(t, objects);
            }
            objects.Push(obj);
        }
    }
}
