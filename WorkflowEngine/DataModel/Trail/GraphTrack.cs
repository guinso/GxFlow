namespace GxFlow.WorkflowEngine.DataModel.Trail
{
    public class GraphTrack
    {
        public GraphTrack() { }

        public GraphTrack(string diagramID, string fromID, string toID) { 
            DiagramID = diagramID;
            FromID = fromID;
            ToID = toID;
        }

        public string TransactionID { get; set; } = Guid.NewGuid().ToString();

        public string DiagramID { get; set; } = string.Empty;

        public string FromID { get; set; } = string.Empty;

        public string ToID { get; set; } = string.Empty;

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }

    public interface IGraphTracker
    {
        void RegisterTrack(GraphTrack track);

        IEnumerable<string> FindCaller(string diagramID, string destinyID);

        IEnumerable<GraphTrack> Trails { get; }
    }

    public class GraphTracker : IGraphTracker
    {
        protected List<GraphTrack> _tracks = new List<GraphTrack>();

        public IEnumerable<GraphTrack> Trails => _tracks;

        public IEnumerable<string> FindCaller(string diagramID, string destinyID)
        {
            return _tracks.Where(x => x.ToID == destinyID && x.DiagramID == diagramID)
                .Select(y => y.FromID);
        }

        public void RegisterTrack(GraphTrack track)
        {
            _tracks.Add(track);
        }
    }
}