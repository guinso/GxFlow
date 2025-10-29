using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Trail;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
{
    [XmlRoot("node")]
    public class EndNode : NodeBase
    {
        public override Task Initialize(GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override void RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            vars.EndRun(ID);
        }

        protected override Task RunOutgoing(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        #region code generation


        protected override string GenCodeAssignDynamicInputs(GraphVariable vars)
        {
            return string.Empty;
        }

        protected override string GenCodeRunOutgoing(GraphVariable vars)
        {
            return "return Task.CompletedTask;";
        }
        #endregion
    }
}