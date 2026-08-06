using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MyFramework.Util.Editor
{
    public static class SOUtil
    {
        public static List<T> GetAllInstances<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var assets = new List<T>(guids.Length);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) assets.Add(asset);
            }

            return assets;
        }
    }
}