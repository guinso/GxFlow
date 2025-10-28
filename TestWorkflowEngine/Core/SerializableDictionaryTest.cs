using GxFlow.WorkflowEngine.Core;
using System.Text;
using System.Xml.Serialization;

namespace TestWorkflowEngine.Core
{
    [XmlRoot]
    public class MockDictionaryWrapper
    {
        [XmlElement("dictionary")]
        public SerializableDictionary<string, int> Vars { get; set; } = new SerializableDictionary<string, int>();
    }

    [TestClass]
    public class SerializableDictionaryTest
    {
        [TestMethod]
        public void TestSerializeXML()
        {
            var vars = new MockDictionaryWrapper();
            vars.Vars["koko"] = 3;
            vars.Vars["jojo"] = 10;


            string rawXML = string.Empty;

            var serializer = new XmlSerializer(typeof(MockDictionaryWrapper));
            using (var stream = new MemoryStream())
            using(var writter = new StreamWriter(stream, Encoding.UTF8))
            {
                serializer.Serialize(writter, vars);
                stream.Position = 0;

                rawXML = Encoding.UTF8.GetString(stream.ToArray());
            }

            byte[] buffer = Encoding.UTF8.GetBytes(rawXML);
            using(var stream = new MemoryStream(buffer))
            using (var reader = new StreamReader(stream))
            {
                var obj = serializer.Deserialize(reader) as MockDictionaryWrapper;
                Assert.IsNotNull(obj);
                Assert.AreEqual(3, obj.Vars["koko"]);
                Assert.AreEqual(10, obj.Vars["jojo"]);
            }
        }

        [TestMethod]
        public void TestHasKey()
        {
            var vars = new SerializableDictionary<string, int>();
            vars["koko"] = 3;
            vars["jojo"] = 10;

            Assert.IsTrue(vars.HasKey("koko"));
        }
    }
}
