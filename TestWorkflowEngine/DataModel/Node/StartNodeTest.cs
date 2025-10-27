using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Node;
using GxFlow.WorkflowEngine.DataModel.Script;

namespace TestWorkflowEngine.DataModel.Node
{
    [TestClass]
    public class StartNodeTest
    {
        [TestMethod]
        public void TestRun()
        {
            var node = new StartNode();
            var endNode = new EndNode();

            var vars = new GraphVariable
            {
                Nodes = new Dictionary<string, INode> {
                    { node.ID, node },
                    { endNode.ID, endNode }
                },
                Flows = new List<IFlow> { 
                    new Flow(node, endNode)
                }
            };

            node.Run(vars, CancellationToken.None).Wait();
        }

        [TestMethod]
        public void TestRun2()
        {
            var node = new StartNode();
            
            var vars = new GraphVariable
            {
                Nodes = new Dictionary<string, INode> {
                    { node.ID, node },
                },
                Flows = new List<IFlow>()
            };

            Assert.ThrowsException<AggregateException>(() => {
                node.Run(vars, CancellationToken.None).Wait();
            });
        }

        [TestMethod]
        public void TestXmlSerialization()
        {
            string expectedName = "asd";

            var node = new StartNode();
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
                    { startNode.ID, startNode },
                    { endNode.ID, endNode }
                },
                Flows = new List<IFlow> {
                    new Flow(startNode, endNode)
                }
            };

            var code = startNode.ToCSharp(vars);
            var fullCode = CSharpHelper.GenerateNamespace(startNode.ID, code);
            var (asm, obj) = CSharpHelper.CompileAndLoadInstance(
                [fullCode], $"GxFlow.WorkflowEngine.Compiled_{startNode.ID}.StartNode_{startNode.ID}");

            Assert.IsNotNull(asm);
            Assert.IsNotNull(obj);

            var instance = obj as INode;
            Assert.IsNotNull(instance);

            instance.Run(vars, CancellationToken.None).Wait();
        }
    }
}