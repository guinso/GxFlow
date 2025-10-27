using GxFlow.WorkflowEngine.DataModel.Trail;

namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface IGraphObj
    {
        string ID { get; }

        string TypeName { get; }
        
        string DisplayName { get; set; }

        string Note { get; set; }
    }

    public interface IGraphRunnableTracker
    {
        Task Run(GraphTrack runInfo, GraphVariable vars, CancellationToken token);
    }

    public interface IGraphRunnable
    {
        Task Run(GraphVariable vars, CancellationToken token);
    }

    public interface IScriptTransformer
    {
        string ToCSharp(GraphVariable vars);
    }
}