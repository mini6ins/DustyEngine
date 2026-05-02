using DustyEngine;
using DustyEngine.Components;
using SceneSystem.EngineObject.GameObject;

namespace SceneSystem.Scene;

public static class GameObjectHierarchyService
{
    public static event Action<MeshRenderer>? OnRendererAdded;
    public static event Action<MeshRenderer>? OnRendererRemoved;

    public static void NotifyRendererAdded(MeshRenderer renderer) => OnRendererAdded?.Invoke(renderer);
    public static void NotifyRendererRemoved(MeshRenderer renderer) => OnRendererRemoved?.Invoke(renderer);

    private static int Count(GameObject root)
    {
        var count = 0;
        Traverse(root, _ => count++);
        return count;
    }

    public static void Traverse(GameObject root, Action<GameObject> action)
    {
        action(root);

        foreach (var child in root.Children)
        {
            Traverse(child, action);
        }
    }

    public static void Add(DustyEngine.Scene.Scene scene, GameObject gameObject, GameObject? parent)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (gameObject.GetComponent<Transform>() == null)
        {
            gameObject.AddComponent(new Transform());
            Debug.Log($"[SceneManager] Added Transform to GameObject [{gameObject.Name}]", Debug.LogLevel.Info, true);
        }

        if (parent == null)
        {
            if (!scene.GameObjects.Contains(gameObject))
            {
                scene.GameObjects.Add(gameObject);
                Debug.Log($"[Scene: {scene.Name}] Added GameObject [{gameObject.Name}] to Scene",
                    Debug.LogLevel.Info, true);
            }
        }
        else
        {
            if (!parent.Children.Contains(gameObject))
            {
                parent.Children.Add(gameObject);
                gameObject.Parent = parent;
                Debug.Log(
                    $"[Scene: {scene.Name}] Added GameObject [{gameObject.Name}] under Parent [{parent.Name}]",
                    Debug.LogLevel.Info, true);
            }
        }

        var children = gameObject.Children.ToList();
        foreach (var child in children)
        {
            Add(scene, child, gameObject);
        }

        foreach (var component in gameObject.Components)
        {
            if (component is MeshRenderer meshRenderer)
            {
                OnRendererAdded?.Invoke(meshRenderer);
            }
        }
    }

    public static void Remove(DustyEngine.Scene.Scene scene, GameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var children = gameObject.Children.ToList();
        foreach (var child in children)
        {
            Remove(scene, child);
        }

        gameObject.InvokeMethodInComponents("OnDisable");

        foreach (var component in gameObject.Components)
        {
            if (component is MeshRenderer meshRenderer)
            {
                OnRendererRemoved?.Invoke(meshRenderer);
            }
        }

        var removed = false;
        if (scene.GameObjects.Remove(gameObject))
        {
            removed = true;
            Debug.Log($"[Scene: {scene.Name}] Removed GameObject [{gameObject.Name}] from Scene",
                Debug.LogLevel.Info, true);
        }
        else if (gameObject.Parent != null)
        {
            if (gameObject.Parent.Children.Remove(gameObject))
            {
                removed = true;
                Debug.Log(
                    $"[Scene: {scene.Name}] Removed GameObject [{gameObject.Name}] from Parent [{gameObject.Parent.Name}]",
                    Debug.LogLevel.Info, true);
            }
        }

        if (removed)
        {
            gameObject.Parent = null;
            gameObject.Destroy();
        }
        else
        {
            Debug.Log(
                $"[Scene: {scene.Name}] [WARNING] GameObject [{gameObject.Name}] not found in scene or parent!",
                Debug.LogLevel.Warning, false);
        }
    }

    public static int Count(IEnumerable<GameObject> roots) => roots.Sum(Count);
}