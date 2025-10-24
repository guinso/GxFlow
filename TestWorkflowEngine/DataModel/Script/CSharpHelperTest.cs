using GxFlow.WorkflowEngine.DataModel.Script;
using System.Drawing;
using System.Runtime.Loader;

namespace TestWorkflowEngine.DataModel.Script
{
    [TestClass]
    public class CSharpHelperTest
    {
        string _sourceCode = @"
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

        [TestMethod]
        public async Task TestToCode()
        {
            string strExpected = "koko";
            string actual = CSharpHelper.ToCode(strExpected);
            Assert.AreEqual($"\"{strExpected}\"", actual);

            int intExpected = 3;
            actual = CSharpHelper.ToCode(intExpected);
            Assert.AreEqual("3", actual);

            float floatExpected = 3.4f;
            actual = CSharpHelper.ToCode(floatExpected);
            Assert.AreEqual("3.4", actual);

            double doubleExpected = 5.41278;
            actual = CSharpHelper.ToCode(doubleExpected);
            Assert.IsTrue(actual.Contains("5.41278"));

            Point pointExpected = new Point(3, 5);
            string kk = CSharpHelper.ToCode(pointExpected);
            Point pointActual = await CSharpHelper.Eval<Point>(
                $"return {kk};", 
                new GxFlow.WorkflowEngine.DataModel.Core.GraphVariable(), 
                CancellationToken.None);
            Assert.AreEqual(pointExpected, pointActual);
        }

        [TestMethod]
        public void TestCompileToDll()
        {
            var (asmRaw, pdbRaw) = CSharpHelper.CompileToDll(_sourceCode);

            var appContext = new AssemblyLoadContext("exampleAsmContext", true);
            using(var stream = new  MemoryStream(asmRaw))
            {
                var asm = appContext.LoadFromStream(stream);

                Assert.IsNotNull(asm);

                var type = asm.GetType("MySpace.MyClass");
                Assert.IsNotNull(type);

                var instance = Activator.CreateInstance(type);
                var methodInfo = type.GetMethod("Run");

                Assert.IsNotNull(methodInfo);

                methodInfo.Invoke(instance, null);
            }

            appContext.Unload();
        }
    }
}