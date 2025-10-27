using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Trail;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.IO;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;

namespace GxFlow.WorkflowEngine.Script
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

        public static async Task<T> Eval<T>(string script, GraphTrack runInfo, GraphVariable vars, CancellationToken token)
        {
            var opt = ScriptOptions.Default
                .AddImports(s_standardNamespaces);

            var references = GetAssemblyReference();
            opt = opt.AddReferences(references);

            var ret = await CSharpScript.RunAsync<T>(
                script,
                opt,
                new GraphVariableWrapper(runInfo, vars),
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

        public static (byte[], byte[]) CompileToDll(string[] sourceCodes, string dllName = "MyAssembly.dll")
        {
            List<SyntaxTree> parsedSourceCode = new List<SyntaxTree>();
            foreach (var code in sourceCodes)
            {
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                    code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

                parsedSourceCode.Add(syntaxTree);
            }
           
            var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithUsings(s_standardNamespaces);

            var references = GetAssemblyReference();

            CSharpCompilation compilation = CSharpCompilation.Create(
                dllName,
                parsedSourceCode,
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

        public static string GenerateNamespace(string namespaceID, string content, string[]? additionalNamespaces = null)
        {
            var strBuilder = new StringBuilder();
            if (additionalNamespaces != null)
            {
                foreach (var ns in additionalNamespaces)
                {
                    strBuilder.AppendLine($"using {ns};");
                }
            }
            
            string code = @$"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading;
            using System.Threading.Tasks;
            {strBuilder.ToString()}

            namespace GxFlow.WorkflowEngine.Compiled_{namespaceID} {{
                {content}
            }}";

            return code;
        }

        public static (AssemblyLoadContext, object) CompileAndLoadInstance(string[] sourceCode, string instanceType, AssemblyLoadContext? appDomain = null)
        {
            var (dll, pdb) = CompileToDll(sourceCode);

            if (appDomain == null)
            {
                appDomain = new AssemblyLoadContext(Guid.NewGuid().ToString("N"), true);
            }

            using (var stream = new MemoryStream(dll))
            {
                var assembly = appDomain.LoadFromStream(stream);
                if (assembly is null)
                    throw new NullReferenceException("Cannot load assemly from source codes");

                var type = assembly.GetType(instanceType);
                if (type is null)
                    throw new NullReferenceException($"instance type {instanceType} not found in source code");

                var instance = Activator.CreateInstance(type);
                if (instance is null)
                    throw new NullReferenceException($"failed to instantiated object from type {instanceType}");

                return (appDomain, instance);
            }
        }
    }
}