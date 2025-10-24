using GxFlow.WorkflowEngine.DataModel.Core;
using System.Xml;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Xml
{
    public class XmlHolderArr<T> where T : class, IGraphObj
    {
        public XmlHolderArr()
        {
            ListItems = [];
        }

        public XmlHolderArr(List<T> arr)
        {
            ListItems = arr;
        }

        [XmlIgnore]
        public List<T> ListItems = [];

        [XmlAnyElement]
        public XmlElement[] Items
        {
            get
            {
                return ListItems.Select(XmlHelper.ToXMLElement).ToArray();
            }

            set
            {
                ListItems.Clear();

                foreach (var x in value)
                {
                    if (x.HasAttribute("type") == false)
                    {
                        continue;
                    }

                    var strType = x.GetAttribute("type");
                    if (string.IsNullOrEmpty(strType))
                        continue;

                    Type nodeType = Type.GetType(strType);
                    if (nodeType is null)
                        throw new NullReferenceException($"Unable to resolve type <{strType}>");

                    var xmlStr = x.OuterXml;
                    var tmp = XmlHelper.FromXmlString(xmlStr, nodeType) as T;

                    if (tmp != null)
                    {
                        ListItems.Add(tmp);
                    }
                }
            }
        }
    }
}
