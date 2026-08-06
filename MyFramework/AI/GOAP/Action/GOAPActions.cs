using System.Collections.Generic;
using MyFramework.AI.GOAP.State;
using Sirenix.OdinInspector;

namespace MyFramework.AI.GOAP.Action
{
    public class GOAPActions 
    {
        public List<GOAPActionBase> actions = new List<GOAPActionBase>();
    
        //Value：可以满足GOAPStateType的行为列表 
        public Dictionary<GOAPStateType, List<GOAPActionBase>> actionEffectDic;

        public void Init(GOAPAgent agent, IGOAPOwner owner)
        {
            actionEffectDic = new Dictionary<GOAPStateType,List<GOAPActionBase>>();
            foreach (var action in actions)
            {
                action.Init(agent, owner);
                foreach (var effect in action.effects)
                {
                    AddActionEffect(effect.stateType,action);
                }
            }
        }

        private void AddActionEffect(GOAPStateType stateType, GOAPActionBase action)
        {
            if (!actionEffectDic.TryGetValue(stateType, out List<GOAPActionBase> actions))
            {
                actions = new List<GOAPActionBase>();
                actionEffectDic.Add(stateType, actions);
            }
            actions.Add(action);
        }

#if UNITY_EDITOR
        [Button("检查所有行为状态类型")]
        public void CheckAllActionState()
        {
            foreach (GOAPActionBase action in actions)
            {
                foreach (var pre in action.preconditions)
                {
                    pre.CheckState();
                }
            
                foreach (var effect in action.effects)
                {
                    effect.CheckState();
                }
            }
        }
#endif
    }
}
