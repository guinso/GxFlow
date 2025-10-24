using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Node;
using GxFlow.WorkflowEngine.DataModel.Script;
using System.Runtime.Loader;
using System.Text;
using System.Xml.Serialization;

namespace TestWorkflowEngine.DataModel.Core
{
    [TestClass]
    public class DiagramTest
    {
        [TestMethod]
        public void TestXmlSerialization() 
        {
            var expected = new Diagram();
            expected.Nodes.Add(new StartNode { ID = "123" });
            expected.Nodes.Add(new ScriptNode
            {
                ID = "456",
                Script = new GraphProperty<string> { Value = "int k = 7 + 6; Result = k;" },
            });
            expected.Nodes.Add(new EndNode { ID = "789" });

            expected.Flows.Add(new Flow { FromID = "123", ToID = "456" });
            expected.Flows.Add(new Flow { FromID = "456", ToID = "789" });

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
            using(var stream = new MemoryStream(byteArr))
            using(var reader = new StreamReader(stream, Encoding.UTF8))
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
            var diagram = new Diagram();

            diagram.Variables["a"] = 7;
            diagram.Variables["b"] = "John";
            diagram.Variables["c"] = "return 3 + 4";

            diagram.Nodes.Add(new StartNode{ ID = "123" });
            diagram.Nodes.Add(new ScriptNode { 
                ID = "456",
                Script = new GraphProperty<string> { BindPath = "return (string)Vars[\"c\"];" }, //Value = "int k = 7 + 6; return k;" },
            });
            diagram.Nodes.Add(new EndNode{ ID = "789" });

            diagram.Flows.Add(new Flow { FromID = "123", ToID = "456" });
            diagram.Flows.Add(new Flow { FromID = "456", ToID = "789" });

            var vars = diagram.MakeVars();

            var code = diagram.ToCSharp(vars);
            Assert.IsNotNull(code);

            var (asmRaw, _) = CSharpHelper.CompileToDll(code);
            var appDomain = new AssemblyLoadContext("asdsda", true);
            using (var stream = new MemoryStream(asmRaw))
            {
                var asm = appDomain.LoadFromStream(stream);
                Assert.IsNotNull(asm);

                var type = asm.GetType($"GxFlow.WorkflowEngine.Compiled.Diagram_{diagram.ID}");
                Assert.IsNotNull(type);

                var instance = Activator.CreateInstance(type) as IDiagram;
                Assert.IsNotNull(instance);

                var task = instance.Run(new GraphVariable(), CancellationToken.None);
                task.Wait();

                //var methodInfo = type.GetMethod("Run");
                //Assert.IsNotNull(methodInfo);

                //var task = (Task)methodInfo.Invoke(instance, [CancellationToken.None]);
                //Assert.IsNotNull(task);
                //task.Wait();
            }
        }
    }
}