using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Script;
using GxFlow.WorkflowEngine.Trail;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
{
    [XmlRoot("node")]
    public class TriggerNode : NodeBase
    {
        protected bool _forceStop = false;

        [XmlElement("mode")]
        [GraphInput("mode")]
        public GraphProperty<TriggerMode> Mode { get; set; } = new GraphProperty<TriggerMode>(TriggerMode.TIMER);

        [XmlElement("timerdelay")]
        [GraphInput("timerdelay")]
        public GraphProperty<int> TimerDelayMS { get; set; } = new GraphProperty<int>(100);

        [XmlElement("scripteval")]
        [GraphInput("scripteval")]
        public GraphProperty<string> ScriptEvalToTrigger { get; set; } = new GraphProperty<string>(string.Empty);

        protected override void RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            //TODO: support event based trigger mode
            switch (Mode.Value)
            {
                case TriggerMode.TIMER:
                    RunTimerMode(runInfo, vars, token).Wait();
                    break;
                case TriggerMode.EVENT:
                    RunEventMode(runInfo, vars, token).Wait();
                    break;
                default:
                    throw new NotImplementedException($"unhandle trigger mode ({Mode.Value})");
            }
        }

        protected virtual async Task RunTimerMode(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            while (!_forceStop && token.IsCancellationRequested)
            {
                _ = Task.Run(async () => {
                    var isReadyToTrigger = await CSharpHelper.Eval<bool>(ScriptEvalToTrigger.Value, runInfo, vars, token);
                    if (isReadyToTrigger)
                    {
                        _ = vars.GotoNode(_outgoingNode.ID, runInfo, token);
                    }
                });

                await Task.Delay(TimerDelayMS.Value, token);
            }
        }

        protected virtual async Task RunEventMode(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public virtual void StopTimer()
        {
            _forceStop = true;
        }
    }

    public enum TriggerMode
    {
        TIMER,
        EVENT
    }
}