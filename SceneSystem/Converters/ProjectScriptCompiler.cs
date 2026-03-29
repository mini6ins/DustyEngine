using DustyEngine.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SceneSystem.Converters;

public static class ProjectScriptCompiler
{
    public static bool CompileAllScripts(
        string projectRoot,
        string outputDllPath,
        out List<string> errors)
    {
        errors = [];

        var assetsPath = Path.Combine(projectRoot, "Assets");
        if (!Directory.Exists(assetsPath))
        {
            errors.Add($"Assets folder not found: {assetsPath}");
            return false;
        }

        var scriptFiles = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);
        if (scriptFiles.Length == 0)
        {
            return true;
        }

        var syntaxTrees = scriptFiles
            .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file))
            .ToList();

        var references = CollectReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: Path.GetFileNameWithoutExtension(outputDllPath),
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                platform: Platform.AnyCpu));

        var outputDir = Path.GetDirectoryName(outputDllPath)!;
        Directory.CreateDirectory(outputDir);

        var tempDllPath = Path.Combine(outputDir, $"UserScripts_{Guid.NewGuid():N}.tmp.dll");

        try
        {
            using (var dllStream = new FileStream(tempDllPath, FileMode.Create, FileAccess.Write))
            {
                var emitResult = compilation.Emit(dllStream);

                if (!emitResult.Success)
                {
                    errors.AddRange(
                        emitResult.Diagnostics
                            .Where(d => d.Severity == DiagnosticSeverity.Error)
                            .Select(d => d.ToString()));
                    return false;
                }
            }

            File.Move(tempDllPath, outputDllPath, overwrite: true);
            return true;
        }
        catch
        {
            if (File.Exists(tempDllPath))
                File.Delete(tempDllPath);
            throw;
        }
    }

    private static List<MetadataReference> CollectReferences()
    {
        var references = new List<MetadataReference>();
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (!string.IsNullOrWhiteSpace(tpa))
        {
            foreach (var path in tpa.Split(Path.PathSeparator))
                AddPath(path);
        }

        AddPath(typeof(Component).Assembly.Location);

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (asm.IsDynamic)
                    continue;

                if (string.IsNullOrWhiteSpace(asm.Location))
                    continue;

                AddPath(asm.Location);
            }
            catch
            {
                // ignored
            }
        }

        return references;

        void AddPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!File.Exists(path))
                return;

            if (added.Add(path))
                references.Add(MetadataReference.CreateFromFile(path));
        }
    }
}