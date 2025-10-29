using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Trail;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node;

[XmlRoot("node")]
public class StartNode : NodeBase
{
    public override Task Run(GraphTrack RunInfo, GraphVariable Vars, CancellationToken token)
    {
        return RunOutgoing(RunInfo, Vars, token);
    }

    protected override void RunContext(GraphTrack runInfo, GraphVariable globalVariables, CancellationToken token)
    {
        return;
    }

    #region code generation
    protected override string GenCodeAssignDynamicInputs(GraphVariable vars)
    {
        return string.Empty;
    }

    protected override string GenCodeRunContext(GraphVariable vars)
    {
        return string.Empty;
    }
    #endregion
}