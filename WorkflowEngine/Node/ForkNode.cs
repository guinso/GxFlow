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

        protected override string GenCodeContext(GraphVariable vars)
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
                strBuilder.AppendLine($"node_{item}.Run(track_{item}, Vars, token);");
                strBuilder.AppendLine();
            }

            return strBuilder.ToString();
        }

        protected override string GenCodeExtra(GraphVariable vars)
        {
            return string.Empty;
        }

        protected override Task RunCleanUp(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
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

                var node = vars.Nodes[item];
                var track = new GraphTrack(runInfo.DiagramID, ID, item);
                vars.GraphTracker.RegisterTrack(track);

                node.Run(track, vars, token);
            }

            return Task.CompletedTask;
        }

        protected override Task RunInit(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
