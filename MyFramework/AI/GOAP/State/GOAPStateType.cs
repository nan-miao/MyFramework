using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace MyFramework.AI.GOAP.State
{
    [HideLabel]
    public struct GOAPStateType
    {
        [HideLabel,ValueDropdown("GetAllState")]public string name;
        public static implicit operator GOAPStateType(string s) { return new GOAPStateType { name = s }; }
        public static implicit operator string(GOAPStateType s) { return s.name; }

        #region Editor

#if UNITY_EDITOR
        private List<string> GetAllState()
        {
            List<string> res = new List<string>();
            GOAPGlobal global = GOAPEditorUtility.GetGlobal();
        
            //获取全局的StateType
            if (global != null && global.GlobalStates != null && global.GlobalStates.stateDic!= null)
            {
                foreach (var item in global.GlobalStates.stateDic)
                {
                    res.Add(item.Key);
                }
            }

            //获取GOAPAgent局部的StateType
            if (GOAPEditorUtility.agent != null && GOAPEditorUtility.agent.states != null && GOAPEditorUtility.agent.states.stateDic != null)
            {
                foreach (var item in GOAPEditorUtility.agent.states.stateDic)
                {
                    res.Add(item.Key);
                }
            }
        
            return res;
        }
#endif

        #endregion
    }
}
