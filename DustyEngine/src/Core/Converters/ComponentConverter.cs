using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DustyEngine.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SceneSystem.Attributes;

namespace DustyEngine.Core.Converters
{
    public class ComponentConverter : JsonConverter<Component>
    {
        private static readonly Dictionary<string, Type> ComponentTypes;
        private static readonly Dictionary<Type, string> ComponentSourcePaths = new();

        static ComponentConverter()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            ComponentTypes = assemblies
                .SelectMany(SafeGetTypes)
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Component)))
                .GroupBy(t => t.Name)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public override Component Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);

            var typeName = doc.RootElement.GetProperty("Type").GetString() ?? "";

            ComponentTypes.TryGetValue(typeName, out var componentType);

            if (doc.RootElement.TryGetProperty("SourcePath", out var externalSourcePathEl))
            {
                var raw = externalSourcePathEl.GetString() ?? string.Empty;
                var sourcePath = ResolvePath(raw);
                Debug.Log($"Source Path: {sourcePath}", Debug.LogLevel.Info, true);

                var externalComponent = LoadOrCompileComponent(sourcePath);
                if (externalComponent != null)
                {
                    componentType = externalComponent.GetType();
                    ComponentSourcePaths[componentType] = sourcePath;
                }
            }

            if (componentType == null)
                throw new JsonException($"Unknown component type '{typeName}'.");

            var newOptions = new JsonSerializerOptions(options)
            {
                Converters = { this }
            };

            var instance = (Component)JsonSerializer.Deserialize(
                doc.RootElement.GetRawText(),
                componentType,
                newOptions
            )!;

            ResolveObjPathInComponent(instance);

            return instance;
        }

        private static Component? LoadOrCompileComponent(string path)
        {
            var absPath = ResolvePath(path);
            var typeName = Path.GetFileNameWithoutExtension(absPath);
            Component? component = null;

            var ext = Path.GetExtension(absPath);
            if (ext.Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Loading component from DLL: {absPath}", Debug.LogLevel.Info, true);
                component = LoadComponentFromDll(absPath, typeName);
            }
            else if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Compiling component from source: {absPath}", Debug.LogLevel.Info, true);
                string dllPath = CompileSourceToDll(absPath);
                component = LoadComponentFromDll(dllPath, typeName);
            }
            else
            {
                Debug.Log($"Unsupported file type: {absPath}", Debug.LogLevel.Error, true);
            }

            if (component != null)
                ComponentSourcePaths[component.GetType()] = absPath;

            return component;
        }

        private static Component? LoadComponentFromDll(string dllPath, string typeName)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);

                var type = SafeGetTypes(assembly).FirstOrDefault(t => t.Name == typeName);

                if (type == null)
                {
                    Debug.Log($"Type '{typeName}' not found in '{dllPath}'", Debug.LogLevel.Error);
                    return null;
                }

                if (!typeof(Component).IsAssignableFrom(type))
                {
                    Debug.Log($"Type '{typeName}' does not inherit from Component", Debug.LogLevel.Error);
                    return null;
                }

                Debug.Log($"Successfully loaded component type: {type.FullName}", Debug.LogLevel.Info, true);

                return (Component)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                Debug.Log($"Error loading component from DLL: {ex.Message}", Debug.LogLevel.Error, true);
                throw;
            }
        }

        private static string CompileSourceToDll(string sourcePath)
        {
            if (!string.IsNullOrEmpty(DustyEngine.ProjectFolderPath))
                DustyEngine.ProjectFolderPath = Path.GetFullPath(DustyEngine.ProjectFolderPath);

            var outputDirectory = Path.Combine(DustyEngine.ProjectFolderPath, "Settings", "Dlls");

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            var outputDllPath = Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(sourcePath) + ".dll"
            );

            if (File.Exists(outputDllPath))
            {
                var sourceLastModified = File.GetLastWriteTimeUtc(sourcePath);
                var dllLastModified = File.GetLastWriteTimeUtc(outputDllPath);

                if (sourceLastModified <= dllLastModified)
                {
                    Debug.Log($"Using existing DLL: {outputDllPath}", Debug.LogLevel.Info, true);
                    return outputDllPath;
                }
            }

            var sourceCode = File.ReadAllText(sourcePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();

            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name.ToString())
                .Distinct()
                .ToList();

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location),
                MetadataReference.CreateFromFile(Assembly.Load("Microsoft.CSharp").Location),
                MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location), // DustyEngine.dll
                MetadataReference.CreateFromFile(typeof(GraphicsEngineOpenGL.Input.Input).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Utils.KeyCode).Assembly.Location), // если KeyCode там

            };

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var location = SafeAssemblyLocation(asm);
                if (string.IsNullOrWhiteSpace(location) || !File.Exists(location)) continue;

                try
                {
                    references.Add(MetadataReference.CreateFromFile(location));
                }
                catch (Exception ex)
                {
                    Debug.Log($"[Warning] Could not add reference for {location}: {ex.Message}",
                        Debug.LogLevel.Warning);
                }
            }

            foreach (var ns in usingDirectives)
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => SafeGetTypes(a).Any(t => t.Namespace == ns));

                var loc = SafeAssemblyLocation(asm);
                if (asm != null && !string.IsNullOrWhiteSpace(loc) && File.Exists(loc) &&
                    !references.Any(r => string.Equals(r.Display, loc, StringComparison.OrdinalIgnoreCase)))
                {
                    references.Add(MetadataReference.CreateFromFile(loc));
                }
            }

            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputDllPath),
                [syntaxTree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using (var fileStream = new FileStream(outputDllPath, FileMode.Create, FileAccess.Write))
            {
                var result = compilation.Emit(fileStream);

                if (!result.Success)
                {
                    foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                        Debug.Log($"Compilation error: {diagnostic.GetMessage()}", Debug.LogLevel.Error);

                    throw new Exception("Roslyn compilation failed.");
                }
            }

            Debug.Log($"Compiled new DLL at: {outputDllPath}", Debug.LogLevel.Info, true);
            return outputDllPath;
        }

        public override void Write(Utf8JsonWriter writer, Component value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Type", value.GetType().Name);

            if (ComponentSourcePaths.TryGetValue(value.GetType(), out string absSourcePath))
            {
                var projectRoot = string.IsNullOrEmpty(DustyEngine.ProjectFolderPath)
                    ? Directory.GetCurrentDirectory()
                    : Path.GetFullPath(DustyEngine.ProjectFolderPath);

                var toWrite = absSourcePath;
                try
                {
                    if (Path.IsPathRooted(absSourcePath))
                        toWrite = Path.GetRelativePath(projectRoot, absSourcePath);
                }
                catch
                {
                    // ignored
                }

                writer.WriteString("SourcePath", toWrite);
            }

            var type = value.GetType();
            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var member in members)
            {
                if (member.Name.Contains("k__BackingField"))
                    continue;

                if (HasJsonIgnore(member, type))
                    continue;

                var shouldSerialize = false;

                switch (member)
                {
                    case FieldInfo field:
                        shouldSerialize = field.IsPublic || field.GetCustomAttribute<SerializeFieldAttribute>() != null;
                        break;
                    case PropertyInfo prop:
                    {
                        if (prop.CanRead && prop.GetMethod?.IsPublic == true)
                            shouldSerialize = true;

                        if (prop.GetCustomAttribute<SerializeFieldAttribute>() != null && prop.CanRead)
                            shouldSerialize = true;
                        break;
                    }
                }

                if (!shouldSerialize)
                    continue;

                object? valueToWrite;

                try
                {
                    valueToWrite = member switch
                    {
                        FieldInfo f => f.GetValue(value),
                        PropertyInfo p => p.GetValue(value),
                        _ => null
                    };
                }
                catch (Exception ex)
                {
                    Debug.Log($"[Warning] Skipping '{member.Name}': {ex.Message}", Debug.LogLevel.Warning);
                    continue;
                }

                if (valueToWrite == null) continue;
                {
                    try
                    {
                        writer.WritePropertyName(member.Name);

                        var toSerialize = valueToWrite;

                        if (valueToWrite is string s && ShouldRelativizeObjPath(member, s))
                            toSerialize = MakeRelativeToProjectRoot(s);

                        JsonSerializer.Serialize(writer, toSerialize, options);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[Warning] Failed to serialize '{member.Name}': {ex.Message}",
                            Debug.LogLevel.Warning);
                    }
                }
            }

            writer.WriteEndObject();
        }

        private static bool HasJsonIgnore(MemberInfo member, Type declaringType)
        {
            if (member.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                return true;

            switch (member)
            {
                case PropertyInfo prop:
                {
                    var realProp = declaringType.GetProperty(prop.Name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (realProp?.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        return true;
                    break;
                }
                case FieldInfo field:
                {
                    var realField = declaringType.GetField(field.Name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (realField?.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        return true;
                    break;
                }
            }

            return false;
        }


        private static void ResolveObjPathInComponent(Component component)
        {
            var type = component.GetType();

            var prop = type.GetProperty("Path", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(string) && prop.CanRead && prop.CanWrite)
            {
                var s = (string?)prop.GetValue(component);
                if (!string.IsNullOrWhiteSpace(s) && s.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    prop.SetValue(component, ResolvePath(s));
            }

            var field = type.GetField("Path", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(string)) return;
            {
                var s = (string?)field.GetValue(component);
                if (!string.IsNullOrWhiteSpace(s) && s.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    field.SetValue(component, ResolvePath(s));
            }
        }

        private static bool ShouldRelativizeObjPath(MemberInfo member, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            if (!string.Equals(member.Name, "Path", StringComparison.OrdinalIgnoreCase))
                return false;

            return value.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) && Path.IsPathRooted(value);
        }

        private static string MakeRelativeToProjectRoot(string absPathOrRaw)
        {
            var absPath = ResolvePath(absPathOrRaw);

            var projectRoot = string.IsNullOrEmpty(DustyEngine.ProjectFolderPath)
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(DustyEngine.ProjectFolderPath);

            try
            {
                return Path.GetRelativePath(projectRoot, absPath);
            }
            catch
            {
                return absPathOrRaw;
            }
        }


        private static string ResolvePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return rawPath;

            if (rawPath.StartsWith("~"))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                rawPath = Path.Combine(home, rawPath.TrimStart('~').TrimStart('/', '\\'));
            }

            rawPath = Environment.ExpandEnvironmentVariables(rawPath);

            if (Path.IsPathRooted(rawPath))
                return Path.GetFullPath(rawPath);

            var baseDir = !string.IsNullOrEmpty(DustyEngine.ProjectFolderPath)
                ? Path.GetFullPath(DustyEngine.ProjectFolderPath)
                : Directory.GetCurrentDirectory();

            var combined = Path.Combine(baseDir, rawPath);
            return Path.GetFullPath(combined);
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch
            {
                return [];
            }
        }

        private static string SafeAssemblyLocation(Assembly? asm)
        {
            try
            {
                return asm?.Location ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
