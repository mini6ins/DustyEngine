using DustyEngine.Components;

namespace DustyEngine.Scene;

public static class SceneManager
{
    private static List<Scene> sceneList = [];
    private static Scene currentScene;
    
      public static void AddGameObjectRecursively(GameObject gameObject, GameObject? parent)
    {
        if (parent == null)
        {
            if (!currentScene.GameObjects.Contains(gameObject))
            {
                currentScene.GameObjects.Add(gameObject);
                Debug.Log($"[Scene: {currentScene.Name}] Added GameObject [{gameObject.Name}] to Scene", Debug.LogLevel.Info, true);
            }
        }
        else
        {
            parent.Children.Add(gameObject);
            gameObject.Parent = parent;
            Debug.Log($"[Scene: {currentScene.Name}] Added GameObject [{gameObject.Name}] under Parent [{parent.Name}]", Debug.LogLevel.Info, true);
        }

        gameObject.InvokeMethodInComponents("OnEnable");
        gameObject.InvokeMethodInComponents("Start");

        foreach (var child in gameObject.Children)
        {
            AddGameObjectRecursively(child, gameObject);
        }
    }

    public static void RemoveGameObjectRecursively(GameObject gameObject)
    {
        foreach (var child in gameObject.Children)
        {
            RemoveGameObjectRecursively(child);
        }

        if (currentScene.GameObjects.Remove(gameObject))
        {
            gameObject.Destroy();
            Debug.Log($"[Scene: {currentScene.Name}] Removed GameObject [{gameObject.Name}] from Scene", Debug.LogLevel.Info, true);
        }
        else if (gameObject.Parent != null)
        {
            gameObject.Parent.Children.Remove(gameObject);
            Debug.Log($"[Scene: {currentScene.Name}] Removed GameObject [{gameObject.Name}] from Parent [{gameObject.Parent.Name}]", Debug.LogLevel.Info, true);
        }
        else
        {
            Debug.Log($"[Scene: {currentScene.Name}] [WARNING] GameObject [{gameObject.Name}] not found in scene or parent!", Debug.LogLevel.Warning, false);
        }
    }

    public static Camera? FindCamera(Scene scene)
    {
        foreach (var obj in scene.GameObjects)
        {
            var camera = FindCameraRecursive(obj);
            if (camera != null)
                return camera;
        }
        return null;
    }

    private static Camera? FindCameraRecursive(GameObject obj)
    {
        foreach (var component in obj.Components)
        {
            if (component is Camera camera)
                return camera;
        }

        foreach (var child in obj.Children)
        {
            var result = FindCameraRecursive(child);
            if (result != null)
                return result;
        }

        return null;
    }
    
    public static void CollectMeshRenderers(GameObject obj, List<MeshRenderer> renderers)
    {
        foreach (var component in obj.Components)
        {
            if (component is MeshRenderer meshRenderer)
            {
                renderers.Add(meshRenderer);
            }
        }

        foreach (var child in obj.Children)
        {
            CollectMeshRenderers(child, renderers);
        }
    }
    
    public static int GetTotalObjectsCount()
    {
        int count = 0;

        foreach (var gameObject in currentScene.GameObjects)
        {
            count += CountChildrenRecursively(gameObject);
        }

        return count;
    }

    private static int CountChildrenRecursively(GameObject gameObject)
    {
        int count = 1;

        foreach (var child in gameObject.Children)
        {
            count += CountChildrenRecursively(child);
        }

        return count;
    }
}