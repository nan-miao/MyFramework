using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MyFramework.Save
{
    [CreateAssetMenu(fileName = "SaveConfig",menuName = "Data/SO/GlobalConfig/SaveConfig")]
    public class SaveConfig : ScriptableObject
    {
        public List<SaveSetting> saveSettings = new List<SaveSetting>();
    }
}