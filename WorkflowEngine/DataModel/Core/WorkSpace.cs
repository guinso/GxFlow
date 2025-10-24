using GxFlow.WorkflowEngine.DataModel.Xml;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface IWorkSpace: IGraphObj
    {
        List<IDiagramExt> Diagrams { get; }
    }

    public interface IWorkSpaceExt: IWorkSpace, IScriptTransformer
    {

    }

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
        public List<IDiagramExt> Diagrams { get; protected set; } = new List<IDiagramExt>();

        [XmlElement("diagrams")]
        public XmlHolderArr<IDiagramExt> XmlDiagrams { get; set; } = new XmlHolderArr<IDiagramExt>();

        [XmlAttribute("defaultdiagram")]
        public string DefaultDiagramID { get; set; } = string.Empty;

        public Task GetTaskStatus()
        {
            throw new NotImplementedException();
        }

        public Task Run(GraphVariable vars, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public string ToCSharp(GraphVariable vars)
        {
            throw new NotImplementedException();
        }
    }
}
