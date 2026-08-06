using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace MyFramework.AI.GOAP.Editor
{
    [CustomEditor(typeof(GOAPAgent))]//应用与GOAPAgent类
    public class GOAPAgentEditor : OdinEditor //获取选中的GOAPAgent
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            GOAPEditorUtility.agent = (GOAPAgent)target;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GOAPEditorUtility.agent = null;
        }
    }
}
