using DustyEngine.Components;
using SceneSystem.EngineObject.GameObject;

namespace DustyEngine.Scene;

public static class SceneManager
{
    private static readonly List<Scene> sceneList = [];
    private static Scene? currentScene;
    public static Action<MeshRenderer> AddRenderer = _ => { };
    public static Action<MeshRenderer> RemoveRenderer = _ => { };

    private static uint _nextGameObjectId = 1;
    private static uint _nextComponentId = 1;
    public static uint GenerateGameObjectId() => _nextGameObjectId++;
    public static uint GenerateComponentId() => _nextComponentId++;


    public static Scene? CurrentScene
    {
        get
        {
            if (currentScene == null && sceneList.Count > 0)
            {
                currentScene = sceneList[0];
                Debug.Log($"[SceneManager] Auto-set current scene to: {currentScene.Name}", Debug.LogLevel.FatalError, true);
            }

            return currentScene;
        }
        set => currentScene = value;
    }


    public static void AddGameObjectRecursively(GameObject gameObject, GameObject? parent)
    {
        if (CurrentScene == null)
        {
            Debug.Log("[SceneManager] No scenes available!", Debug.LogLevel.Error, false);
            return;
        }

        if (gameObject.GetComponent<Transform>() == null)
        {
            gameObject.AddComponent(new Transform());
            Debug.Log($"[SceneManager] Added Transform to GameObject [{gameObject.Name}]", Debug.LogLevel.Info, true);
        }

        if (parent == null)
        {
            if (!CurrentScene.GameObjects.Contains(gameObject))
            {
                CurrentScene.GameObjects.Add(gameObject);
                Debug.Log($"[Scene: {CurrentScene.Name}] Added GameObject [{gameObject.Name}] to Scene",
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
                    $"[Scene: {CurrentScene.Name}] Added GameObject [{gameObject.Name}] under Parent [{parent.Name}]",
                    Debug.LogLevel.Info, true);
            }
        }

        var children = gameObject.Children.ToList();
        foreach (var child in children)
        {
            AddGameObjectRecursively(child, gameObject);
        }

        gameObject.InvokeMethodInComponents("OnEnable");
    
        foreach (var component in gameObject.Components)
        {
            if (component is MeshRenderer meshRenderer)
            {
                AddRenderer?.Invoke(meshRenderer);
            }
        }
    
        gameObject.InvokeMethodInComponents("Start");
    }
    public static void RemoveGameObjectRecursively(GameObject gameObject)
    {
        if (CurrentScene == null)
        {
            Debug.Log("[SceneManager] No scenes available!", Debug.LogLevel.Error);
            return;
        }

        var children = gameObject.Children.ToList();
        foreach (var child in children)
        {
            RemoveGameObjectRecursively(child);
        }

        gameObject.InvokeMethodInComponents("OnDisable");


        foreach (var component in gameObject.Components)
        {
            if (component is MeshRenderer meshRenderer)
            {
                RemoveRenderer?.Invoke(meshRenderer);
            }
        }

        var removed = false;
        if (CurrentScene.GameObjects.Remove(gameObject))
        {
            removed = true;
            Debug.Log($"[Scene: {CurrentScene.Name}] Removed GameObject [{gameObject.Name}] from Scene",
                Debug.LogLevel.Info, true);
        }
        else if (gameObject.Parent != null)
        {
            if (gameObject.Parent.Children.Remove(gameObject))
            {
                removed = true;
                Debug.Log(
                    $"[Scene: {CurrentScene.Name}] Removed GameObject [{gameObject.Name}] from Parent [{gameObject.Parent.Name}]",
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
                $"[Scene: {CurrentScene.Name}] [WARNING] GameObject [{gameObject.Name}] not found in scene or parent!",
                Debug.LogLevel.Warning, false);
        }
    }

    public static List<Camera>? FindCameras()
    {
        if (CurrentScene == null)
        {
            Debug.Log("[SceneManager] No scene provided and no scenes available!", Debug.LogLevel.Warning, false);
            return null;
        }

        List<Camera> cameras = new List<Camera>();

        foreach (var obj in CurrentScene.GameObjects)
        {
            var camera = FindCameraRecursive(obj);
            if (camera != null)
                cameras.Add(camera);
        }

        return cameras;
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
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(renderers);

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
        if (CurrentScene == null)
        {
            Debug.Log("[SceneManager] No scenes available!", Debug.LogLevel.Warning, false);
            return 0;
        }

        int count = 0;
        foreach (var gameObject in CurrentScene.GameObjects)
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

    public static void LoadScene(string sceneName)
    {
        ArgumentNullException.ThrowIfNull(sceneName);
        CurrentScene = FindScene(sceneName);
        Debug.Log($"[SceneManager] Current scene set to: {CurrentScene.Name}", Debug.LogLevel.Info, true);
    }

    public static void LoadScene(uint  index)
    {
        CurrentScene = FindScene(index);
        Debug.Log($"[SceneManager] Current scene set to: {CurrentScene.Name}", Debug.LogLevel.Info, true);
    }

    public static string? GetCurrentScene() => CurrentScene.Name;
    public static string? GetCurrentScenePath() => CurrentScene.Path;

    public static void AddScene(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (!sceneList.Contains(scene))
        {
            sceneList.Add(scene);
            Debug.Log($"[SceneManager] Added scene: {scene.Name}", Debug.LogLevel.Info, true);


            if (sceneList.Count == 1 && currentScene == null)
            {
                currentScene = scene;
                Debug.Log($"[SceneManager] Auto-set first scene as current: {scene.Name}", Debug.LogLevel.Info, true);
            }
        }
    }

    public static void RemoveScene(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (sceneList.Remove(scene))
        {
            if (currentScene == scene)
            {
                currentScene = sceneList.Count > 0 ? sceneList[0] : null;
                if (currentScene != null)
                {
                    Debug.Log($"[SceneManager] Current scene switched to: {currentScene.Name}", Debug.LogLevel.Info,
                        true);
                }
            }

            Debug.Log($"[SceneManager] Removed scene: {scene.Name}", Debug.LogLevel.Info, true);
        }
    }

    public static Scene? FindScene(string name)
    {
        return sceneList.FirstOrDefault(s => s.Name == name);
    }
    public static Scene? FindScene(uint index)
    {
        return sceneList[(int)index];
    }

    public static IReadOnlyList<Scene> GetAllScenes() => sceneList.AsReadOnly();

    public static void InvokeRecursive(GameObject gameObject, string methodName)
    {
        if (gameObject.ActiveInHierarchy)
        {
            gameObject.InvokeMethodInComponents(methodName);
        }

        foreach (var child in gameObject.Children)
        {
            InvokeRecursive(child, methodName);
        }
    }
}
