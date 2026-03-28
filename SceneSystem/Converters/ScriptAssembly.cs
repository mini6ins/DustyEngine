using System.Reflection;
using DustyEngine.Components;

namespace SceneSystem.Converters;

public static class ScriptAssemblyManager
{
    private static Assembly? _scriptAssembly;
    private static readonly Dictionary<string, Type> ScriptTypesByName = new();
    private static readonly Dictionary<string, Type> ScriptTypesByFullName = new();

    public static void Load(string dllPath)
    {
        if (!File.Exists(dllPath))
            throw new FileNotFoundException($"Script DLL not found: {dllPath}");

        _scriptAssembly = Assembly.LoadFrom(dllPath);

        ScriptTypesByName.Clear();
        ScriptTypesByFullName.Clear();

        foreach (var type in SafeGetTypes(_scriptAssembly))
        {
            if (!type.IsClass || type.IsAbstract)
                continue;

            if (!typeof(Component).IsAssignableFrom(type))
                continue;

            if (type.FullName != null)
                ScriptTypesByFullName[type.FullName] = type;

            ScriptTypesByName.TryAdd(type.Name, type);
        }
    }

    public static Type? ResolveScriptType(string scriptClass)
    {
        if (string.IsNullOrWhiteSpace(scriptClass))
            return null;

        if (ScriptTypesByFullName.TryGetValue(scriptClass, out var fullNameType))
            return fullNameType;

        return ScriptTypesByName.GetValueOrDefault(scriptClass);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
        catch
        {
            return [];
        }
    }
}