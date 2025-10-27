using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Trail;
using GxFlow.WorkflowEngine.Script;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Node
{
    [XmlRoot("node")]
    public class ScriptNode : NodeBase
    {
        public ScriptNode() { }

        public ScriptNode(string script)
        {
            Script = new GraphProperty<string>(script);
        }

        [XmlElement("script")]
        [GraphInput(nameof(Script), Description = "C# scripting to evalute in runtime")]
        public GraphProperty<string> Script { get; set; } = new GraphProperty<string>(string.Empty);

        [XmlIgnore]
        [GraphOutput("Output", Description = "general output from script")]
        public object Result { get; private set; } = new object();

        protected override async Task RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            var ret = await CSharpScript.RunAsync(Script.Value, ScriptOptions.Default,
                new GraphVariableWrapper(runInfo, vars), typeof(GraphVariableWrapper), token);

            Result = ret.ReturnValue;
        }

        protected override Task RunCleanUp(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task RunInit(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        #region code generation
        protected override string GenCodeContext(GraphVariable vars)
        {
            if(string.IsNullOrEmpty(Script.BindPath))
            {
                return @$"Result = new Func<object>(() => {{
                    {Script.Value}
                }})();";
            }
            else
            {
                return $"Result = {typeof(CSharpHelper).FullName}.Eval<{Result.GetType().FullName}>(Script, RunInfo, Vars, token);";
            }
                
        }
        #endregion
    }
}