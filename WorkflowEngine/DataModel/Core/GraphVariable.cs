using GxFlow.WorkflowEngine.DataModel.Trail;

namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public class GraphVariable
    {
        public IGraphTracker GraphTracker { get; set; } = new GraphTracker();

        #region variables
        public SerializableDictionary<string, object> Variables { get; set; } = new SerializableDictionary<string, object>();

        public object this[string key]
        {
            get => Variables[key];
            set => Variables[key] = value;
        }
        #endregion

        #region flow handler
        public Dictionary<string, INode> Nodes { get; set; } = new Dictionary<string, INode>();

        public List<IFlow> Flows { get; set; } = new List<IFlow>();

        public IEnumerable<INode> SearchNextNode(string nodeID)
        {
            var nextNodesId = Flows
                .Where(x => x.FromID == nodeID)
                .Select(x => x.ToID);

            var nextNodes = Nodes
                .Where(x => nextNodesId.Contains(x.Value.ID))
                .Select(y => y.Value);

            return nextNodes;
        }

        public bool HasNode(string nodeID)
        {
            return Nodes.ContainsKey(nodeID);
        }

        public Action<string> EndRun { get; set; } = (nodeID) => { };
        #endregion
    }

    public class GraphVariableWrapper
    {
        public GraphVariableWrapper(GraphTrack runInfo, GraphVariable vars)
        {
            Vars = vars;
            RunInfo = runInfo;
        }

        public GraphVariable Vars { get; set; }

        public GraphTrack RunInfo { get; set; }
    }
}
