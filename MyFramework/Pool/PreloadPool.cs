using System.Collections.Generic;
using UnityEngine;

namespace MyFramework.Pool
{
    public class PreloadPool : PoolBase
    {
        private Stack<GameObject> m_Stack = new Stack<GameObject>();
        private List<GameObject> m_UsedList = new List<GameObject>();
        private int m_MaxNum;
        private GameObject m_Root;

        public void InitPreload(int maxNum, GameObject prefab, int preloadCount)
        {
            m_MaxNum = maxNum;
            m_Root = new GameObject($"{poolType}_Root");
            m_Root.transform.SetParent(transform);

            for (int i = 0; i < preloadCount; i++)
            {
                var obj = Instantiate(prefab, m_Root.transform);
                obj.SetActive(false);
                m_Stack.Push(obj);
            }
        }

        public override Object Spawn(string name)
        {
            GameObject obj;
            if (m_Stack.Count > 0)
            {
                obj = m_Stack.Pop();
                m_UsedList.Add(obj);
            }
            else if (m_UsedList.Count < m_MaxNum)
            {
                // 动态创建新对象（可选）
                return null;
            }
            else
            {
                // 重用最早使用的对象
                obj = m_UsedList[0];
                m_UsedList.RemoveAt(0);
                m_UsedList.Add(obj);
            }
        
            obj.SetActive(true);
            obj.transform.SetParent(null);
            return obj;
        }

        public override void UnSpawn(string name, Object obj)
        {
            if (obj is GameObject go)
            {
                go.SetActive(false);
                go.transform.SetParent(m_Root.transform);
                m_Stack.Push(go);
                m_UsedList.Remove(go);
            }
        }

        public override void Release(bool force = false)
        {
            if (force)
            {
                while (m_Stack.Count > 0)
                    Destroy(m_Stack.Pop());
                foreach (var obj in m_UsedList)
                    Destroy(obj);
                m_UsedList.Clear();
            }
        }
    }
}