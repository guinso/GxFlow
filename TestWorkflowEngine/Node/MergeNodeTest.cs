using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Node;
using GxFlow.WorkflowEngine.Script;

namespace TestWorkflowEngine.Node
{
    [TestClass]
    public class MergeNodeTest
    {
        protected (Diagram, ScriptNode, ScriptNode, ScriptNode) MakeDiagram()
        {
            var startNode = new StartNode();
            var endNode = new EndNode();
            var fork = new ForkNode();
            var merge = new MergeNode();

            var script1 = new ScriptNode("return 3;");
            var script2 = new ScriptNode("return \"abc\";");
            var script3 = new ScriptNode("return 12.67;");

            var diagram = new Diagram();
            diagram.XmlNodes.ListItems = [startNode, endNode, fork, merge, script1, script2, script3];
            diagram.XmlFlows.ListItems = [
                new Flow(startNode, fork),
                new Flow(merge, endNode),
                new Flow(fork, script1),
                new Flow(fork, script2),
                new Flow(fork, script3),
                new Flow(script1, merge),
                new Flow(script2, merge),
                new Flow(script3, merge)
            ];

            return (diagram, script1, script2, script3);
        }

        [TestMethod]
        public async Task TestRun()
        {
            var (diagram, script1, script2, script3) = MakeDiagram();

            var vars = new GraphVariable();

            await diagram.Initialize(vars, CancellationToken.None);
            await diagram.Run(vars, CancellationToken.None);

            Assert.AreEqual(3, (int)script1.Result);
            Assert.AreEqual("abc", (string)script2.Result);
            Assert.AreEqual(12.67, (double)script3.Result);
        }

        [TestMethod]
        public async Task TestToCSharp()
        {
            var (diagram, script1, script2, script3) = MakeDiagram();

            var vars = diagram.MakeVars();
            var code = diagram.ToCSharp(vars);
            Assert.IsNotNull(code);

            var fullCode = CSharpHelper.GenerateNamespace(diagram.ID, code);

            var (appDomain, obj) = CSharpHelper.CompileAndLoadInstance([fullCode], $"GxFlow.WorkflowEngine.Compiled_{diagram.ID}.Diagram_{diagram.ID}");
            var instance = obj as Diagram;
            Assert.IsNotNull(instance);

            vars = instance.MakeVars();
            await instance.Initialize(vars, CancellationToken.None);
            await instance.Run(vars, CancellationToken.None);

            appDomain.Unload();
        }

        [TestMethod]
        public void TestXmlSerialization()
        {
            string expectedName = "asd";

            var node = new MergeNode();
            node.DisplayName = expectedName;

            TestHelper.XmlSerialize(node, actual =>
            {
                Assert.AreEqual(expectedName, actual.DisplayName);
            });
        }
    }
}
