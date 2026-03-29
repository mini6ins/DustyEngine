using System.Reflection;
using DustyEngine.Components;

namespace SceneSystem.Converters;

public static class ScriptAssembly
{
    private static readonly Dictionary<string, Type> ScriptTypesByName = new();
    private static readonly Dictionary<string, Type> ScriptTypesByFullName = new();
    
    private static ScriptLoadContext? _currentContext;
    private static Assembly? _scriptAssembly;

    public static event Action? OnAssemblyReloaded;
    
    public static IEnumerable<Type> GetLoadedTypes()
    {
        if (_scriptAssembly == null) return [];

        try { return _scriptAssembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; }
        catch { return []; }
    }
    
    public static void Load(string dllPath)
    {
        if (!File.Exists(dllPath))
            throw new FileNotFoundException($"Script DLL not found: {dllPath}");

        var shadowPath = Path.Combine(
            Path.GetDirectoryName(dllPath)!,
            $"UserScripts_shadow_{Guid.NewGuid():N}.dll");

        File.Copy(dllPath, shadowPath, overwrite: true);

        if (_currentContext != null)
        {
            _currentContext.Unload();
            _currentContext = null;
            _scriptAssembly = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        _currentContext = new ScriptLoadContext(shadowPath);
        _scriptAssembly = _currentContext.LoadFromAssemblyPath(shadowPath);

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
        
        OnAssemblyReloaded?.Invoke();
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