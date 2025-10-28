using GxFlow.WorkflowEngine.Core;
using GxFlow.WorkflowEngine.Node;
using GxFlow.WorkflowEngine.Trail;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace TestWorkflowEngine.Node
{
    [TestClass]
    public sealed class ScriptNodeTest
    {
        [TestMethod]
        public async Task TestRun()
        {
            var vars = new GraphVariable();

            var variables = new SerializableDictionary<string, object>();
            variables["abc"] = 4;
            variables["efg"] = "hello";
            //variables["ss"] = "return 78 - 3;";
            vars.Variables = variables;

            var node = new ScriptNode("int k = 3 + 2; return k;");
            node.ID = "123";

            vars.Nodes[node.ID] = node;
            vars.Nodes["456"] = new EndNode { ID = "456" };

            vars.Flows.Add(new Flow { FromID = "123", ToID = "456" });

            
            await node.Run(new GraphTrack(string.Empty, string.Empty, node.ID), vars, CancellationToken.None);
            int ret = (int)node.Result;
            Assert.AreEqual(5, ret);

            node.Script.Value = "return (int)Vars[\"abc\"] + 3;";
            await node.Run(new GraphTrack(string.Empty, string.Empty, node.ID), vars, CancellationToken.None);
            ret = (int)node.Result;
            Assert.AreEqual(7, ret);
        }

        [TestMethod]
        public async Task TestRunWithInputPathBinding()
        {
            var variables = new SerializableDictionary<string, object>();
            variables["abc"] = 4;
            variables["efg"] = @"return ""hello"" + "" world"";";
            variables["ss"] = "return 78 - 3;";

            var node = new ScriptNode("int k = 3 + 2; return k;");
            node.ID = "1";

            var endNode = new EndNode { ID = "2" };

            var vars = new GraphVariable { 
                Variables = variables,
                Flows = [ new Flow(node.ID, endNode.ID)]
            };
            vars.Nodes[node.ID] = node;
            vars.Nodes[endNode.ID] = endNode;

            node.Script.BindPath = "return (string)Vars[\"efg\"];";
            await node.Run(new GraphTrack(string.Empty, string.Empty, node.ID), vars, CancellationToken.None);
            Assert.AreEqual("hello world", node.Result);

            node.Script.BindPath = "return (string)Vars[\"ss\"];";
            await node.Run(new GraphTrack(string.Empty, string.Empty, node.ID), vars, CancellationToken.None);
            Assert.AreEqual(75, node.Result);
        }

        [TestMethod]
        public async Task TestRunChangeGlobalVariables()
        {
            var variables = new SerializableDictionary<string, object>();
            variables["abc"] = 4;
            variables["ss"] = 72;
            variables["script"] = @$"Vars[""abc""] = (int)Vars[""abc""] + 9; 
                return (int)Vars[""ss""] + 3;";

            var node = new ScriptNode("int k = 3 + 2; return k;");
            node.ID = "1";

            node.Script.BindPath = "return (string)Vars[\"script\"];";
            
            var vars = new GraphVariable { 
                Variables = variables,
                Flows = [ new Flow("1", "2") ]
            };
            vars.Nodes["1"] = node;
            vars.Nodes["2"] = new EndNode { ID = "2" };

            await node.Run(new GraphTrack(string.Empty, string.Empty, node.ID), vars, CancellationToken.None);
            Assert.AreEqual(75, node.Result);
            Assert.AreEqual(13, (int)variables["abc"]);
        }

        [TestMethod]
        public async Task TestRunScriptCS()
        {
            var kk = await CSharpScript.RunAsync("int k = 5;");
            var ret = await kk.ContinueWithAsync("return k + 4;");
            //var ret = await CSharpScript.RunAsync("return k + 4;");

            int expected = 9;
            int actual = (int)ret.ReturnValue;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task TestRunMultiScripts()
        {
            var variables = new SerializableDictionary<string, object>();
            variables["a1"] = 4;
            variables["a2"] = "koko jelly";
            variables["s1"] = "return \"hello\" + \" world\";";
            variables["s2"] = "int popo = 78 - 3; Vars[\"a1\"] = popo;";

            var vars = new GraphVariable { Variables = variables };

            var node1 = new ScriptNode("return 3;");
            node1.ID = "1";

            var node2 = new ScriptNode();
            node2.ID = "2";
            node2.Script.BindPath = "return (string)Vars[\"s1\"];";

            var node3 = new ScriptNode(@"
                var msg = ""hello there"";
                System.Console.WriteLine(msg);
            ");
            node3.ID = "3";

            var node4 = new ScriptNode();
            node4.ID = "4";
            node4.Script.BindPath = "return (string)Vars[\"s2\"];";

            //var opt = ScriptOptions.Default.AddImports("System");

            vars.Nodes.Add(node1.ID, node1);
            vars.Nodes.Add(node2.ID, node2);
            vars.Nodes.Add(node3.ID, node3);
            vars.Nodes.Add(node4.ID, node4);
            vars.Nodes.Add("5", new EndNode { ID = "5" });

            vars.Flows.Add(new Flow { FromID = "1", ToID = "2" });
            vars.Flows.Add(new Flow { FromID = "2", ToID = "3" });
            vars.Flows.Add(new Flow { FromID = "3", ToID = "4" });
            vars.Flows.Add(new Flow { FromID = "4", ToID = "5" });

            await node1.Run(new GraphTrack(string.Empty, string.Empty, node1.ID), vars, CancellationToken.None);

            Assert.AreEqual(75, variables["a1"]);
        }

        [TestMethod]
        public void TestCompileAndRun()
        {
            //source:
            //https://stackoverflow.com/questions/32769630/how-to-compile-a-c-sharp-file-with-roslyn-programmatically

            #region step 1: code generation
            string sourceCode = @"
                using System; 
                using System.IO;
                using System.Net; 
                using System.Linq; 
                using System.Text; 
                using System.Text.RegularExpressions; 
                using System.Collections.Generic;

                namespace MySpace 
                {
                    public class MyClass
                    {
                        Dictionary<string, object> dic = new Dictionary<string, object>();

                        public MyClass()
                        {
                            dic[""a1""] = 4;
                        }
                        
                        public void Run() 
                        {
                            var node1 = new Func<object>(() => { return 3; })();
                            var node2 = new Func<object>(() => { return ""hello"" + "" world""; })();
                            
                            var msg = ""hello there"";
                            Console.WriteLine(msg);

                            int popo = 78 - 3;
                            dic[""a1""] = popo;
                        }
                    }
                }
            ";
            #endregion

            #region step 2: code analysis
            /*
            IEnumerable<string> DefaultNamespaces =
            [
                "System",
                "System.IO",
                "System.Net",
                "System.Linq",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.Collections.Generic"
            ];
            */

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));
            #endregion

            #region step 3: prepare compilation configuration
            string sdkPath = "C:\\Program Files\\dotnet\\packs\\Microsoft.NETCore.App.Ref\\9.0.10\\ref\\net9.0";
            var dlls = Directory.GetFiles(sdkPath, "*.dll");
            List<MetadataReference> refs = new List<MetadataReference>();
            foreach (var dll in dlls)
            {
                refs.Add(MetadataReference.CreateFromFile(dll));
            }
            refs.Add(MetadataReference.CreateFromFile(typeof(IGraphObj).Assembly.Location));
            var references = refs.ToArray();

            var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)
                .WithOptimizationLevel(OptimizationLevel.Release);
                //.WithUsings(DefaultNamespaces);

            CSharpCompilation compilation = CSharpCompilation.Create(
                "MyAssembly.dll",
                [syntaxTree],
                references,
                compileOptions
            );
            #endregion

            #region step 4: compile and run the assembly
            using(var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var result = compilation.Emit(dllStream, pdbStream); //actual compilation process runs here

                if (result.Success)
                {
                    dllStream.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(dllStream.ToArray());

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    var type = assembly.GetType("MySpace.MyClass");
                    if (type is null)
                        throw new NullReferenceException(nameof(type));

                    var instance = Activator.CreateInstance(type);
                    var methodInfo = type.GetMethod("Run");

                    if(methodInfo is null)
                        throw new NullReferenceException(nameof(methodInfo));

                    methodInfo.Invoke(instance, null);

                    stopwatch.Stop();
                    Console.WriteLine($"total execute time {stopwatch.Elapsed.TotalMilliseconds}ms");
                }
                else
                {
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        Console.WriteLine(diagnostic.ToString());
                    }

                    throw new Exception("failed to compile code");
                }
            }
            #endregion
        }

        [TestMethod]
        public void TestToCSharp()
        {
            var node1 = new ScriptNode { Script = new GraphProperty<string>("return 3 + 4;") , ID="123" };
            var node2 = new ScriptNode { Script = new GraphProperty<string> { BindPath = "return (string)Vars[\"b\"];" }, ID = "456" };
            var endNode = new EndNode { ID = "789" };

            var vars = new GraphVariable { 
                Flows = [ new Flow(node1.ID, node2.ID), new Flow(node2.ID, endNode.ID)]
            };

            vars["a"] = 58;
            vars["b"] = "return 3 + 4;";

            vars.Nodes[node1.ID] = node1;
            vars.Nodes[node2.ID] = node2;
            vars.Nodes[endNode.ID] = endNode;

            var scriptStr = node1.ToCSharp(vars);
            Assert.IsNotNull(scriptStr);

            var scriptStr2 = node2.ToCSharp(vars);
            Assert.IsNotNull(scriptStr2);
        }

        [TestMethod]
        public void TestXmlSerialization()
        {
            string rawXML = string.Empty;

            string expectedScript = "int k = 8; return k + 5;";
            var obj = new ScriptNode(expectedScript);

            var serializer = new XmlSerializer(typeof(ScriptNode));
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
                var actual = serializer.Deserialize(reader) as ScriptNode;
                Assert.IsNotNull(actual);

                Assert.AreEqual(expectedScript, actual.Script.Value);
            }
        }

        [TestMethod]
        public void TestXmlSerialization2()
        {
            string rawXML = string.Empty;

            string expectedScript = "int k = 8; return k + 5;";
            var obj = new ScriptNode(string.Empty);
            obj.Script.BindPath = expectedScript;

            var serializer = new XmlSerializer(typeof(ScriptNode));
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
                var actual = serializer.Deserialize(reader) as ScriptNode;
                Assert.IsNotNull(actual);

                Assert.AreEqual(expectedScript, actual.Script.BindPath);
            }
        }
    }
}