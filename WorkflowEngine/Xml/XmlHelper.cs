using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.Xml
{
    internal class XmlHelper
    {
        public static string ToXMLString(object obj, bool excludeNamespace = false)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", ""); // Adds an empty namespace, effectively removing default ones


            string rawXML = string.Empty;

            var serializer = new XmlSerializer(obj.GetType());

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                if(excludeNamespace)
                    serializer.Serialize(writer, obj, ns);
                else
                    serializer.Serialize(writer, obj);

                rawXML = Encoding.UTF8.GetString(stream.ToArray());
            }

            //reference: https://stackoverflow.com/questions/17795167/xml-loaddata-data-at-the-root-level-is-invalid-line-1-position-1
            if (rawXML.Length > 0 && rawXML[0] == '\uFEFF') // Check for BOM
            {
                rawXML = rawXML.Substring(1);
            }

            return rawXML;
        }

        public static object FromXmlString(string xmlString, Type objType)
        {
            var serializer = new XmlSerializer(objType);
            var byteArr = Encoding.UTF8.GetBytes(xmlString);

            //reference: https://stackoverflow.com/questions/17795167/xml-loaddata-data-at-the-root-level-is-invalid-line-1-position-1
            using (var stream = new MemoryStream(byteArr))
            using (var reader = new StreamReader(stream, true))
            {
                var ret = serializer.Deserialize(reader);
                if (ret == null)
                    throw new NullReferenceException();

                return ret;
            }
        }

        public static XmlElement ToXMLElement(object obj)
        {
            var xmlString = ToXMLString(obj, true);

            using (StringReader sr = new StringReader(xmlString))
            using (XmlReader reader = XmlReader.Create(sr))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);

                if (doc.DocumentElement is null)
                    throw new NullReferenceException("Failed to convert object to Xml element");

                var element = doc.DocumentElement;

                if (element == null)
                    throw new NullReferenceException();

                return element;
            }
        }
    }
}
