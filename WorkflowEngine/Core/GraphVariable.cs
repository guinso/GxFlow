using GxFlow.WorkflowEngine.Trail;

namespace GxFlow.WorkflowEngine.Core
{
    public class GraphVariable
    {
        public string DiagramID { get; set; } = string.Empty;

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

        public Task GotoNode(string nodeID, GraphTrack runInfo, CancellationToken token)
        {
            if (Nodes.ContainsKey(nodeID) == false)
                throw new NullReferenceException($"Target node ID ({nodeID}) not found");
            else if (Flows.Count(x => x.FromID == runInfo.ToID && x.ToID == nodeID) == 0)
                throw new NullReferenceException($"No control flow define from ({runInfo.ToID}), to ({nodeID})");

            var node = Nodes[nodeID];
            var track = new GraphTrack(runInfo.DiagramID, runInfo.ToID, nodeID);
            GraphTracker.RegisterTrack(track);

            return node.Run(track, this, token);
        }
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
