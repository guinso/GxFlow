using GxFlow.WorkflowEngine.DataModel.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text.Json;

namespace GxFlow.WorkflowEngine.DataModel.Script
{
    public class CSharpHelper
    {
        private static List<MetadataReference> s_asmRef = new List<MetadataReference>();
        private static readonly List<string> s_standardNamespaces = [
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Text",
            "System.Threading.Tasks"];

        public static async Task<T> Eval<T>(string script, GraphVariable vars, CancellationToken token)
        {
            var opt = ScriptOptions.Default
                .AddImports(s_standardNamespaces);

            var references = GetAssemblyReference();
            opt = opt.AddReferences(references);

            var ret = await CSharpScript.RunAsync<T>(
                script,
                opt,
                new GraphVariableWrapper { Vars = vars },
                typeof(GraphVariableWrapper),
                token);

            return ret.ReturnValue;
        }

        public static string ToCode(object obj)
        {
            if(obj is null)
                throw new NullReferenceException(nameof(obj));

            var type = obj.GetType();

            if (type == typeof(string))
                return $"\"{obj}\"";
            else if (type == typeof(int))
                return ((int)obj).ToString();
            else if (type == typeof(float))
                return ((float)obj).ToString();
            else if (type == typeof(double))
                return ((double)obj).ToString();
            else if(type == typeof(bool))
                return ((bool)obj).ToString();
            else
            {
                string jsonTxt = JsonSerializer.Serialize(obj).Replace("\"", "\\\"");

                return $"{typeof(JsonSerializer).FullName}.Deserialize<{type.FullName}>(\"{jsonTxt}\")";
            }
        }

        public static List<MetadataReference> GetAssemblyReference()
        {
            if (s_asmRef.Count() == 0)
            {
                s_asmRef = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
                    .Select(x => MetadataReference.CreateFromFile(x.Location) as MetadataReference)
                    .ToList();
            }

            return s_asmRef;
        }

        public static (byte[], byte[]) CompileToDll(string sourceCode, string dllName = "MyAssembly.dll")
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                sourceCode, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

            var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithUsings(s_standardNamespaces);

            var references = GetAssemblyReference();

            CSharpCompilation compilation = CSharpCompilation.Create(
                dllName,
                [syntaxTree],
                references,
                compileOptions
            );

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var result = compilation.Emit(dllStream, pdbStream); //actual compilation process runs here

                if (result.Success)
                {
                    dllStream.Seek(0, SeekOrigin.Begin);
                    byte[] dllRaw = dllStream.ToArray();

                    pdbStream.Seek(0, SeekOrigin.Begin);
                    byte[] pdbRaw = pdbStream.ToArray();

                    return (dllRaw, pdbRaw);
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
        }
    }
}
