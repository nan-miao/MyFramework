using MyFramework.AI.GOAP.Plan;
using UnityEditor;
using UnityEngine;

namespace MyFramework.AI.GOAP.Editor
{
   public class GOAPPlanWindow : EditorWindow
   {
      [MenuItem("GOAP/GOAPPlanWindow")]

      static void OpenWindow()
      {
         GetWindow<GOAPPlanWindow>();
      }

      private GOAPPlan plan;
      private Vector2 scrollPosition;
      private void OnGUI()
      {
         if (Selection.gameObjects.Length == 0)
         {
            return;
         }
      
         GameObject go = Selection.gameObjects[0];
         if (go == null) return;
         GOAPAgent agent = go.GetComponent<GOAPAgent>();
         if (agent == null) return;
         plan = agent.plan;
         if (plan == null) return;
         if (plan == null || plan.startNode == null || plan.goalName == null) return;
         GOAPPlanNode startNode = plan.startNode;
         EditorGUILayout.LabelField($"计划:{plan.goalName}");
         scrollPosition = GUILayout.BeginScrollView(scrollPosition);
         Color oldColor = GUI.color;
         PrintNode(startNode);
         GUI.color = oldColor;
         GUILayout.EndScrollView();
      }

      private void PrintNode(GOAPPlanNode node, int depth = 0)
      {
         string prefix = new string(' ', depth * 6);
         string nodeName = $"{prefix}{node.action.GetType().Name}";
         GUI.color = plan.runingNode == node ? Color.red : Color.yellow;
         EditorGUILayout.LabelField(nodeName);

         for (int i = 0; i < node.preconditions.Count; i++)
         {
            PrintNode(node.preconditions[i], depth + 1);
         }
      }
   }
}
