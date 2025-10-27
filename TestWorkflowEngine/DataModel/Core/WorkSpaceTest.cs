using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Node;
using GxFlow.WorkflowEngine.DataModel.Script;

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
    }
}