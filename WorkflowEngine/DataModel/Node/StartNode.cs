using GxFlow.WorkflowEngine.DataModel.Core;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Node;

[XmlRoot("node")]
public class StartNode : NodeBase
{
    protected override string GenCodeContext(GraphVariable vars)
    {
        return string.Empty;
    }

    protected override Task RunCleanUp(GraphVariable globalVariables, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    protected override Task RunContext(GraphVariable globalVariables, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    protected override Task RunInit(GraphVariable globalVariables, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}