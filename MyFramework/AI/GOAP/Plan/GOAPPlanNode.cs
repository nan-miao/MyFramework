using System.Collections.Generic;
using MyFramework.AI.GOAP.Action;

namespace MyFramework.AI.GOAP.Plan
{
    public class GOAPPlanNode
    {
        public GOAPActionBase action; //自身action
        public GOAPPlanNode parent; //父节点
        public List<GOAPPlanNode> preconditions = new List<GOAPPlanNode>(); //前置节点，其实就是子节点
        public int indexAtParent; //自身是父节点的第几个

        public void Destroy()
        {
            if (action == null)//中断避免二次回收
            {
                return;
            }
        
            action = null;
            parent?.Destroy();
            parent = null;
            foreach (var p in preconditions)
            {
                p.Destroy();
            }
            preconditions.Clear();
            GOAPObjectPool.Recycle(this);
        }

        public GOAPRunState Start()
        {
            return action.StartRun();
        }
    
        public GOAPRunState Update()
        {
            return action.OnUpdate();
        }

        public void Stop()
        {
            action.OnStop();
        }
    
    }
}