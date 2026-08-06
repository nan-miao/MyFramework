using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFramework.AI.GOAP.Plan
{
    public class GOAPPlan
    {
        public GOAPPlanNode startNode; //最终完成目标效果的节点
        public GOAPPlanNode runingNode; //运行中的节点
        public string goalName; //目标
        [ShowInInspector,ReadOnly]public bool running {get; private set;}
        public GOAPPlanNode stageNode => runingNode.parent; //父节点
        public int runningNodeChildIndex =>runingNode.indexAtParent;
    
        public void StartRun(string goalName,GOAPPlanNode targetNode)
        {
            running = false;
            this.goalName =goalName;
            this.startNode = targetNode;
        
            //找到整个数结构最下层的节点
            StartRunNode(GetDeepestNode(startNode));
        }
    
        public void Stop()
        {
            RecycleNodes(startNode);
            startNode = null;
            running = false;
        }
    
        private GOAPPlanNode GetDeepestNode(GOAPPlanNode startNode)
        {
            if (startNode.preconditions.Count == 0) 
                return startNode;
        
            GOAPPlanNode tempNode = startNode.preconditions[0];
            return GetDeepestNode(tempNode);
        
        }
    
        public void OnUpdate()
        {
            GOAPRunState nodeState = runingNode.Update();

            if (nodeState == GOAPRunState.Succeed) //执行下一个
            {
                runingNode.Stop();
                // 如果完成的是startNode ,代表计划完成
                if (runingNode==startNode)
                {
                    Debug.Log("任务完成");
                    Stop();
                    return;
                }
            
                //有同层可以执行则运行同层的下一个节点
                if (runningNodeChildIndex < stageNode.preconditions.Count-1)
                {
                    StartRunNode(stageNode.preconditions[runningNodeChildIndex + 1]);
                }
                //不存在下一个节点，则运行父节点
                else
                {
                    StartRunNode(stageNode);
                }
            }
            else if(nodeState == GOAPRunState.Failed)
            {
                Stop();
            }
        
            //执行中就不用处理
        }

        private void RecycleNodes(GOAPPlanNode node)
        {
            if (node != null)
            {
                foreach (var item in node.preconditions)
                {
                    RecycleNodes(item);
                }
            
                node.action.Recycle();
                node.action = null;
                node.parent = null;
                node.indexAtParent = 0;
                node.preconditions.Clear();
            }
        }

        private void StartRunNode(GOAPPlanNode node)
        {
            runingNode = node;
            running = runingNode.Start() == GOAPRunState.Running;
            if (!running)
            {
                RecycleNodes(startNode);
            }
        }

        public void OnDestroy()
        {
            if (runingNode!=null)
            {
                runingNode.action?.OnDestroy();
            }

            if (startNode !=null)
            {
                RecycleNodes(startNode);
            }
        }
    
    }
}