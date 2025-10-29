using GxFlow.WorkflowEngine.Trail;

namespace GxFlow.WorkflowEngine.Core
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
        Task Initialize(GraphVariable vars, CancellationToken token);

        Task Run(GraphTrack runInfo, GraphVariable vars, CancellationToken token);
    }

    public interface IGraphRunnable
    {
        Task Initialize(GraphVariable vars, CancellationToken token);

        Task Run(GraphVariable vars, CancellationToken token);
    }

    public interface IScriptTransformer
    {
        string ToCSharp(GraphVariable vars);
    }
}