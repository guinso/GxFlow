using System.Xml;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Core
{
    public interface IFlow : IGraphObj
    {
        public string FromID { get; set; }

        public string ToID { get; set; }
    }

    public interface IFlowExt : IFlow, IScriptTransformer
    {

    }

    [XmlRoot("flow")]
    public abstract class FlowBase : IFlowExt
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

        [XmlAttribute("from")]
        public string FromID { get; set; } = string.Empty;

        [XmlAttribute("to")]
        public string ToID { get; set; } = string.Empty;

        public string ToCSharp(GraphVariable vars)
        {
            return $@"
            public class {GetType().Name}_{ID} : {GetType().Name} 
            {{
                public {GetType().Name}_{ID}() {{
                    ID = ""{ID}"";
                    DisplayName = ""{DisplayName}"";
                    Note = ""{Note}"";

                    FromID = ""{FromID}"";
                    ToID = ""{ToID}"";
                }}
            }}";
        }
    }

    [XmlRoot("flow")]
    public class Flow : FlowBase
    {
        public Flow()
        {

        }

        public Flow(string fromID, string toID)
        {
            FromID = fromID;
            ToID = toID;
        }

        public Flow(INode fromNode, INode toNode)
        {
            FromID = fromNode.ID;
            ToID = toNode.ID;
        }
    }
}
