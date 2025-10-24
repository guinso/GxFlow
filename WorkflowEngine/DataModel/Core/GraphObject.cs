namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface IGraphObj
    {
        string ID { get; }

        string TypeName { get; }
        
        string DisplayName { get; set; }

        string Note { get; set; }
    }

    public interface IGraphRunnable
    {
        Task Run(GraphVariable vars, CancellationToken token);

        Task GetTaskStatus();
    }

    public interface IScriptTransformer
    {
        string ToCSharp(GraphVariable vars);
    }
}