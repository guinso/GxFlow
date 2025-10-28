using GxFlow.WorkflowEngine.DataModel.Trail;
using GxFlow.WorkflowEngine.Script;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface INode : IGraphObj, IGraphRunnableTracker
    {
        public IEnumerable<PropertyInfo> Inputs { get; }

        public IEnumerable<PropertyInfo> Outputs { get; }
    }

    public interface INodeExt: INode, IScriptTransformer
    {

    }

    [XmlRoot("node")]
    public abstract class NodeBase : INodeExt
    {
        protected string _type = string.Empty;

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
        public virtual async Task Run(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            await RunInit(runInfo, vars, token);

            await AssignInputs(runInfo, vars, token);

            await RunContext(runInfo, vars, token);

            await RunCleanUp(runInfo, vars, token);

            await RunOutgoing(runInfo, vars, token);
        }

        protected virtual async Task AssignInputs(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            var ins = Inputs;
            foreach (var inputItem in ins)
            {
                var val = inputItem.GetValue(this) as IGraphProperty;
                if (val != null)
                {
                    await val.EvalValue(runInfo, vars, token);
                }
            }
        }

        protected abstract Task RunInit(GraphTrack runInfo, GraphVariable vars, CancellationToken token);

        protected abstract Task RunCleanUp(GraphTrack runInfo, GraphVariable vars, CancellationToken token);

        protected abstract Task RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token);

        protected virtual async Task RunOutgoing(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            var node = FindNextSingleNode(vars);

            var track = new GraphTrack(runInfo.DiagramID, ID, node.ID);
            vars.GraphTracker.RegisterTrack(track);

            await node.Run(track, vars, token);
        }

        protected virtual INode FindNextSingleNode(GraphVariable vars)
        {
            var nextNodes = vars.SearchNextNode(ID);

            if (nextNodes.Count() == 0)
                throw new Exception($"No flow define for {GetType().Name}({ID})");
            else if (nextNodes.Count() > 1)
                throw new Exception($"More than one flows define for {GetType().Name}({ID})");
            else
            {
                return nextNodes.First();
            }
        }
        #endregion

        #region inputs and outputs
        [XmlIgnore]
        public IEnumerable<PropertyInfo> Inputs { 
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
            string propertyInfoTypeName = typeof(PropertyInfo).FullName;
            string GraphTrackTypeName = typeof(GraphTrack).FullName;
            string varsTypeName = typeof(GraphVariable).FullName;

            return @$"
            public class {GetType().Name}_{ID} : {typeof(INode).FullName}
            {{
                protected List<{propertyInfoTypeName}> _inputProps = new List<{propertyInfoTypeName}>();
                protected List<{propertyInfoTypeName}> _outputProps = new List<{propertyInfoTypeName}>();

                public string ID => ""{ID}"";

                public string TypeName => ""{GetType().Name}_{ID}"";

                public string DisplayName {{ get; set; }} = string.Empty;

                public string Note {{ get; set; }} = string.Empty;

                public {GetType().Name}_{ID}() 
                {{
                    {GenCodePropInfoInit()}
                }}

                public async Task Run({GraphTrackTypeName} RunInfo, {varsTypeName} Vars, CancellationToken token)
                {{
                    //handle input
                    {GenCodeHandleInputAssignment(vars)}

                    //handle process
                    {GenCodeContext(vars)}

                    //handle call next node
                    {GenCodeHandleOutgoing(vars)}              
                }}

                public System.Action<{GetType().Name}_{ID}> OnBegin {{ get; set;}} = (obj) => {{}};

                {GenCodeInputProp()}

                {GenCodeOutputProp()}

                public IEnumerable<{propertyInfoTypeName}> Inputs => _inputProps;

                public IEnumerable<{propertyInfoTypeName}> Outputs => _outputProps;
            }}
            ";
        }

        protected virtual string GenCodePropInfoInit()
        {
            var strBuilder = new StringBuilder();

            foreach (var prop in Inputs)
            {
                strBuilder.AppendLine($"_inputProps.Add(GetType().GetProperty(nameof({prop.Name})));");

            }

            strBuilder.AppendLine();

            foreach (var prop in Outputs)
            {
                strBuilder.AppendLine($"_outputProps.Add(GetType().GetProperty(nameof({prop.Name})));");
            }

            return strBuilder.ToString();
        }

        protected virtual string GenCodeHandleInputAssignment(GraphVariable vars)
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (var propInfo in Inputs)
            {
                var prop = propInfo.GetValue(this) as IGraphProperty;
                if (prop is null)
                    throw new NullReferenceException($"Input property {propInfo.Name} cannot cast to {nameof(IGraphProperty)}");

                if (string.IsNullOrEmpty(prop.BindPath) == false)
                {
                    strBuilder.AppendLine($"{propInfo.Name} = Get{propInfo.Name}(Vars);");
                }
            }

            return strBuilder.ToString();
        }

        protected virtual string GenCodeInputProp()
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine("#region inputs");

            var ins = Inputs;
            foreach (var propInfo in ins)
            {
                var prop = propInfo.GetValue(this) as IGraphProperty;
                if (prop != null)
                {
                    strBuilder.Append($"public {prop.ValueType.FullName} {propInfo.Name} {{ get; set; }}");

                    if (string.IsNullOrEmpty(prop.BindPath))
                    {
                        string codeVal = CSharpHelper.ToCode(prop.GetValue());
                        strBuilder.AppendLine($" = {codeVal};");
                    }
                    else
                    {
                        strBuilder.AppendLine();
                        strBuilder.AppendLine();
                        strBuilder.AppendLine(@$"protected {prop.ValueType.FullName} Get{propInfo.Name}({typeof(GraphVariable).FullName} Vars){{ 
                            {prop.BindPath} 
                        }}");
                    }

                    strBuilder.AppendLine();
                }
            }
            strBuilder.AppendLine("#endregion");

            return strBuilder.ToString();
        }

        protected virtual string GenCodeOutputProp()
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine("#region outputs");

            var outs = Outputs;
            foreach (var prop in outs)
            {
                strBuilder.AppendLine($"public {prop.PropertyType.FullName} {prop.Name} {{ get; set; }}");
                strBuilder.AppendLine();
            }
            strBuilder.AppendLine("#endregion");

            return strBuilder.ToString();
        }

       

        protected virtual string GenCodeHandleOutgoing(GraphVariable vars)
        {
            var node = FindNextSingleNode(vars);
            string graphTrackTypeName = typeof(GraphTrack).FullName;

            return @$"
                var track = new {graphTrackTypeName}(RunInfo.DiagramID, ID, ""{node.ID}"");
                Vars.GraphTracker.RegisterTrack(track);

                await Vars.Nodes[""{ node.ID}""].Run(track, Vars, token);";
        }

        protected abstract string GenCodeContext(GraphVariable vars);
        #endregion
    }
}