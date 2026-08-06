using MyFramework.AI.GOAP.State;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFramework.AI.GOAP
{
    public class GOAPGlobal : SerializedMonoBehaviour
    {
        public static GOAPGlobal Instance{get; private set;}

        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] private GOAPStates globalStates;
        public GOAPStates GlobalStates => globalStates;
    
        public bool TryGetGlobalState(string targetSate,out GOAPStateBase state)
        {
            state = null;
            if (globalStates == null || globalStates.stateDic == null) return false;
        
            return globalStates.TryGetState(targetSate,out state);
        }
    
    }
}
