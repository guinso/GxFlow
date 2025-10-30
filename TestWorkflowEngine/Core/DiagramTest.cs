using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Node;
using GxFlow.WorkflowEngine.Script;
using System.Text;
using System.Xml.Serialization;

namespace TestWorkflowEngine.Core
{
    [TestClass]
    public class DiagramTest
    {
        protected Diagram MakeDiagramInstance()
        {
            var diagram = new Diagram();

            diagram.Variables["a"] = 7;
            diagram.Variables["b"] = "John";
            diagram.Variables["c"] = "return 3 + 4;";

            diagram.XmlNodes.ListItems.Add(new StartNode { ID = "123" });
            diagram.XmlNodes.ListItems.Add(new ScriptNode
            {
                ID = "456",
                Script = new GraphProperty<string> { BindPath = "return (string)Vars[\"c\"];" }, // Value = "int k = 7 + 6; return k;" } //  
            });
            diagram.XmlNodes.ListItems.Add(new EndNode { ID = "789" });

            diagram.XmlFlows.ListItems.Add(new Flow { FromID = "123", ToID = "456" });
            diagram.XmlFlows.ListItems.Add(new Flow { FromID = "456", ToID = "789" });

            return diagram;
        }

        [TestMethod]
        public void TestXmlSerialization()
        {
            var expected = new Diagram();
            expected.XmlNodes.ListItems.Add(new StartNode { ID = "123" });
            expected.XmlNodes.ListItems.Add(new ScriptNode
            {
                ID = "456",
                Script = new GraphProperty<string> { Value = "int k = 7 + 6; Result = k;" },
            });
            expected.XmlNodes.ListItems.Add(new EndNode { ID = "789" });

            expected.XmlFlows.ListItems.Add(new Flow { FromID = "123", ToID = "456" });
            expected.XmlFlows.ListItems.Add(new Flow { FromID = "456", ToID = "789" });

            string rawXML = string.Empty;

            var serializer = new XmlSerializer(typeof(Diagram));
            using (var stream = new MemoryStream())
            using (var writter = new StreamWriter(stream, Encoding.UTF8))
            {
                serializer.Serialize(writter, expected);
                rawXML = Encoding.UTF8.GetString(stream.ToArray());
            }

            Diagram actual;

            var byteArr = Encoding.UTF8.GetBytes(rawXML);
            using (var stream = new MemoryStream(byteArr))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                actual = serializer.Deserialize(reader) as Diagram;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Nodes.Count(), actual.Nodes.Count());
            Assert.AreEqual(expected.Flows.Count(), actual.Flows.Count());
        }

        [TestMethod]
        public void TestToCSharp()
        {
            var diagram = MakeDiagramInstance();

            var vars = diagram.MakeVars();

            var code = diagram.ToCSharp(vars);
            Assert.IsNotNull(code);

            var fullCode = CSharpHelper.GenerateNamespace(diagram.ID, code);

            var (appDomain, obj) = CSharpHelper.CompileAndLoadInstance([fullCode], $"GxFlow.WorkflowEngine.Compiled_{diagram.ID}.Diagram_{diagram.ID}");
            var instance = obj as Diagram;
            Assert.IsNotNull(instance);

            vars = instance.MakeVars();
            instance.Initialize(vars, CancellationToken.None).Wait();
            instance.Run(vars, CancellationToken.None).Wait();

            appDomain.Unload();
        }

        [TestMethod]
        public void TestRun()
        {
            var diagram = MakeDiagramInstance();
            var vars = diagram.MakeVars();

            diagram.Initialize(vars, CancellationToken.None).Wait();
            diagram.Run(vars, CancellationToken.None).Wait();
        }
    }
}