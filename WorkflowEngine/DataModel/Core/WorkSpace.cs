namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface IWorkspace: IGraphObj
    {
        IEnumerable<IDiagram> Diagrams { get; }
    }
}
