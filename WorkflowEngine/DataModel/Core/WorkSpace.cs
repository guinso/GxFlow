using GxFlow.WorkflowEngine.DataModel.Script;
using GxFlow.WorkflowEngine.DataModel.Xml;
using System.Runtime.Loader;
using System.Text;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface IWorkSpace: IGraphObj
    {
        Task Run(CancellationToken token);

        IEnumerable<IDiagram> Diagrams { get; }
    }

    public interface IWorkSpaceExt: IWorkSpace, IScriptTransformer
    {
        
    }

    [XmlRoot("workspace")]
    public abstract class WorkSpaceBase : IWorkSpaceExt
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

        [XmlIgnore]
        public IEnumerable<IDiagram> Diagrams => XmlDiagrams.ListItems.Select(x => x);

        [XmlElement("diagrams")]
        public XmlHolderArr<IDiagramExt> XmlDiagrams { get; set; } = new XmlHolderArr<IDiagramExt>();

        [XmlAttribute("defaultdiagram")]
        public string DefaultDiagramID { get; set; } = string.Empty;

        public async Task Run(CancellationToken token)
        {
            var defaultDiagram = XmlDiagrams.ListItems.First(x => x.ID == DefaultDiagramID);
            if (defaultDiagram == null)
            {
                throw new NullReferenceException($"Default diagram {DefaultDiagramID} not found");
            }

            var vars = new GraphVariable();

            await defaultDiagram.Run(vars, token);
        }

        public string ToCSharp(GraphVariable vars)
        {
            string workspaceClasName = $"{GetType().Name}_{ID}";
            string worksapceTypeName = typeof(IWorkSpace).FullName;

            string code = @$"
            public class {workspaceClasName}: {worksapceTypeName} {{
                    public string ID => ""{ID}"";

                    public string TypeName => ""{workspaceClasName}"";

                    public string DisplayName {{ get; set; }} = string.Empty;

                    public string Note {{ get; set; }} = string.Empty;

                    {GenCodeDeclareDiagrams()}

                    {GenCodeRun()}
            }}

            {GenCodeDiagramSourceCode(vars)}
            ";

            return code;
        }

        protected string GenCodeDeclareDiagrams()
        {
            var strBuilder = new StringBuilder();

            string diagramTypeName = typeof(IDiagram).FullName;

            foreach (var diagram in Diagrams)
            {
                strBuilder.AppendLine($"protected Diagram_{diagram.ID} m_diagram_{diagram.ID} = new Diagram_{diagram.ID}();");
            }

            strBuilder.AppendLine();
            strBuilder.AppendLine($"public IEnumerable<{diagramTypeName}> Diagrams => [");

            foreach (var diagram in Diagrams)
            {
                strBuilder.AppendLine($"m_diagram_{diagram.ID},");
            }
            strBuilder.AppendLine();
            strBuilder.AppendLine("];");

            return strBuilder.ToString();
        }

        protected string GenCodeRun()
        {
            var graphVariableTypeName = typeof(GraphVariable).FullName;

            var defaultDiagram = Diagrams.First(x => x.ID == DefaultDiagramID);
            if (defaultDiagram == null)
            {
                throw new NullReferenceException($"Default diagram {DefaultDiagramID} not found");
            }

            return $@"
            public async Task Run(CancellationToken token) {{
                var vars = new {graphVariableTypeName}();

                await m_diagram_{DefaultDiagramID}.Run(vars, token);
            }}";
        }

        protected string GenCodeDiagramSourceCode(GraphVariable vars)
        {
            var strBuilder = new StringBuilder();

            foreach(var diagram in XmlDiagrams.ListItems)
            {
                var sourceCode = diagram.ToCSharp(vars);

                strBuilder.AppendLine(sourceCode);
                strBuilder.AppendLine();
            }

            return strBuilder.ToString();
        }
    }

    [XmlRoot("workspace")]
    public class WorkSpace : WorkSpaceBase {
        public (AssemblyLoadContext, IWorkSpace) CompileCSharp(AssemblyLoadContext? appDomain = null)
        {
            string sourceCode = ToCSharp(new GraphVariable());
            string fullSourceCode = CSharpHelper.GenerateNamespace(ID, sourceCode);

            string workspaceTypeName = typeof(IWorkSpace).FullName;

            var (app, obj) = CSharpHelper.CompileAndLoadInstance([fullSourceCode], $"GxFlow.WorkflowEngine.Compiled_{ID}.WorkSpace_{ID}", appDomain);
            appDomain = app;

            var instance = obj as IWorkSpace;
            if (instance is null)
            {
                throw new NullReferenceException("failed to cast instance to IWorkSpace type");
            }

            return (appDomain, instance);
        }
    }
}