using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Trail;
using System.Text;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
{
    [XmlRoot("node")]
    public class ForkNode : NodeBase
    {
        protected IEnumerable<IFlow> _targets = Array.Empty<IFlow>();

        protected override void InitOutgoingNode(GraphVariable vars)
        {
            var targets = vars.Flows.Where(x => x.FromID == ID);
            foreach(var target in targets)
            {
                if(vars.HasNode(target.ToID) == false)
                    throw new NullReferenceException($"Node {target.ToID} not found");
            }

            _targets = targets;
        }

        #region runnable
        protected override void RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            foreach (var item in _targets)
            {
                vars.GotoNode(item.ToID, runInfo, token);
            }
        }

        protected override Task RunOutgoing(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }
        #endregion
    }
}