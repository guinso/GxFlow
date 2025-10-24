using System.Xml;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface IFlow : IGraphObj
    {
        public string FromID { get; set; }

        public string ToID { get; set; }
    }

    public interface IFlowExt: IFlow
    {

    }

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

        public abstract void OnDeserialization(object? sender);

        //public virtual XmlElement ToXmlElement()
        //{
        //    var xmlDoc = new XmlDocument();
        //    var root = xmlDoc.CreateElement("flow");

        //    var props = GetType().GetProperties();
        //    foreach (var prop in props)
        //    {
        //        if (prop.GetCustomAttribute<XmlIgnoreAttribute>() != null)
        //            continue;

        //        var attr = prop.GetCustomAttribute<XmlAttributeAttribute>();
        //        if( attr != null )
        //        {
        //            var xmlAttr = xmlDoc.CreateAttribute(attr.AttributeName);
        //            var val = prop.GetValue(this);
        //            if( val != null )
        //            {
        //                xmlAttr.Value = val.ToString();
        //            }
        //            else
        //            {
        //                xmlAttr.Value = string.Empty;
        //            }
        //        }
        //    }

        //    var idAttr = xmlDoc.CreateAttribute("id");
        //    idAttr.Value = ID;
        //    root.Attributes.Append(idAttr);

        //    return root;
        //}
    }

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

        public override void OnDeserialization(object? sender)
        {
            return;
        }
    }
}
