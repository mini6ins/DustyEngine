using System.Reflection;
using DustyEngine.Components;
using SceneSystem.Attributes;

namespace GraphicsEngineOpenGL.Editor.Panels.HierarchyPanel;

public static class ComponentTypeCache
{
    private static List<Type>? _types;

    public static IReadOnlyList<Type> GetAll()
    {
        if (_types != null) return _types;

        var baseType = typeof(Component);

        _types = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(SafeGetTypes)
            .Where(t =>
                t != null &&
                t.IsClass &&
                !t.IsAbstract &&
                !t.ContainsGenericParameters &&
                t != baseType &&
                baseType.IsAssignableFrom(t) &&
                !t.IsDefined(typeof(HideInAddComponentMenuAttribute), false)
            )
            .OrderBy(t => t!.Name)
            .ToList()!;

        return _types;
    }


    private static IEnumerable<Type?> SafeGetTypes(Assembly a)
    {
        try
        {
            return a.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types;
        }
    }
}
