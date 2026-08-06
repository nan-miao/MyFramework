#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyFramework.AI.GOAP
{
    public static class GOAPEditorUtility
    {
        public static GOAPAgent agent;
        public static GOAPGlobal global { get; private set; }

        [InitializeOnLoadMethod] 
        public static void Init()
        {
            TryGetGlobal();
            EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
        }
        private static void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            GetGlobal();
        }
        private static void TryGetGlobal()
        {
            if (global == null) GetGlobal();
        }
        public static GOAPGlobal GetGlobal()
        {
            global = GameObject.FindAnyObjectByType<GOAPGlobal>();
        
            return global;
        }
    }
}
#endif