using GxFlow.WorkflowEngine.DataModel.Node;
using GxFlow.WorkflowEngine.DataModel.Script;
using GxFlow.WorkflowEngine.DataModel.Xml;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface IDiagram: IGraphObj, IGraphRunnable
    {
        SerializableDictionary<string, object> Variables { get; }

        IEnumerable<INode> Nodes { get; }

        IEnumerable<IFlow> Flows { get; }
    }

    public interface IDiagramExt: IDiagram, IScriptTransformer
    {
        
    }

    [XmlRoot("diagram")]
    [Serializable]
    public abstract class DiagramBase : IDiagramExt
    {
        protected SerializableDictionary<string, object> _variables = new SerializableDictionary<string, object>();

        protected string _type = string.Empty;
        protected Task _task = Task.CompletedTask;

        [XmlAttribute("id")]
        public string ID { get; set; } = Guid.NewGuid().ToString("N");

        [XmlAttribute("type")]
        public string TypeName
        {
            get
            {
                if (string.IsNullOrEmpty(_type))
                    _type = GetType().FullName;

                return _type;
            }

            set
            {
                _type = value;
            }
        }

        [XmlAttribute("name")]
        public string DisplayName { get; set; } = string.Empty;

        [XmlAttribute("note")]
        public string Note { get; set; } = string.Empty;

        #region properties nodes
        [XmlIgnore]
        public IEnumerable<INode> Nodes => XmlNodes.ListItems.Select(x => x);

        [XmlElement("nodes")]
        public XmlHolderArr<INodeExt> XmlNodes { get; set; } = new XmlHolderArr<INodeExt>();
        #endregion

        #region properties flows
        [XmlIgnore]
        public IEnumerable<IFlow> Flows => XmlFlows.ListItems.Select(x => x);

        [XmlElement("flows")]
        public XmlHolderArr<IFlowExt> XmlFlows { get; set; } = new XmlHolderArr<IFlowExt>();
        #endregion

        [XmlElement("variables")]
        public SerializableDictionary<string, object> Variables => _variables;

        public void OnDeserialization(object? sender)
        {
            return;
        }

        #region Runnable
        public async Task Run(GraphVariable vars, CancellationToken token)
        {
            _task = Task.Run(async () =>
            {
                var startNode = FindStartNode();

                var globalVars = MakeVars();

                await startNode.Run(globalVars, token);
            });

            await _task;
        }

        protected INode FindStartNode()
        {
            var startNodes = Nodes.Where(x => x.GetType().IsAssignableTo(typeof(StartNode)));
            if (startNodes.Count() == 0)
                throw new Exception("Diagram must define a start node");
            else if (startNodes.Count() > 1)
                throw new Exception("Diagram cannot run with multiple start nodes");

            return startNodes.First();
        }

        public Task GetTaskStatus()
        {
            return _task;
        }

        public GraphVariable MakeVars()
        {
            var vars = new GraphVariable { Variables = Variables };
            foreach (var node in Nodes)
            {
                vars.Nodes[node.ID] = node;
            }

            foreach(var flow in Flows)
            {
                vars.Flows.Add(flow);
            }

            return vars;
        }
        #endregion

        protected abstract ILogger GetLogger();

        public string ToCSharp(GraphVariable vars)
        {
            string diagramClasName = $"{GetType().Name}_{ID}";

            string inodeTypeName = typeof(INode).FullName;
            string serializableTypeName = "GxFlow.WorkflowEngine.DataModel.Core.SerializableDictionary<string, object>";
            //string dicNodeTypeName = $"Dictionary<string, {inodeTypeName}>";

            vars = MakeVars();

            string code = @$"
                public class {diagramClasName}: {typeof(IDiagram).FullName} {{

                    protected Task _task = Task.CompletedTask;

                    public string ID => ""{ID}"";

                    public string TypeName => ""{diagramClasName}"";

                    public string DisplayName {{ get; set; }} = string.Empty;

                    public string Note {{ get; set; }} = string.Empty;

                    public {serializableTypeName} Variables {{get; protected set; }} =  new {serializableTypeName}();

                    #region node variables
                    {GenCodeNodeVariables()}
                    #endregion

                    public {diagramClasName}()
                    {{
                        #region global variables
                        {GenCodeGlobalVarInit()}
                        #endregion
                    }}

                    public async Task Run({typeof(GraphVariable).FullName} Vars, CancellationToken token)
                    {{
                        _task = Task.Run(async () => {{
                            {GenCodeRun()}
                        }});

                        await _task;     
                    }}

                    public Task GetTaskStatus()
                    {{
                        return _task;
                    }}
                }}

            #region node class definition
            {GenCodeNodeSourceCode(vars)}
            #endregion";

            return code;
        }

        protected virtual string GenCodeNodeVariables()
        {
            string INodeTypeName = typeof(INode).FullName;
            string IFlowTypeName = typeof(IFlow).FullName;
            string dicNodeTypeName = $"Dictionary<string, {INodeTypeName}>";

            var strBuilder = new StringBuilder();

            foreach (var node in Nodes)
            {
                string className = $"{ node.GetType().Name }_{ node.ID}";

                strBuilder.AppendLine($"protected {className} m_{className} = new {className}();");
            }

            //declare IEnumrable<INode> Nodes
            strBuilder.AppendLine($"public IEnumerable<{INodeTypeName}> Nodes => [");
            foreach (var node in Nodes)
            {
                strBuilder.AppendLine($"m_{node.GetType().Name}_{node.ID},");
                
            }
            strBuilder.AppendLine("];");
            strBuilder.AppendLine();

            //declare Dictionary<string, INode> _nodes
            strBuilder.AppendLine($@"protected {dicNodeTypeName} _nodes = new {dicNodeTypeName}();");

            //declare IEnumrable<IFlow> Flows
            strBuilder.AppendLine($"public IEnumerable<{IFlowTypeName}> Flows => Array.Empty<{IFlowTypeName}>();");

            return strBuilder.ToString();
        }

        protected virtual string GenCodeGlobalVarInit()
        {
            var strBuilder = new StringBuilder();

            //initialize Variables
            foreach (var key in Variables.GetKeys())
            {
                var value = Variables[key];

                string codeVal = CSharpHelper.ToCode(value);
                string code = $"Variables[\"{key}\"] = {codeVal};";

                strBuilder.AppendLine(code);
            }
            strBuilder.AppendLine();

            //initialize _nodes
            foreach (var node in Nodes)
            {

                strBuilder.AppendLine($"_nodes[\"{node.ID}\"] =  m_{node.GetType().Name}_{node.ID};");
            }

            return strBuilder.ToString();
        }

        protected virtual string GenCodeRun()
        {
            var strBuilder = new StringBuilder();

            strBuilder.AppendLine($"var vars = new {typeof(GraphVariable).FullName} {{ Variables = Variables, Nodes = _nodes }};");

            var startNode = FindStartNode();
            var nodeVarName = $"m_{startNode.GetType().Name}_{startNode.ID}";
            strBuilder.AppendLine($"await {nodeVarName}.Run(vars, token);");

            return strBuilder.ToString();
        }

        protected virtual string GenCodeNodeSourceCode(GraphVariable vars)
        {
            var strBuilder = new StringBuilder();

            foreach (var node in XmlNodes.ListItems)
            {
                var code = node.ToCSharp(vars);
                strBuilder.AppendLine(code);
            }

            return strBuilder.ToString();
        }
    }

    [XmlRoot("diagram")]
    [Serializable]
    public class Diagram : DiagramBase
    {
        protected override ILogger GetLogger()
        {
            throw new NotImplementedException();
        }
    }
}