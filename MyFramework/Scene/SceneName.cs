using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;

namespace MyFramework.Scene
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [Serializable]
    [InlineProperty]
    public struct SceneName
    {
        [HideLabel] [ValueDropdown("GetSceneName")]
        public string name;

        public static implicit operator SceneName(string s)
        {
            return new SceneName { name = s };
        }

        public static implicit operator string(SceneName s)
        {
            return s.name;
        }

#if UNITY_EDITOR
        private List<string> GetSceneName()
        {
            var res = new List<string>();

            // 获取项目中所有的场景文件
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);

                // 避免重复添加同名场景
                if (!res.Contains(sceneName)) res.Add(sceneName);
            }

            res.Sort();
            return res;
        }
#endif
    }
}