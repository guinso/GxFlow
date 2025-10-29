using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Script;
using GxFlow.WorkflowEngine.Trail;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
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
        public object Result { get; protected set; } = new object();

        protected override void RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            var task = CSharpScript.RunAsync(Script.Value, ScriptOptions.Default,
                new GraphVariableWrapper(runInfo, vars), typeof(GraphVariableWrapper), token);

            task.Wait();

            Result = task.Result.ReturnValue;
        }

        #region code generation
        protected override string GenCodeRunContext(GraphVariable vars)
        {
            if (string.IsNullOrEmpty(Script.BindPath))
            {
                return @$"Result = RunScript(RunInfo, Vars, token);";
            }
            else
            {
                return $@"var task = {typeof(CSharpHelper).FullName}.Eval<{Result.GetType().FullName}>(Script.Value, RunInfo, Vars, token);
                task.Wait();

                Result = task.Result;";
            }
        }

        protected override string GenCodeExtra(GraphVariable vars)
        {
            if (string.IsNullOrEmpty(Script.BindPath))
            {
                return $@"protected object RunScript(GraphTrack RunInfo, GraphVariable Vars, CancellationToken token){{
                    {Script.Value}
                }}";
            }
            else
            {
                return string.Empty;
            }
        }
        #endregion
    }
}