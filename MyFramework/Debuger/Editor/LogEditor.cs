using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyFramework.Debuger.Editor
{
    public class LogEditor :EditorWindow
    {
        [MenuItem("Tools/Log/打开日志系统",priority = 1)]
        public static void LoadReport()
        {
            ScriptingDefineSymbols.AddScriptingDefineSymbol("OPEN_LOG");
            GameObject reporter = GameObject.Find("Reporter");
            if (reporter==null)
            { 
                reporter= new GameObject("Reporter");
                reporter.AddComponent<LogSystem>();
                AssetDatabase.SaveAssets();//NOTE::保存资源
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());//NOTE::保存场景
                AssetDatabase.Refresh();//NOTE::刷新资源
                Debug.Log("Open Log Finish!");
            }
        }
   
        [MenuItem("Tools/Log/关闭日志系统",priority = 2)]
        public static void CloseReport()
        {
            ScriptingDefineSymbols.RemoveScriptingDefineSymbol("OPEN_LOG");
            GameObject reporter = GameObject.Find("Reporter");
            if (reporter!=null)
            { 
                GameObject.DestroyImmediate(reporter);
                AssetDatabase.SaveAssets();//NOTE::保存资源
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());//NOTE::保存场景
                AssetDatabase.Refresh();//NOTE::刷新资源
                Debug.Log("Close Log Finish!");
            }
        }
    
        [MenuItem("Tools/Log/打开日志存储路径",priority = 3)]
        static void OpenPersistentDataPath()
        {
            EditorUtility.RevealInFinder(LogConfig.logFileSavePath);
        }
    
        private string logSOFileName="LogConfig";
        private LogConfig logConfig;
        private SerializedObject logSerializer;
        [MenuItem("Tools/Log/设置",priority=4)]  
        static void OpenGUILayoutExample()  
        {
            var window =  GetWindow<LogEditor>("LogSetting");
            window.LoadOrCreatLogConfigSO();
            window.Show();
        }
        private void OnGUI()
        {
            logConfig = (LogConfig)EditorGUILayout.ObjectField(logConfig, typeof(LogConfig), false);
            if (logConfig==null)
                return;
            if (logSerializer == null || logSerializer.targetObject != logConfig)
            {
                logSerializer = new SerializedObject(logConfig);
            }
        
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(logSerializer.FindProperty("openLog"),new GUIContent("开启日志"));
            EditorGUILayout.PropertyField(logSerializer.FindProperty("logHeadFix"),new GUIContent("日志前缀"));
            EditorGUILayout.PropertyField(logSerializer.FindProperty("openTime"),new GUIContent("显示时间"));
            EditorGUILayout.PropertyField(logSerializer.FindProperty("showThreadID"),new GUIContent("显示线程"));
            EditorGUILayout.PropertyField(logSerializer.FindProperty("logSave"),new GUIContent("存储日志"));
            EditorGUILayout.PropertyField(logSerializer.FindProperty("showColorName"),new GUIContent("显示颜色名称"));

            if (EditorGUI.EndChangeCheck())
            {
                logSerializer.ApplyModifiedProperties();
                EditorUtility.SetDirty(logConfig);
                AssetDatabase.SaveAssets();
            }
       
        }

        private void LoadOrCreatLogConfigSO()
        {
            //获取当前编辑器脚本路径
            MonoScript thisScript = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(thisScript);

            if (string.IsNullOrEmpty(scriptPath))
                return;
        
            //构建SO文件路径
            string directory = Path.GetDirectoryName(scriptPath);
            string soFilePath = Path.Combine(directory, logSOFileName + ".asset");
        
            logConfig = AssetDatabase.LoadAssetAtPath<LogConfig>(soFilePath);
        
            // 确保目录存在
            if (logConfig == null)
            {
                logConfig = ScriptableObject.CreateInstance<LogConfig>();
            
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }
            
                AssetDatabase.CreateAsset(logConfig, soFilePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            logSerializer = new SerializedObject(logConfig);
        }
    }
}