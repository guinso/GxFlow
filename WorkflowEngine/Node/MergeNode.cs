using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Trail;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
{
    [XmlRoot("node")]
    public class MergeNode : NodeBase
    {
        [XmlElement("receives")]
        [GraphInput("receives")]
        public GraphProperty<List<string>> Receives { get; set; } = new GraphProperty<List<string>>(new List<string>());

        protected Dictionary<string, int> _receiveCounter = new Dictionary<string, int>();
        protected object _locker = new object();

        #region runnable
        public override async Task Initialize(GraphVariable vars, CancellationToken token)
        {
            if (string.IsNullOrEmpty(Receives.BindPath) == false)
                await Receives.EvalValue(new GraphTrack(), vars, token);

            _receiveCounter.Clear();
            foreach (var receive in Receives.Value)
            {
                _receiveCounter[receive] = 0;
            }

            await base.Initialize(vars, token);
        }

        public override Task Run(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            lock (_locker)
            {
                RunContext(runInfo, vars, token);
            }

            return RunOutgoing(runInfo, vars, token);
        }

        protected override void RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            if (_receiveCounter.ContainsKey(runInfo.FromID) == false)
                throw new NullReferenceException($"caller node({runInfo.FromID}) not specify in paramter Receives");

            _receiveCounter[runInfo.FromID]++;
        }

        protected override Task RunOutgoing(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            int sumOfReceives = _receiveCounter.Where(x => x.Value > 0).Sum(x => 1);
            if (sumOfReceives == Receives.Value.Count)
            {
                //clear counter
                foreach (var (k, v) in _receiveCounter)
                {
                    _receiveCounter[k]--;
                }

                //TODO: log proceed next node

                return base.RunOutgoing(runInfo, vars, token);
            }
            else
            {
                //TODO: log not proceed next node
            }

            return Task.CompletedTask;
        }
        #endregion
    }
}