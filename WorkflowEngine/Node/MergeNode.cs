using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Trail;
using System.Text;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
{
    [XmlRoot("node")]
    public class MergeNode : NodeBase
    {
        protected Dictionary<string, int> _receiveCounter = new Dictionary<string, int>();
        protected IEnumerable<IFlow> _receiversCache = Array.Empty<IFlow>();
        protected int _receiverCount = 0;
        protected object _locker = new object();

        #region runnable
        public override async Task Initialize(GraphVariable vars, CancellationToken token)
        {
            _receiversCache = vars.Flows.Where(x => x.ToID == ID);
            _receiverCount = _receiversCache.Count();

            _receiveCounter.Clear();
            foreach (var receiver in _receiversCache)
            {
                _receiveCounter[receiver.FromID] = 0;
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
            if (sumOfReceives == _receiverCount)
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