using System.Collections.Generic;
using MyFramework.AI.GOAP.Action;
using MyFramework.AI.GOAP.Goals;
using MyFramework.AI.GOAP.Plan;
using MyFramework.AI.GOAP.State;
using MyFramework.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFramework.AI.GOAP
{
    public class GOAPAgent : SerializedMonoBehaviour
    {
        [LabelText("目标")]public GOAPGoals goals =new GOAPGoals();
        [LabelText("局部状态")]public GOAPStates states = new GOAPStates();
        [LabelText("全部行为")] public GOAPActions actions = new GOAPActions();
        [LabelText("计划")] public GOAPPlan plan = new GOAPPlan();
    
        public Dictionary<string,GOAPStateBase> defualtStateDic = new Dictionary<string,GOAPStateBase>();
        public IGOAPOwner owner { get;private set; }
    
        public void Init(IGOAPOwner owner)
        {
            this.owner = owner;
            actions.Init(this,owner);
            goals.Init(this,owner);
            BackupDefaultStates();
        }
    
        private void BackupDefaultStates()
        {
            defualtStateDic.Clear();
        
            // 备份states中的所有状态
            foreach (var kvp in states.stateDic)
            {
                if (kvp.Value != null)
                {
                    // 使用GOAPStateBase的Copy方法创建深拷贝
                    GOAPStateBase clonedState = kvp.Value.Copy();
                    if (clonedState != null)
                    {
                        defualtStateDic[kvp.Key] = clonedState;
                    }
                }
            }
        
        }
    
        //TODO::可以用Update管理器代理该部分
        public void OnUpdate()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }
        
            if (owner == null)//说明未被初始化
            {
                return;
            }
        
            //计划在执行就不需要去构建任务了
            if (!plan.running)
            {
                SortedList<string,GOAPGoals.Goal> sortedGoals = goals.UpdateGoals();
            
                foreach (var item in sortedGoals)
                {
                    //优先级不是负数，同时可以基于这个目标生成计划
                    if (item.Value.piority >0)
                    {
                        if (GeneratePlan(item.Key,out GOAPPlanNode targetNode))
                        {
                            Debug.Log("目标构建成功"+ item.Key);
                            RunPlan(item.Key,targetNode);
                            break;
                        }
                    }
                } 
            }
            else
            {
                //如果当前目标是可以被中断的，可以尝试找优先级更高的目标
                GOAPGoals.Goal currentGoal = goals.dic[plan.goalName];
                if (currentGoal.canBeBreak)
                {
                    SortedList<string,GOAPGoals.Goal> sortedGoals = goals.UpdateGoals();

                    foreach (var item in sortedGoals)
                    {
                        if (item.Key != plan.goalName
                            && item.Value.canBreak
                            && item.Value.piority > currentGoal.piority
                            && GeneratePlan(item.Key,out GOAPPlanNode targetNode))
                        {
                            Debug.Log("目标被替换为优先级更高的，并构建计划成功"+ item.Key);
                            StopPlan();
                            RunPlan(item.Key,targetNode);
                        }
                    }
                }
                plan.OnUpdate(); 
            }
        }
    
        private void OnDestroy()
        {
            plan.OnDestroy();
            MonoManager.Instance?.RemoveUpdateListener(OnUpdate);
        }

        private void OnEnable()
        {
            foreach (var action in actions.actions)
            {
                if (action.needCooldownStart)
                {
                    action.ApplyCoolDownOnActive();
                }
            }
        }

        #region 状态

        public void ApplyEffect(GOAPTypeAndComparer effect)
        {
            states.ApplyEffect(effect);
        }
    
        public bool CheckStateForPrecondition(GOAPStateType stateType, GOAPStateComparer stateComparer)
        {
            if (GOAPGlobal.Instance.TryGetGlobalState(stateType, out GOAPStateBase state))
            {
                return state.CompareForPrecondition(stateComparer);
            }
            else
            {
                return states.CheckStateForPrecondition(stateType, stateComparer);
            }
        }
        public bool CheckStateForEffect(GOAPStateType stateType, GOAPStateComparer stateComparer)
        {
            if (GOAPGlobal.Instance.TryGetGlobalState(stateType, out GOAPStateBase state))
            {
                return state.CompareForEffect(stateComparer);
            }
            else
            {
                return states.CheckStateForEffect(stateType, stateComparer);
            }
        }

        #endregion
    
        #region 生成计划

        //自定义优先级比较方法
        private class PlanNodePriorityComparer : IComparer<GOAPPlanNode>
        {
            public int Compare(GOAPPlanNode x, GOAPPlanNode y)
            {
                return -x.action.priority.CompareTo(y.action.priority);
            }
        }

        /// <summary>
        /// 通过对象池获取 排序后的计划集合
        /// </summary>
        /// <returns></returns>
        private SortedSet<GOAPPlanNode> GetNodeSortedSet()
        {
            SortedSet<GOAPPlanNode> nodes = GOAPObjectPool.Get<SortedSet<GOAPPlanNode>>();
            if (nodes == null) nodes = new SortedSet<GOAPPlanNode>(new PlanNodePriorityComparer());
            return nodes;
        }

        private void RecycleNodeSortedSet(SortedSet<GOAPPlanNode> nodes)
        {
            foreach (var item in nodes)
            {
                item.Destroy();
            }
            nodes.Clear();
            GOAPObjectPool.Recycle(nodes);
        }

        //comparer为具体的要求
        /// <summary>
        /// 找到符合某个效果的所有行为并形成计划节点
        /// </summary>
        /// <param name="targetStateType"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        private SortedSet<GOAPPlanNode> GetPlanNodesByEffectStateType(GOAPStateType targetStateType, GOAPStateComparer comparer)
        {
            SortedSet<GOAPPlanNode> stateTypeNodes = GetNodeSortedSet();

            if (actions.actionEffectDic.TryGetValue(targetStateType,out List<GOAPActionBase> actionList))
            {
                foreach (var action in actionList)
                {
                    foreach (var effect in action.effects)
                    {
                        if (effect.stateType == targetStateType && effect.stateComparer.EqualsComparer(comparer))
                        {
                            action.UpdatePriority();
                            GOAPPlanNode node =GOAPObjectPool.GetOrNew<GOAPPlanNode>();
                            node.action = action;
                            stateTypeNodes.Add(node);
                            //如果只允许一个effect 可以直接break
                            //break;
                        }
                    }
                }
            }
            return stateTypeNodes;
        }
        /// <summary>
        /// 基于一个源头构建计划路径
        /// 失败的可能性：某个环境中无法达成某个前置条件
        /// </summary>
        /// <param name="startNode"></param>
        /// <returns></returns>
        private bool TryBuildPlanPath(GOAPPlanNode startNode)
        {
            //节点的行为处于冷却时或节点的行为暂未启用时直接返回
            if (startNode.action.onCooldown || !startNode.action.applyAction)
            {
                return false;
            }
        
            //遍历所有条件，必须全部满足才可以进行构建成功
            foreach (var pre in startNode.action.preconditions)
            {
                //当前状态的满足情况
                bool check = CheckStateForPrecondition(pre.stateType, pre.stateComparer);
                if (!check)//当前状态不满足，需要寻找可以满足的其他Action作为子节点
                {
                    SortedSet<GOAPPlanNode> preNodes =GetPlanNodesByEffectStateType(pre.stateType, pre.stateComparer);
                    GOAPPlanNode targetNode = null;
                    foreach (var preItemNode in preNodes)
                    {
                        if (preItemNode!=startNode && TryBuildPlanPath(preItemNode))// preItemNode!=startNode避免自己是自己的前提
                        {
                            targetNode = preItemNode;
                            preItemNode.parent = startNode;
                            preItemNode.indexAtParent = startNode.preconditions.Count;
                            startNode.preconditions.Add(preItemNode);
                            check = true;
                            break;
                        }
                    }

                    if (targetNode != null)
                    {
                        preNodes.Remove(targetNode);
                    }
                
                    RecycleNodeSortedSet(preNodes);
                    if (!check)//意味着当前无法满足条件
                    {
                        return false;
                    }
                }
            }
            return true;
        }

    
        private bool GeneratePlan(string goalName,out GOAPPlanNode targetNode)
        {
            bool success = false;
            GOAPGoals.Goal goal = goals.dic[goalName];
            targetNode = null;
            //遍历所有的效果，如果已经全部满足则没有意义
            if (CheckStateForEffect(goal.targetState, goal.targetValue))
            {
                return false;
            }
            GOAPStateType targetStateType = goal.targetState;
        
            //获取符合效果的全部Action以此尝试构建计划,成功的作为初始Action
            //NOTE::问题点
            SortedSet<GOAPPlanNode> nodes = GetPlanNodesByEffectStateType(targetStateType, goal.targetValue);
            foreach (var node in nodes)
            {
                if (TryBuildPlanPath(node))
                {
                    targetNode = node;
                    node.parent = null;
                    node.indexAtParent = 0;
                    success = true;
                    break;
                }
            }

            if (targetNode != null)
            {
                nodes.Remove(targetNode);
            }
        
            RecycleNodeSortedSet(nodes);
        
            return success;
        }

        #endregion

        #region 执行任务

        private void RunPlan(string goalName,GOAPPlanNode targetNode)
        {
            plan.StartRun(goalName,targetNode);
        
        }

        public void StopPlan()
        {
            plan.Stop();
        }

        /// <summary>
        /// 重置局部状态  放入对象池使用
        /// </summary>
        public void ResetStates()
        {
            // 从默认状态备份中恢复
            foreach (var kvp in defualtStateDic)
            {
                if (kvp.Value is GOAPStateBase state)
                {
                    // 使用Copy()方法创建新的副本，避免引用问题
                    GOAPStateBase clonedState = state.Copy();
                    if (clonedState != null)
                    {
                        states.stateDic[kvp.Key].SetValue(clonedState);
                    }
                }
            }
        }

        /// <summary>
        /// 重置行为信息  放入对象池使用
        /// </summary>
        public void ResetActionInfo()
        {
            foreach (var A in actions.actions)
            {
                A.ResetInfo();
            }
        }

        #endregion
    }
}
