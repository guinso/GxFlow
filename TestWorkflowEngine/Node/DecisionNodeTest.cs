using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Node;
using GxFlow.WorkflowEngine.Script;

namespace TestWorkflowEngine.Node
{
    [TestClass]
    public class DecisionNodeTest
    {
        protected (Diagram, DecisionNode, ScriptNode, ScriptNode) MakeDiagram()
        {
            var start = new StartNode();
            var end = new EndNode();
            var decision = new DecisionNode();
            var script1 = new ScriptNode("return 1;");
            var script2 = new ScriptNode("return \"moko\";");

            decision.PreScript.Value = "int j = (int)Vars[\"abc\"];";
            decision.Criteria.Value = [
                new DecisionCriteria("j == 5", script1.ID),
                new DecisionCriteria("j > 10", script2.ID),
            ];

            var diagram = new Diagram();

            diagram.Variables["abc"] = 15;

            diagram.XmlNodes.ListItems = [start, end, decision, script1, script2];

            diagram.XmlFlows.ListItems = [
                new Flow(start, decision),
                new Flow(decision, script1),
                new Flow(decision, script2),
                new Flow(script1, end),
                new Flow(script2, end)
            ];

            return (diagram, decision, script1, script2);
        }

        [TestMethod]
        public void TestXmlSerialization()
        {
            string expectedName = "asd";

            var node = new DecisionNode();
            node.DisplayName = expectedName;
            node.PreScript.Value = "int k = 8;";

            node.Criteria.Value = new List<DecisionCriteria> { 
                new DecisionCriteria("k > 9", "123"),
                new DecisionCriteria("k <= 8", "456")
            };

            TestHelper.XmlSerialize(node, actual =>
            {
                Assert.AreEqual(expectedName, actual.DisplayName);

                Assert.AreEqual(node.Criteria.Value.Count, actual.Criteria.Value.Count);
                Assert.AreEqual(node.PreScript.Value, actual.PreScript.Value);
            });
        }

        [TestMethod]
        public void TestRun()
        {
            var (diagram, decision, script1, script2) = MakeDiagram();
            var vars = new GraphVariable();

            diagram.Initialize(vars, CancellationToken.None).Wait();
            diagram.Run(vars, CancellationToken.None).Wait();

            Assert.IsTrue(vars.GraphTracker.Trails.Count(x => x.FromID == decision.ID && x.ToID == script2.ID) == 1);
        }

        [TestMethod]
        public void TestToCSharp()
        {
            var (diagram, decision, script1, script2) = MakeDiagram();

            var vars = diagram.MakeVars();
            var code = diagram.ToCSharp(vars);
            var fullCode = CSharpHelper.GenerateNamespace(diagram.ID, code);

            var (appDomain, obj) = CSharpHelper.CompileAndLoadInstance([fullCode], $"GxFlow.WorkflowEngine.Compiled_{diagram.ID}.Diagram_{diagram.ID}");
            var instance = obj as Diagram;
            Assert.IsNotNull(instance);

            vars = instance.MakeVars();
            instance.Initialize(vars, CancellationToken.None).Wait();
            instance.Run(vars, CancellationToken.None).Wait();

            Assert.IsTrue(vars.GraphTracker.Trails.Count(x => x.FromID == decision.ID && x.ToID == script2.ID) == 1);

            appDomain.Unload();
        }
    }
}
