using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Trail;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node;

[XmlRoot("node")]
public class StartNode : NodeBase
{
    protected override string GenCodeContext(GraphVariable vars)
    {
        return string.Empty;
    }

    protected override string GenCodeExtra(GraphVariable vars)
    {
        return string.Empty;
    }

    protected override Task RunCleanUp(GraphTrack runInfo, GraphVariable globalVariables, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    protected override Task RunContext(GraphTrack runInfo, GraphVariable globalVariables, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    protected override Task RunInit(GraphTrack runInfo, GraphVariable globalVariables, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}