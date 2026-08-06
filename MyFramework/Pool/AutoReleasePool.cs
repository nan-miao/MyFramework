using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyFramework.Pool
{
    public class AutoReleasePool : PoolBase
    {
        protected List<PoolObject> m_Objects = new List<PoolObject>();

        public override Object Spawn(string name)
        {
            for (int i = m_Objects.Count - 1; i >= 0; i--)
            {
                if (m_Objects[i].Name == name)
                {
                    var obj = m_Objects[i].Object;
                    m_Objects.RemoveAt(i);
                    if (obj is GameObject go) go.SetActive(true);
                    return obj;
                }
            }
            return null;
        }

        public override void UnSpawn(string name, Object obj)
        {
            if (obj is GameObject go)
            {
                go.SetActive(false);
                go.transform.SetParent(transform);
            }
            m_Objects.Add(new PoolObject(name, obj));
        }

        public override void Release(bool force = false)
        {
            if (force)
            {
                foreach (var item in m_Objects)
                    Destroy(item.Object);
                m_Objects.Clear();
                return;
            }

            for (int i = m_Objects.Count - 1; i >= 0; i--)
            {
                if (DateTime.Now.Ticks - m_Objects[i].LastUseTime.Ticks >= m_ReleaseTime * 10000000)
                {
                    Destroy(m_Objects[i].Object);
                    m_Objects.RemoveAt(i);
                }
            }
        }
    }
}