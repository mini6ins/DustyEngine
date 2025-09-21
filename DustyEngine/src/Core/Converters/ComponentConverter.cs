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
    /// <summary>
    /// JSON-конвертер для компонентов. Поддерживает:
    /// - загрузку типа по имени (Type)
    /// - загрузку/компиляцию компонента из SourcePath (.cs или .dll)
    /// - запись SourcePath (относительно папки проекта)
    /// </summary>
    public class ComponentConverter : JsonConverter<Component>
    {
        private static readonly Dictionary<string, Type> ComponentTypes;
        private static readonly Dictionary<Type, string> ComponentSourcePaths = new();

        static ComponentConverter()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            ComponentTypes = assemblies
                .SelectMany(a => SafeGetTypes(a))
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Component)))
                .GroupBy(t => t.Name)
                .ToDictionary(g => g.Key, g => g.First()); // если имён-дубликатов несколько – берём первый
        }

        public override Component Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                string typeName = doc.RootElement.GetProperty("Type").GetString() ?? "";

                ComponentTypes.TryGetValue(typeName, out Type? componentType);

                // Если указан внешний путь к исходнику/сборке – пробуем загрузить/скомпилировать
                if (doc.RootElement.TryGetProperty("SourcePath", out JsonElement externalSourcePathEl))
                {
                    string raw = externalSourcePathEl.GetString() ?? string.Empty;
                    string sourcePath = ResolvePath(raw);
                    Debug.Log($"Source Path: {sourcePath}", Debug.LogLevel.Info, true);

                    Component? externalComponent = LoadOrCompileComponent(sourcePath);
                    if (externalComponent != null)
                    {
                        componentType = externalComponent.GetType();
                        ComponentSourcePaths[componentType] = sourcePath;
                    }
                }

                if (componentType == null)
                {
                    throw new JsonException($"Unknown component type '{typeName}'.");
                }

                var newOptions = new JsonSerializerOptions(options)
                {
                    Converters = { this }
                };

                return (Component)JsonSerializer.Deserialize(doc.RootElement.GetRawText(), componentType, newOptions)!;
            }
        }

        /// <summary>
        /// Загрузка компонента из .dll или компиляция из .cs, с учётом абсолютного пути.
        /// </summary>
        public static Component? LoadOrCompileComponent(string path)
        {
            string absPath = ResolvePath(path);
            string typeName = Path.GetFileNameWithoutExtension(absPath);
            Component? component = null;

            string ext = Path.GetExtension(absPath);
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
            {
                ComponentSourcePaths[component.GetType()] = absPath;
            }

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
            // Убедимся, что путь проекта абсолютный
            if (!string.IsNullOrEmpty(Program.ProjectFolderPath))
                Program.ProjectFolderPath = Path.GetFullPath(Program.ProjectFolderPath);

            string outputDirectory = Path.Combine(Program.ProjectFolderPath, "Settings", "Dlls");

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string outputDllPath = Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(sourcePath) + ".dll"
            );

            // Кэш: если .dll свежее исходника — используем её
            if (File.Exists(outputDllPath))
            {
                DateTime sourceLastModified = File.GetLastWriteTimeUtc(sourcePath);
                DateTime dllLastModified = File.GetLastWriteTimeUtc(outputDllPath);

                if (sourceLastModified <= dllLastModified)
                {
                    Debug.Log($"Using existing DLL: {outputDllPath}", Debug.LogLevel.Info, true);
                    return outputDllPath;
                }
            }

            string sourceCode = File.ReadAllText(sourcePath);
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
            };

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var location = SafeAssemblyLocation(asm);
                if (!string.IsNullOrWhiteSpace(location) && File.Exists(location))
                {
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
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using (var fileStream = new FileStream(outputDllPath, FileMode.Create, FileAccess.Write))
            {
                var result = compilation.Emit(fileStream);

                if (!result.Success)
                {
                    foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        Debug.Log($"Compilation error: {diagnostic.GetMessage()}", Debug.LogLevel.Error);
                    }

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

            // Сохраняем SourcePath (стараемся записать относительный к ProjectFolderPath — переносимее)
            if (ComponentSourcePaths.TryGetValue(value.GetType(), out string absSourcePath))
            {
                string projectRoot = string.IsNullOrEmpty(Program.ProjectFolderPath)
                    ? Directory.GetCurrentDirectory()
                    : Path.GetFullPath(Program.ProjectFolderPath);

                string toWrite = absSourcePath;
                try
                {
                    if (Path.IsPathRooted(absSourcePath))
                    {
                        toWrite = Path.GetRelativePath(projectRoot, absSourcePath);
                    }
                }
                catch
                {
                    // на всякий случай оставим исходный путь
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

                bool shouldSerialize = false;

                if (member is FieldInfo field)
                {
                    shouldSerialize = field.IsPublic || field.GetCustomAttribute<SerializeFieldAttribute>() != null;
                }
                else if (member is PropertyInfo prop)
                {
                    if (prop.CanRead && prop.GetMethod?.IsPublic == true)
                        shouldSerialize = true;

                    if (prop.GetCustomAttribute<SerializeFieldAttribute>() != null && prop.CanRead)
                        shouldSerialize = true;
                }

                if (!shouldSerialize)
                    continue;

                object? valueToWrite = null;

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

                if (valueToWrite != null)
                {
                    try
                    {
                        writer.WritePropertyName(member.Name);
                        JsonSerializer.Serialize(writer, valueToWrite, options);
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

            if (member is PropertyInfo prop)
            {
                var realProp = declaringType.GetProperty(prop.Name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (realProp?.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    return true;
            }

            if (member is FieldInfo field)
            {
                var realField = declaringType.GetField(field.Name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (realField?.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Превращает путь в абсолютный:
        /// - разворачивает '~' и переменные среды
        /// - относительные пути делает относительными к Program.ProjectFolderPath (если задан) или к текущему каталогу
        /// </summary>
        private static string ResolvePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return rawPath;

            // ~ → $HOME
            if (rawPath.StartsWith("~"))
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                rawPath = Path.Combine(home, rawPath.TrimStart('~').TrimStart('/', '\\'));
            }

            // Переменные окружения
            rawPath = Environment.ExpandEnvironmentVariables(rawPath);

            if (Path.IsPathRooted(rawPath))
                return Path.GetFullPath(rawPath);

            string baseDir = !string.IsNullOrEmpty(Program.ProjectFolderPath)
                ? Path.GetFullPath(Program.ProjectFolderPath)
                : Directory.GetCurrentDirectory();

            string combined = Path.Combine(baseDir, rawPath);
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
                return Enumerable.Empty<Type>();
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
