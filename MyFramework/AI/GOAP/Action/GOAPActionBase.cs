using System.Collections.Generic;
using MyFramework.AI.GOAP.Plan;
using MyFramework.AI.GOAP.State;
using Sirenix.OdinInspector;

namespace MyFramework.AI.GOAP.Action
{
    public class GOAPTypeAndComparer
    {
        [OnValueChanged("CheckState")]public GOAPStateType stateType;
        public GOAPStateComparer stateComparer;
#if UNITY_EDITOR
        public void CheckState()
        {
            if (GOAPEditorUtility.global != null 
                && GOAPEditorUtility.global.TryGetGlobalState(stateType,out GOAPStateBase state)
                && (stateComparer == null || stateComparer.GetType() != state.GetComparerType()))
            {
                stateComparer = state.GetComparer();
            }
            else if(GOAPEditorUtility.agent != null 
                    && GOAPEditorUtility.agent.states.TryGetState(stateType,out state)
                    && (stateComparer == null || stateComparer.GetType() != state.GetComparerType()))
            {
                stateComparer = state.GetComparer();
            }
        }
#endif
    }

    public abstract class GOAPActionBase
    {
        [LabelText("前提")] public List<GOAPTypeAndComparer> preconditions = new List<GOAPTypeAndComparer>();
        [LabelText("效果")] public List<GOAPTypeAndComparer> effects = new List<GOAPTypeAndComparer>();
        [LabelText("代价值"),HorizontalGroup("1")] public float costValue;
        [LabelText("效果值"),HorizontalGroup("1")] public float effectValue;
        public void ResetEffectValue() => effectValue = 0;
        public void ResetCostValue() => costValue = 0;
    
        [LabelText("优先级"),ReadOnly,ShowInInspector,HorizontalGroup("1")] public virtual float priority  => effectValue -costValue;
        [ReadOnly]public bool onCooldown = false;
        public void SetCooldown(float cooldown) => onCooldown = cooldown > 0;
        [LabelText("启用事件")] public bool applyAction = true;
    
        protected GOAPAgent agent;

        public virtual void Init(GOAPAgent agent, IGOAPOwner owner)
        {
            this.agent = agent;
        }

        public virtual bool CheckPrecondition()
        {
            foreach (var item in preconditions)
            {
                if (!agent.CheckStateForPrecondition(item.stateType, item.stateComparer))
                {
                    return false;
                }
            }
            return true;
        }
        public virtual bool CheckEffect()
        {
            foreach (var item in effects)
            {
                if (!agent.CheckStateForEffect(item.stateType, item.stateComparer))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual GOAPRunState StartRun()
        {
            if (CheckEffect())
            {
                return GOAPRunState.Succeed;
            }
            else if (CheckPrecondition())
            {
                OnStart();
                return GOAPRunState.Running;
            }
            else
            {
                return GOAPRunState.Failed;
            }
        }
        public virtual void OnStart(){}

        public virtual GOAPRunState OnUpdate() { return default; }

        public virtual void OnStop() { }
        public virtual void OnDestroy() {}

        public void ApplyEffect()
        {
            for (int i = 0; i < effects.Count; i++)
            {
                GOAPTypeAndComparer effect = effects[i];
                if (GOAPGlobal.Instance.TryGetGlobalState(effect.stateType,out GOAPStateBase state))
                {
                    state.ApplyEffect(effect.stateComparer);
                }
                else
                {
                    agent.ApplyEffect(effect);
                }
            }
        }

        public virtual void UpdatePriority()
        {
        
        }

        /// <summary>
        /// 用于计划正常暂停时清空节点间关联时调用
        /// </summary>
        public virtual void Recycle()
        {
        
        }
    
        /// <summary>
        /// 用于对象池回收重置参数
        /// </summary>
        public virtual void ResetInfo()
        {
        
        }
    
        [ToggleLeft,LabelText("激活时冷却")]public bool needCooldownStart = false;
        [ShowIf("needCooldownStart"),LabelText("激活冷却时间")] public float cooldownStart = 0f;
        public virtual void ApplyCoolDownOnActive()
        {
            applyAction = false;
            TimeManager.Instance.CreateTimer(false,(int)(cooldownStart * 1000f), () =>
            {
                applyAction = true;
            });
        }
    }
}