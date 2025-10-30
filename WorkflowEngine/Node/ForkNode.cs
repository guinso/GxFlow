using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Trail;
using System.Text;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
{
    [XmlRoot("node")]
    public class ForkNode : NodeBase
    {
        [XmlElement("targets")]
        [GraphInput("targets")]
        public GraphProperty<List<string>> Targets { get; set; } = new GraphProperty<List<string>>(new List<string>());

        protected override void InitOutgoingNode(GraphVariable vars)
        {
            return;
        }

        #region runnable
        protected override void RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            foreach (var item in Targets.Value)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                else if (vars.HasNode(item) == false)
                {
                    throw new NullReferenceException($"Node {item} not found");
                }
                else if (vars.SearchNextNode(ID).Count(x => x.ID == item) == 0)
                {
                    throw new NullReferenceException($"No matching flow found; from ID {ID}, to ID {item}");
                }

                vars.GotoNode(item,runInfo, token);
            }
        }

        protected override Task RunOutgoing(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }
        #endregion

        #region code generation
        protected override string GenCodeRunContext(GraphVariable vars)
        {
            var strBuilder = new StringBuilder();

            foreach (var item in Targets.Value)
            {
                if (vars.HasNode(item) == false)
                {
                    throw new NullReferenceException($"Node {item} not found");
                }
                else if (vars.SearchNextNode(ID).Count(x => x.ID == item) == 0)
                {
                    throw new NullReferenceException($"No matching flow found; from ID {ID}, to ID {item}");
                }

                strBuilder.AppendLine($"var node_{item} = Vars.Nodes[\"{item}\"];");
                strBuilder.AppendLine($"var track_{item} = new {typeof(GraphTrack).FullName}(RunInfo.DiagramID, ID, node_{item}.ID);");
                strBuilder.AppendLine($"Vars.GraphTracker.RegisterTrack(track_{item});");
                strBuilder.AppendLine($"_ = node_{item}.Run(track_{item}, Vars, token);");
                strBuilder.AppendLine();
            }

            strBuilder.AppendLine("return Task.CompletedTask;");

            return strBuilder.ToString();
        }

        protected override string GenCodeRunOutgoing(GraphVariable vars)
        {
            return string.Empty;
        }
        #endregion
    }
}
