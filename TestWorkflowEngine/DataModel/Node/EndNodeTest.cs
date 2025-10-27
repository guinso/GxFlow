using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Node;
using GxFlow.WorkflowEngine.DataModel.Trail;
using GxFlow.WorkflowEngine.Script;

namespace TestWorkflowEngine.DataModel.Node
{
    [TestClass]
    public class EndNodeTest
    {
        [TestMethod]
        public void TestRun()
        {
            var startNode = new StartNode();
            var endNode = new EndNode();

            var runInfo = new GraphTrack(string.Empty, string.Empty, endNode.ID);

            var vars = new GraphVariable
            {
                Nodes = new Dictionary<string, INode> {
                    { endNode.ID, endNode }
                },
                Flows = new List<IFlow> {
                    new Flow(endNode, startNode)
                }
            };

            endNode.Run(runInfo, vars, CancellationToken.None).Wait();
        }

        [TestMethod]
        public void TestXmlSerialization()
        {
            string expectedName = "asd";

            var node = new EndNode();
            node.DisplayName = expectedName;

            TestHelper.XmlSerialize(node, actual => {
                Assert.AreEqual(expectedName, actual.DisplayName);
            });
        }

        [TestMethod]
        public void TestCompileCode()
        {
            var startNode = new StartNode();
            var endNode = new EndNode();

            var vars = new GraphVariable
            {
                Nodes = new Dictionary<string, INode> {
                    //{ startNode.ID, startNode },
                    //{ endNode.ID, endNode }
                },
                Flows = new List<IFlow> {
                    //new Flow(node, endNode)
                }
            };

            var runInfo = new GraphTrack(string.Empty, string.Empty, endNode.ID);

            var code = endNode.ToCSharp(vars);
            var fullCode = CSharpHelper.GenerateNamespace(endNode.ID, code);
            var (asm, obj) = CSharpHelper.CompileAndLoadInstance(
                [fullCode], $"GxFlow.WorkflowEngine.Compiled_{endNode.ID}.EndNode_{endNode.ID}");

            Assert.IsNotNull(asm);
            Assert.IsNotNull(obj);

            var instance = obj as INode;
            Assert.IsNotNull(instance);

            instance.Run(runInfo, vars, CancellationToken.None).Wait();
        }
    }
}
