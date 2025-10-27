using System.Text;
using System.Xml.Serialization;

namespace TestWorkflowEngine
{
    public class TestHelper
    {
        public static void XmlSerialize<T>(T expected, Action<T> action) where T : class
        {
            string rawXML = string.Empty;

            var serializer = new XmlSerializer(typeof(T));
            using (var stream = new MemoryStream())
            using (var writter = new StreamWriter(stream, Encoding.UTF8))
            {
                serializer.Serialize(writter, expected);
                stream.Position = 0;

                rawXML = Encoding.UTF8.GetString(stream.ToArray());
            }

            byte[] buffer = Encoding.UTF8.GetBytes(rawXML);
            using (var stream = new MemoryStream(buffer))
            using (var reader = new StreamReader(stream))
            {
                var actual = serializer.Deserialize(reader) as T;
                Assert.IsNotNull(actual);

                action(actual);
            }
        }
    }
}
