using System.Collections.Generic;
using MyFramework.AI.GOAP.Action;

namespace MyFramework.AI.GOAP.State
{
    public class GOAPStates 
    {
        public Dictionary<string,GOAPStateBase> stateDic = new Dictionary<string,GOAPStateBase>();

        public bool TrrAddState(GOAPStateType stateType, GOAPStateBase state)
        {
            return stateDic.TryAdd(stateType, state);
        }

        public bool TryRemoveState(GOAPStateType stateType)
        {
            return stateDic.Remove(stateType);
        }

        public T GetState<T>(GOAPStateType stateType) where T : GOAPStateBase
        {
            return (T)stateDic[stateType];
        }

        /*public bool TryGetState(GOAPStateType type, out GOAPStateBase state)
    {
        state =default;
        if (stateDic == null || type.name == null) 
            return false;
        return stateDic.TryGetValue(type, out state);
        
    }*/

        public bool TryGetState<T>(GOAPStateType stateType, out T state) where T : GOAPStateBase
        {
            state = null;
            if (stateDic == null || stateType.name == null) 
                return false;
        
            if (stateDic.TryGetValue(stateType, out GOAPStateBase tempState))
            {
                state = tempState as T;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CheckStateForPrecondition(GOAPStateType stateType, GOAPStateComparer comparer)
        {
            if (TryGetState(stateType,out GOAPStateBase state))
            {
                return state.CompareForPrecondition(comparer);
            }
            return false;
        }
        public bool CheckStateForEffect(GOAPStateType stateType, GOAPStateComparer comparer)
        {
            if (TryGetState(stateType,out GOAPStateBase state))
            {
                return state.CompareForEffect(comparer);
            }
            return false;
        }
    
        public void ApplyEffect(GOAPTypeAndComparer effect)
        {
            if (stateDic.TryGetValue(effect.stateType,out GOAPStateBase value))
            {
                value.ApplyEffect(effect.stateComparer);
            }
        }
    
    }
}
