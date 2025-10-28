using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Trail;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
{
    [XmlRoot("node")]
    public class EndNode : NodeBase
    {
        protected override string GenCodeContext(GraphVariable vars)
        {
            return "Vars.EndRun(ID);" 
                + Environment.NewLine 
                + "await Task.Delay(5);";
        }

        protected override Task RunCleanUp(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            vars.EndRun(ID);
            return Task.Delay(5);
        }

        protected override Task RunInit(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
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