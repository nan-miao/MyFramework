using UnityEditor;
namespace MyFramework.Save.Editor
{
    public class SaveEditorExtensions
    {
        [MenuItem("EditorExtensions/Save/删除所有存档文件")]
        public static void DeleteGameData()
        {
            SaveManager.Instance.DeleteAllSaveData();
        }
    }
}