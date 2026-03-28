using DustyEngine.Components;

namespace test;

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class ScriptCompiler
{
    public static bool CompileProjectScripts(
        string projectRoot,
        string outputDllPath,
        out List<string> errors)
    {
        errors = new List<string>();

        string assetsPath = Path.Combine(projectRoot, "Assets");
        if (!Directory.Exists(assetsPath))
        {
            errors.Add($"Assets folder not found: {assetsPath}");
            return false;
        }

        string[] scriptFiles = Directory.GetFiles(
            assetsPath,
            "*.cs",
            SearchOption.AllDirectories);

        if (scriptFiles.Length == 0)
        {
            errors.Add("No .cs files found in Assets/");
            return false;
        }

        var syntaxTrees = new List<SyntaxTree>();

        foreach (string file in scriptFiles)
        {
            string code = File.ReadAllText(file);

            var tree = CSharpSyntaxTree.ParseText(
                code,
                path: file);

            syntaxTrees.Add(tree);
        }

        var references = CollectReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "UserScripts",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                platform: Platform.AnyCpu));

        string? outputDir = Path.GetDirectoryName(outputDllPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
            Directory.CreateDirectory(outputDir);

        using var dllStream = new FileStream(outputDllPath, FileMode.Create, FileAccess.Write);

        var emitResult = compilation.Emit(dllStream);

        if (!emitResult.Success)
        {
            foreach (var diagnostic in emitResult.Diagnostics
                         .Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                errors.Add(diagnostic.ToString());
            }

            return false;
        }

        return true;
    }

    private static List<MetadataReference> CollectReferences()
    {
        var references = new List<MetadataReference>();
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!File.Exists(path))
            {
                Console.WriteLine($"Reference not found: {path}");
                return;
            }

            if (added.Add(path))
                references.Add(MetadataReference.CreateFromFile(path));
        }

        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (!string.IsNullOrWhiteSpace(tpa))
        {
            foreach (var path in tpa.Split(Path.PathSeparator))
                AddPath(path);
        }

        AddPath(typeof(Component).Assembly.Location);

        AddPath("/home/maksym/Projects/DustyEngine/Math/bin/Debug/net10.0/Math.dll");
        AddPath("/home/maksym/Projects/DustyEngine/InputSystem/bin/Debug/net10.0/InputSystem.dll");

        return references;
    }
}



class Program
{
    static void Main(string[] args)
    {
        LoadDll();
        // Compile();
    }

    private static void LoadDll()
    {
        string dllPath = "/home/maksym/Projects/DustyEngine/test/test.dll";

        var assembly = Assembly.LoadFrom(dllPath);

        Console.WriteLine("Loaded: " + assembly.FullName);

        foreach (var type in assembly.GetTypes())
        {
            Console.WriteLine("==================================");
            Console.WriteLine("Type: " + type.FullName);

            Console.WriteLine("Namespace: " + type.Namespace);
            Console.WriteLine("BaseType: " + type.BaseType?.FullName);

            Console.WriteLine("IsClass: " + type.IsClass);
            Console.WriteLine("IsAbstract: " + type.IsAbstract);

            Console.WriteLine("Constructors:");
            foreach (var ctor in type.GetConstructors())
            {
                var parameters = string.Join(", ",
                    ctor.GetParameters().Select(p => p.ParameterType.Name));

                Console.WriteLine($"  ctor({parameters})");
            }

            Console.WriteLine("Fields:");
            foreach (var field in type.GetFields(
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.Instance))
            {
                Console.WriteLine($"  {field.FieldType.Name} {field.Name}");
            }

            Console.WriteLine("Properties:");
            foreach (var prop in type.GetProperties(
                         BindingFlags.Public |
                         BindingFlags.Instance))
            {
                Console.WriteLine($"  {prop.PropertyType.Name} {prop.Name}");
            }

            Console.WriteLine("Methods:");
            foreach (var method in type.GetMethods(
                         BindingFlags.Public |
                         BindingFlags.Instance |
                         BindingFlags.DeclaredOnly))
            {
                Console.WriteLine($"  {method.ReturnType.Name} {method.Name}()");
            }
        }
    }

    private static void Compile()
    {
        var ok = ScriptCompiler.CompileProjectScripts(
            projectRoot: "/home/maksym/Projects/DustyEngine/TestProject/",
            outputDllPath: "/home/maksym/Projects/DustyEngine/test/test.dll",
            out var errors);

        if (!ok)
        {
            foreach (var error in errors)
                Console.WriteLine(error);
        }
        else
        {
            Console.WriteLine("Scripts compiled successfully.");
        }
    }
}