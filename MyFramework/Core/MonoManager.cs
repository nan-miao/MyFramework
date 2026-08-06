using System;
using System.Collections.Generic;
using MyFramework.Core.Singleton;

public interface IMonoOwner
{
    void RegisterUpdateListeners();
    void UnregisterUpdateListeners();
}

namespace MyFramework.Core
{
    public class MonoManager : SingletonAutoMono<MonoManager>
    {
        // 使用有序字典来维护执行顺序
        private SortedDictionary<int, Action> updateActions = new SortedDictionary<int, Action>();
        private SortedDictionary<int, Action> lateUpdateActions = new SortedDictionary<int, Action>();
        private SortedDictionary<int, Action> fixedUpdateActions = new SortedDictionary<int, Action>();

        // 频率控制字典（帧间隔/固定时间步长）
        private Dictionary<Action, int> updateFrequencies = new Dictionary<Action, int>();
        private Dictionary<Action, int> lateUpdateFrequencies = new Dictionary<Action, int>();
        private Dictionary<Action, int> fixedUpdateFrequencies = new Dictionary<Action, int>();

        // 帧计数器 - 分开计数
        private int updateFrameCount = 0;
        private int lateUpdateFrameCount = 0;
        private int fixedUpdateFrameCount = 0;

        // 缓存键列表，避免每帧分配新列表
        private List<int> updateCachedKeys = new List<int>();
        private List<int> lateUpdateCachedKeys = new List<int>();
        private List<int> fixedUpdateCachedKeys = new List<int>();

        // 标记是否需要刷新缓存
        private bool updateCacheDirty = false;
        private bool lateUpdateCacheDirty = false;
        private bool fixedUpdateCacheDirty = false;

        #region 生命周期事件的添加和移除

        public void AddUpdateListener(Action action, int order = 0, int frequency = 1)
        {
            if (action == null) return;

            if (!updateActions.ContainsKey(order))
            {
                updateActions[order] = null;
            }

            updateActions[order] += action;
            updateFrequencies[action] = frequency;
            updateCacheDirty = true;
        }

        public void AddLateUpdateListener(Action action, int order = 0, int frequency = 1)
        {
            if (action == null) return;

            if (!lateUpdateActions.ContainsKey(order))
            {
                lateUpdateActions[order] = null;
            }

            lateUpdateActions[order] += action;
            lateUpdateFrequencies[action] = frequency;
            lateUpdateCacheDirty = true;
        }

        public void AddFixedUpdateListener(Action action, int order = 0, int frequency = 1)
        {
            if (action == null) return;

            if (!fixedUpdateActions.ContainsKey(order))
            {
                fixedUpdateActions[order] = null;
            }

            fixedUpdateActions[order] += action;
            fixedUpdateFrequencies[action] = frequency;
            fixedUpdateCacheDirty = true;
        }

        public void RemoveUpdateListener(Action action)
        {
            if (action == null) return;

            List<int> keysToUpdate = new List<int>(updateActions.Keys);

            foreach (int order in keysToUpdate)
            {
                if (updateActions.TryGetValue(order, out var currentAction) && currentAction != null)
                {
                    updateActions[order] = currentAction - action;

                    if (updateActions[order] == null)
                    {
                        updateActions.Remove(order);
                    }
                }
            }

            updateFrequencies.Remove(action);
            updateCacheDirty = true;
        }

        public void RemoveLateUpdateListener(Action action)
        {
            if (action == null) return;

            List<int> keysToUpdate = new List<int>(lateUpdateActions.Keys);

            foreach (int order in keysToUpdate)
            {
                if (lateUpdateActions.TryGetValue(order, out var currentAction) && currentAction != null)
                {
                    lateUpdateActions[order] = currentAction - action;

                    if (lateUpdateActions[order] == null)
                    {
                        lateUpdateActions.Remove(order);
                    }
                }
            }

            lateUpdateFrequencies.Remove(action);
            lateUpdateCacheDirty = true;
        }

        public void RemoveFixedUpdateListener(Action action)
        {
            if (action == null) return;

            List<int> keysToUpdate = new List<int>(fixedUpdateActions.Keys);

            foreach (int order in keysToUpdate)
            {
                if (fixedUpdateActions.TryGetValue(order, out var currentAction) && currentAction != null)
                {
                    fixedUpdateActions[order] = currentAction - action;

                    if (fixedUpdateActions[order] == null)
                    {
                        fixedUpdateActions.Remove(order);
                    }
                }
            }

            fixedUpdateFrequencies.Remove(action);
            fixedUpdateCacheDirty = true;
        }

        #endregion

        private void Update()
        {
            updateFrameCount++;
            ExecuteActions(updateActions, updateFrequencies, updateFrameCount, ref updateCachedKeys,
                ref updateCacheDirty);
        }

        private void LateUpdate()
        {
            lateUpdateFrameCount++;
            ExecuteActions(lateUpdateActions, lateUpdateFrequencies, lateUpdateFrameCount, ref lateUpdateCachedKeys,
                ref lateUpdateCacheDirty);
        }

        private void FixedUpdate()
        {
            fixedUpdateFrameCount++;
            ExecuteActions(fixedUpdateActions, fixedUpdateFrequencies, fixedUpdateFrameCount, ref fixedUpdateCachedKeys,
                ref fixedUpdateCacheDirty);
        }

        private void ExecuteActions(
            SortedDictionary<int, Action> actions,
            Dictionary<Action, int> frequencies,
            int frameCounter,
            ref List<int> cachedKeys,
            ref bool cacheDirty)
        {
            // 刷新缓存（如果需要）
            if (cacheDirty || cachedKeys.Count == 0)
            {
                cachedKeys.Clear();
                cachedKeys.AddRange(actions.Keys);
                cacheDirty = false;
            }

            for (int i = 0; i < cachedKeys.Count; i++)
            {
                int order = cachedKeys[i];

                if (!actions.TryGetValue(order, out var action) || action == null)
                    continue;

                // 获取委托调用列表的副本
                Delegate[] invocationList;
                try
                {
                    invocationList = action.GetInvocationList();
                }
                catch
                {
                    continue;
                }

                foreach (Delegate del in invocationList)
                {
                    Action singleAction = del as Action;
                    if (singleAction == null) continue;

                    // 检查委托是否有效
                    if (singleAction.Target == null)
                    {
                        RemoveInvalidAction(singleAction, actions, frequencies);
                        continue;
                    }

                    if (ShouldExecute(singleAction, frequencies, frameCounter))
                    {
                        singleAction.Invoke();
                    }
                }
            }
        }

        private void RemoveInvalidAction(Action invalidAction, SortedDictionary<int, Action> actions,
            Dictionary<Action, int> frequencies)
        {
            // 立即移除无效委托，不使用协程
            List<int> keysToUpdate = new List<int>(actions.Keys);

            foreach (int order in keysToUpdate)
            {
                if (actions.TryGetValue(order, out var currentAction) && currentAction != null)
                {
                    actions[order] = currentAction - invalidAction;

                    if (actions[order] == null)
                    {
                        actions.Remove(order);
                    }
                }
            }

            frequencies.Remove(invalidAction);
        }

        private bool ShouldExecute(Action action, Dictionary<Action, int> frequencies, int frameCounter)
        {
            if (!frequencies.ContainsKey(action) || frequencies[action] <= 1)
            {
                return true;
            }

            return frameCounter % frequencies[action] == 0;
        }

        // 清空所有监听
        public void ClearAll()
        {
            updateActions.Clear();
            lateUpdateActions.Clear();
            fixedUpdateActions.Clear();
            updateFrequencies.Clear();
            lateUpdateFrequencies.Clear();
            fixedUpdateFrequencies.Clear();

            updateCachedKeys.Clear();
            lateUpdateCachedKeys.Clear();
            fixedUpdateCachedKeys.Clear();

            updateCacheDirty = false;
            lateUpdateCacheDirty = false;
            fixedUpdateCacheDirty = false;
        }
        
    }
}