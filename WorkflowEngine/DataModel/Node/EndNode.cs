using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Trail;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Node
{
    [XmlRoot("node")]
    public class EndNode : NodeBase
    {
        protected override string GenCodeContext(GraphVariable vars)
        {
            return "await Task.Delay(5);";
        }

        protected override Task RunCleanUp(GraphTrack runInfo, GraphVariable globalVariables, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task RunContext(GraphTrack runInfo, GraphVariable globalVariables, CancellationToken token)
        {
            return Task.Delay(5);
        }

        protected override Task RunInit(GraphTrack runInfo, GraphVariable globalVariables, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task RunOutgoing(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override string GenCodeHandleOutgoing(GraphVariable vars)
        {
            return string.Empty;
        }
    }
}