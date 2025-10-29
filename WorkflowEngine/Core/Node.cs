using GxFlow.WorkflowEngine.Script;
using GxFlow.WorkflowEngine.Trail;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Core
{
    public interface INode : IGraphObj, IGraphRunnableTracker
    {
        public IEnumerable<PropertyInfo> Inputs { get; }

        public IEnumerable<PropertyInfo> Outputs { get; }
    }

    public interface INodeExt : INode, IScriptTransformer
    {

    }

    [XmlRoot("node")]
    public abstract class NodeBase : INodeExt
    {
        protected string _type = string.Empty;
        protected INode _outgoingNode;

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

        #region runnable
        public virtual Task Initialize(GraphVariable vars, CancellationToken token)
        {
            InitOutgoingNode(vars);

            return Task.CompletedTask;
        }

        public virtual Task Run(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            AssignInputs(runInfo, vars, token);

            RunContext(runInfo, vars, token);

            return RunOutgoing(runInfo, vars, token);
        }

        protected virtual void InitOutgoingNode(GraphVariable vars)
        {
            var nextNodes = vars.SearchNextNode(ID);

            if (nextNodes.Count() == 0)
                throw new Exception($"No flow define for {GetType().Name}({ID})");
            else if (nextNodes.Count() > 1)
                throw new Exception($"More than one flows define for {GetType().Name}({ID})");
            else
                _outgoingNode = nextNodes.First();
        }

        protected virtual void AssignInputs(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            var ins = Inputs;
            foreach (var inputItem in ins)
            {
                var val = inputItem.GetValue(this) as IGraphProperty;
                if (val != null)
                {
                    val.EvalValue(runInfo, vars, token).Wait();
                }
            }
        }

        protected abstract void RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token);

        protected virtual Task RunOutgoing(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            if (_outgoingNode is null)
                throw new NullReferenceException(nameof(_outgoingNode));

            var track = new GraphTrack(runInfo.DiagramID, ID, _outgoingNode.ID);
            vars.GraphTracker.RegisterTrack(track);

            return _outgoingNode.Run(track, vars, token);
        }
        #endregion

        #region inputs and outputs
        [XmlIgnore]
        public IEnumerable<PropertyInfo> Inputs
        {
            get
            {
                List<PropertyInfo> meta = new List<PropertyInfo>();
                foreach (PropertyInfo prop in GetType().GetProperties())
                {
                    if (prop.CustomAttributes.Count(x => x.AttributeType == typeof(GraphInputAttribute)) > 0)
                        meta.Add(prop);
                }

                return meta;
            }
        }

        [XmlIgnore]
        public IEnumerable<PropertyInfo> Outputs
        {
            get
            {
                List<PropertyInfo> meta = new List<PropertyInfo>();
                foreach (PropertyInfo prop in GetType().GetProperties())
                {
                    if (prop.CustomAttributes.Count(x => x.AttributeType == typeof(GraphOutputAttribute)) > 0)
                        meta.Add(prop);
                }

                return meta;
            }
        }
        #endregion

        #region code generation
        public virtual string ToCSharp(GraphVariable vars)
        {
            return $@"public class {GetType().Name}_{ID} : {GetType().Name} {{
                public {GetType().Name}_{ID}(){{
                    ID = ""{ID}"";
                    DisplayName = ""{DisplayName}"";
                    Note = ""{Note}"";
        
                    {GenCodeInitInputs(vars)}

                    {GenCodeConstructorExtra(vars)}
                }}

                public override Task Run(GraphTrack RunInfo, GraphVariable Vars, CancellationToken token)
                {{
                    {GenCodeAssignDynamicInputs(vars)}

                    {GenCodeRunContext(vars)}

                    {GenCodeRunOutgoing(vars)}
                }}

                {GenCodeDynamicInputDefinition(vars)}

                {GenCodeExtra(vars)}
            }}";
        }

        protected virtual string GenCodeInitInputs(GraphVariable vars)
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (var propInfo in Inputs)
            {
                var prop = propInfo.GetValue(this) as IGraphProperty;
                if (prop is null)
                    throw new NullReferenceException($"Input property {propInfo.Name} cannot cast to {nameof(IGraphProperty)}");

                string codeValue = CSharpHelper.ToCode(prop.GetValue());
                strBuilder.AppendLine($"{propInfo.Name}.Value = {codeValue};");
            }

            return strBuilder.ToString();
        }

        protected virtual string GenCodeAssignDynamicInputs(GraphVariable vars)
        {
            StringBuilder strBuilder = new StringBuilder();
            var ins = Inputs;
            foreach (var propInfo in ins)
            {
                var prop = propInfo.GetValue(this) as IGraphProperty;
                if (prop != null)
                {
                    if (string.IsNullOrEmpty(prop.BindPath) == false)
                    {
                        strBuilder.AppendLine(@$"{propInfo.Name}.Value = Get{propInfo.Name}(Vars);");
                        strBuilder.AppendLine();
                    }
                }
            }

            return strBuilder.ToString();
        }

        protected virtual string GenCodeRunContext(GraphVariable vars)
        {
            return "RunContext(RunInfo, Vars, token);";
        }

        protected virtual string GenCodeRunOutgoing(GraphVariable vars)
        {
            return "return RunOutgoing(RunInfo, Vars, token);";
        }

        protected virtual string GenCodeDynamicInputDefinition(GraphVariable vars)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine("#region inputs");

            var ins = Inputs;
            foreach (var propInfo in ins)
            {
                var prop = propInfo.GetValue(this) as IGraphProperty;
                if (prop != null)
                {
                    if (string.IsNullOrEmpty(prop.BindPath) == false)
                    {
                        strBuilder.AppendLine(@$"protected {CSharpHelper.ToTypeName(prop.ValueType)} Get{propInfo.Name}(GraphVariable Vars){{ 
                            {prop.BindPath} 
                        }}");
                        strBuilder.AppendLine();
                    }
                }
            }
            strBuilder.AppendLine("#endregion");

            return strBuilder.ToString();
        }

        protected virtual string GenCodeConstructorExtra(GraphVariable vars) { return string.Empty; }

        protected virtual string GenCodeExtra(GraphVariable vars) { return string.Empty; }
        #endregion
    }
}