using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Script;
using GxFlow.WorkflowEngine.Trail;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Node
{
    [XmlRoot("node")]
    public class DecisionNode : NodeBase
    {
        protected string _conditionScript = string.Empty;
        protected string _targetNodeID = string.Empty;

        [XmlElement("prescript")]
        [GraphInput("prescript")]
        public GraphProperty<string> PreScript { get; set; } = new GraphProperty<string>();

        [XmlElement("criteria")]
        [GraphInput("criteria")]
        public GraphProperty<List<DecisionCriteria>> Criteria { get; set; } = new GraphProperty<List<DecisionCriteria>>();

        public override Task Initialize(GraphVariable vars, CancellationToken token)
        {
            if (string.IsNullOrEmpty(Criteria.BindPath))
            {
                _conditionScript = GenScriptIfElseLogic();
            }
            
            return Task.CompletedTask;
        }

        protected string GenScriptIfElseLogic()
        {
            var strBuilder = new StringBuilder();
            for (int i = 0; i < Criteria.Value.Count; i++)
            {
                var item = Criteria.Value[i];

                if (i == 0)
                {
                    strBuilder.AppendLine($"if({item.Condition}){{ return \"{item.GotoID}\"; }}");
                }
                else
                {
                    strBuilder.AppendLine($"else if({item.Condition}){{ return \"{item.GotoID}\"; }}");
                }
            }
            strBuilder.AppendLine($"else {{ throw new Exception(\"none of conditions are met\"); }}");

            return strBuilder.ToString();
        }

        #region runnable
        protected override void RunContext(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            if (string.IsNullOrEmpty(Criteria.BindPath) == false)
            {
                _conditionScript = GenScriptIfElseLogic();
            }

            var wrapperVars = new GraphVariableWrapper(runInfo, vars);

            var opt = ScriptOptions.Default
                .AddImports(CSharpHelper.StandardNamespaces);

            var references = CSharpHelper.GetAssemblyReference();
            opt = opt.AddReferences(references);

            var taskScriptState = CSharpScript.RunAsync(PreScript.Value, opt, wrapperVars, typeof(GraphVariableWrapper), token);
            taskScriptState.Wait();

            var scripState = taskScriptState.Result;
            var taskGoTo = scripState.ContinueWithAsync<string>(_conditionScript, opt, token);
            taskGoTo.Wait();

            _targetNodeID = taskGoTo.Result.ReturnValue;
        }

        protected override Task RunOutgoing(GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            return vars.GotoNode(_targetNodeID, runInfo, token);
        }
        #endregion

        #region code generation
        protected override string GenCodeRunContext(GraphVariable vars)
        {
            if(string.IsNullOrEmpty(PreScript.BindPath) && string.IsNullOrEmpty(Criteria.BindPath))
            {
                return "_targetNodeID = RunIfElseLogic(RunInfo, Vars, token);";
            }
            else
            {
                return "RunContext(RunInfo, Vars, token);";
            }
        }

        protected override string GenCodeExtra(GraphVariable vars)
        {
            return $@"
            protected string RunIfElseLogic(GraphTrack RunInfo, GraphVariable Vars, CancellationToken token)
            {{
                {PreScript.Value}

                {GenScriptIfElseLogic()}
            }}
            ";
        }

        protected override string GenCodeRunOutgoing(GraphVariable vars)
        {
            return "return Vars.GotoNode(_targetNodeID, RunInfo, token);";
        }
        #endregion
    }

    [XmlRoot("decision")]
    public class DecisionCriteria
    {
        public DecisionCriteria() { }

        public DecisionCriteria(string condition, string targetNodeID)
        {
            Condition = condition;
            GotoID = targetNodeID;
        }

        [XmlAttribute("condition")]
        public string Condition { get; set; } = string.Empty;

        [XmlAttribute("goto")]
        public string GotoID { get; set; } = string.Empty;

        [XmlAttribute("note")]
        public string Note { get; set; } = string.Empty;
    } 
}