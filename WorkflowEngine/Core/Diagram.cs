using GxFlow.WorkflowEngine.Node;
using GxFlow.WorkflowEngine.Script;
using GxFlow.WorkflowEngine.Trail;
using GxFlow.WorkflowEngine.Xml;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Core
{
    public interface IDiagram : IGraphObj, IGraphRunnable
    {
        SerializableDictionary<string, object> Variables { get; }

        IEnumerable<INode> Nodes { get; }

        IEnumerable<IFlow> Flows { get; }

        DiagramRunStatus RunStatus { get; }
    }

    public enum DiagramRunStatus
    {
        STOP,
        RUNNING
    }

    public interface IDiagramExt : IDiagram, IScriptTransformer
    {

    }

    [XmlRoot("diagram")]
    [Serializable]
    public abstract class DiagramBase : IDiagramExt
    {
        protected SerializableDictionary<string, object> _variables = new SerializableDictionary<string, object>();

        protected const int WAIT_MS = 5;
        protected string _type = string.Empty;
        protected Task _task = Task.CompletedTask;
        protected INode _startNode;

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

        [XmlIgnore]
        public DiagramRunStatus RunStatus { get; protected set; } = DiagramRunStatus.STOP;

        #region Runnable
        public Task Initialize(GraphVariable vars, CancellationToken token)
        {
            _startNode = FindStartNode();

            vars = MakeVars(vars);

            var tasks = new List<Task>();
            foreach (var node in Nodes)
            {
                tasks.Add(node.Initialize(vars, token));
            }

            Task.WaitAll(tasks);

            return Task.CompletedTask;
        }

        public async Task Run(GraphVariable vars, CancellationToken token)
        {
            if (RunStatus == DiagramRunStatus.RUNNING)
            {
                throw new InvalidOperationException($"cannot start to run diagram({ID}), it is still running");
            }

            if (_startNode is null)
                throw new NullReferenceException("Cannot start run diagram without start node");

            RunStatus = DiagramRunStatus.RUNNING;
            //var logger = GetLogger();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                var track = new GraphTrack(ID, string.Empty, _startNode.ID);
                vars.GraphTracker.RegisterTrack(track);

                await _startNode.Run(track, vars, token);

                while (!token.IsCancellationRequested && RunStatus == DiagramRunStatus.RUNNING)
                {
                    Task.Delay(WAIT_MS).Wait();
                }
            }
            catch (Exception ex)
            {
                //logger.LogError(ex.Message);
                //logger.LogInformation(ex.StackTrace);

                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.StackTrace);

                throw;
            }
            finally
            {
                RunStatus = DiagramRunStatus.STOP;

                sw.Stop();
                Console.WriteLine($"Total diagram rum time: {sw.Elapsed.TotalMilliseconds}ms");
            }
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

        public GraphVariable MakeVars(GraphVariable? vars = null)
        {
            if(vars is null)
                vars = new GraphVariable();

            vars.DiagramID = ID;

            vars.Variables = Variables;

            foreach (var node in Nodes)
            {
                vars.Nodes[node.ID] = node;
            }

            foreach (var flow in Flows)
            {
                vars.Flows.Add(flow);
            }

            vars.EndRun = id => RunStatus = DiagramRunStatus.STOP;

            return vars;
        }
        #endregion

        protected abstract ILogger GetLogger();

        public string ToCSharp(GraphVariable vars)
        {
            string diagramClasName = $"{GetType().Name}_{ID}";

            string inodeTypeName = typeof(INode).FullName;
            string diagramStatusTypeName = typeof(DiagramRunStatus).FullName;
            string serializableTypeName = "GxFlow.WorkflowEngine.Core.SerializableDictionary<string, object>";

            vars = MakeVars(vars);

            string code = @$"
            public class {diagramClasName}: {GetType().Name} {{
                #region node variables
                {GenCodeNodeVariables()}
                #endregion

                #region flow variables
                {GenCodeFlowVariables()}
                #endregion

                public {diagramClasName}()
                {{
                    ID = ""{ID}"";
                    DisplayName = ""{DisplayName}"";
                    Note = ""{Note}"";

                    #region global variables
                    {GenCodeInitVariables()}
                    #endregion

                    {GenCodeConstructorExtra(vars)}
                }}

                {GenCodeExtra(vars)}
            }}

            #region node class definition
            {GenCodeNodeSourceCode(vars)}
            #endregion

            #region flow class definition
            {GenCodeFlowSourceCode(vars)}
            #endregion";

            return code;
        }

        protected virtual string GenCodeNodeVariables()
        {
            var strBuilder = new StringBuilder();

            foreach (var node in Nodes)
            {
                string className = $"{node.GetType().Name}_{node.ID}";

                strBuilder.AppendLine($"protected {className} m_{className} = new {className}();");
            }

            return strBuilder.ToString();
        }

        protected virtual string GenCodeFlowVariables()
        {
            var strBuilder = new StringBuilder();

            foreach (var flow in Flows)
            {
                string className = $"{flow.GetType().Name}_{flow.ID}";

                strBuilder.AppendLine($"protected {className} m_{className} = new {className}();");
            }

            return strBuilder.ToString();
        }

        protected virtual string GenCodeInitVariables()
        {
            var strBuilder = new StringBuilder();

            //initialize Variables
            strBuilder.AppendLine("//variables");
            foreach (var key in Variables.GetKeys())
            {
                var value = Variables[key];

                string codeVal = CSharpHelper.ToCode(value);
                string code = $"Variables[\"{key}\"] = {codeVal};";

                strBuilder.AppendLine(code);
            }
            strBuilder.AppendLine();

            //initialize Nodes
            strBuilder.AppendLine("//nodes");
            foreach (var node in Nodes)
            {
                strBuilder.AppendLine($"XmlNodes.ListItems.Add(m_{node.GetType().Name}_{node.ID});");
            }

            strBuilder.AppendLine();

            //initialize Flows
            strBuilder.AppendLine("//flows");
            foreach (var flow in Flows)
            {
                strBuilder.AppendLine($"XmlFlows.ListItems.Add(m_{flow.GetType().Name}_{flow.ID});");
            }

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

        protected virtual string GenCodeFlowSourceCode(GraphVariable vars)
        {
            var strBuilder = new StringBuilder();

            foreach (var flow in XmlFlows.ListItems)
            {
                var code = flow.ToCSharp(vars);
                strBuilder.AppendLine(code);
            }

            return strBuilder.ToString();
        }

        protected virtual string GenCodeConstructorExtra(GraphVariable vars) { return string.Empty; }

        protected virtual string GenCodeExtra(GraphVariable vars) { return string.Empty; }
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