using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Node;
using GxFlow.WorkflowEngine.DataModel.Script;
using System.Text;
using System.Xml.Serialization;

namespace TestWorkflowEngine.DataModel.Core
{
    [TestClass]
    public class WorkSpaceTest
    {
        public WorkSpace MakeWorkSpaceInstance()
        {
            var ws = new WorkSpace();
            var diagram = new Diagram();

            var startNode = new StartNode();
            var endNode = new EndNode();
            var scriptNode = new ScriptNode("int k = 8; return k - 3;");

            diagram.XmlNodes.ListItems.Add(startNode);
            diagram.XmlNodes.ListItems.Add(endNode);
            diagram.XmlNodes.ListItems.Add(scriptNode);
            diagram.XmlFlows.ListItems.Add(new Flow(startNode, scriptNode));
            diagram.XmlFlows.ListItems.Add(new Flow(scriptNode, endNode));

            ws.XmlDiagrams.ListItems.Add(diagram);
            ws.DefaultDiagramID = diagram.ID;

            return ws;
        }

        [TestMethod]
        public void TestRun()
        {
            var ws = new WorkSpace();
            var diagram = new Diagram();

            var startNode = new StartNode();
            var endNode = new EndNode();
            var scriptNode = new ScriptNode("int k = 8; return k - 3;");

            diagram.XmlNodes.ListItems.Add(startNode);
            diagram.XmlNodes.ListItems.Add(endNode);
            diagram.XmlNodes.ListItems.Add(scriptNode);
            diagram.XmlFlows.ListItems.Add(new Flow(startNode, scriptNode));
            diagram.XmlFlows.ListItems.Add(new Flow(scriptNode, endNode));

            ws.XmlDiagrams.ListItems.Add(diagram);
            ws.DefaultDiagramID = diagram.ID;

            ws.Run(CancellationToken.None).Wait();

            Assert.AreEqual(5, (int)scriptNode.Result);
        }

        [TestMethod]
        public void TestToCSharp()
        {
            var ws = MakeWorkSpaceInstance();

            var code = ws.ToCSharp(new GraphVariable());
            Assert.IsTrue(string.IsNullOrWhiteSpace(code) == false);

            var fullCode = CSharpHelper.GenerateNamespace(ws.ID, code);

            var (appDomain, instance) = CSharpHelper.CompileAndLoadInstance([fullCode], $"GxFlow.WorkflowEngine.Compiled_{ws.ID}.WorkSpace_{ws.ID}");
            var workspace = instance as IWorkSpace;

            Assert.IsNotNull(workspace);

            workspace.Run(CancellationToken.None).Wait();

            appDomain.Unload();
        }

        [TestMethod]
        public void TestCompileCSharp()
        {
            var ws = MakeWorkSpaceInstance();

            var (appDomain, expectedWS) = ws.CompileCSharp();

            Assert.IsNotNull(expectedWS);
            Assert.IsNotNull(appDomain);

            expectedWS.Run(CancellationToken.None).Wait();
            appDomain.Unload();
        }

        [TestMethod]
        public void TestXmlSerialization()
        {
            string rawXML = string.Empty;
            var obj = MakeWorkSpaceInstance();
            int expectedNodeCount = obj.Diagrams.First().Nodes.Count();

            
            string expectedScriptContent = "int k = 8; return k - 3;";

            var serializer = new XmlSerializer(typeof(WorkSpace));
            using (var stream = new MemoryStream())
            using (var writter = new StreamWriter(stream, Encoding.UTF8))
            {
                serializer.Serialize(writter, obj);
                stream.Position = 0;

                rawXML = Encoding.UTF8.GetString(stream.ToArray());
            }

            byte[] buffer = Encoding.UTF8.GetBytes(rawXML);
            using (var stream = new MemoryStream(buffer))
            using (var reader = new StreamReader(stream))
            {
                var actual = serializer.Deserialize(reader) as WorkSpace;
                Assert.IsNotNull(actual);

                Assert.AreEqual(expectedNodeCount, actual.Diagrams.First().Nodes.Count());

                var expectedScriptNode = actual.Diagrams.First().Nodes.ElementAt(2) as ScriptNode;
                Assert.IsNotNull(expectedScriptNode);
                Assert.AreEqual(expectedScriptContent, expectedScriptNode.Script.Value);
            }
        }
    }
}