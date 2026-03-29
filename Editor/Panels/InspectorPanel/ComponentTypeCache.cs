using System.Reflection;
using DustyEngine.Components;
using SceneSystem.Attributes;
using SceneSystem.Converters;

namespace Editor.Panels.InspectorPanel;

public static class ComponentTypeCache
{
    private static List<Type>? _types;

    private static void Invalidate() => _types = null;

    static ComponentTypeCache()
    {
        ScriptAssembly.OnAssemblyReloaded += Invalidate;
    }
    
    public static IReadOnlyList<Type> GetAll()
    {
        if (_types != null) return _types;

        var baseType = typeof(Component);

        var builtIn = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(SafeGetTypes);

        var userTypes = ScriptAssembly.GetLoadedTypes();

        _types = builtIn
            .Concat(userTypes)
            .Where(t =>
                t is { IsClass: true } &&
                !t.IsAbstract &&
                !t.ContainsGenericParameters &&
                t != baseType &&
                baseType.IsAssignableFrom(t) &&
                !t.IsDefined(typeof(HideInAddComponentMenuAttribute), false)
            )
            .DistinctBy(t => t.FullName)
            .OrderBy(t => t.Name)
            .ToList()!;

        return _types;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; }
        catch { return []; }
    }
}