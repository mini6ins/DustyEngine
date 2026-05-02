using SceneSystem.EngineObject.GameObject;

namespace SceneSystem.Scene;

public static class ComponentQueryService
{
    public static List<T> Collect<T>(IEnumerable<GameObject> roots)
    {
        var result = new List<T>();

        foreach (var root in roots)
        {
            result.AddRange(Collect<T>(root));
        }

        return result;
    }

    public static List<T> Collect<T>(GameObject root)
    {
        var result = new List<T>();

        GameObjectHierarchyService.Traverse(root, go =>
        {
            foreach (var c in go.Components)
                if (c is T t)
                    result.Add(t);
        });

        return result;
    }

    public static T? FindFirst<T>(GameObject root)
    {
        T? result = default;

        GameObjectHierarchyService.Traverse(root, go =>
        {
            if (result != null) return;

            foreach (var c in go.Components)
            {
                if (c is T comp)
                {
                    result = comp;
                    return;
                }
            }
        });

        return result;
    }
}