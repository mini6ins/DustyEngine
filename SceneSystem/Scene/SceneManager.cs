using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Core;

namespace SceneSystem.Scene;

public static class SceneManager
{
    public static event Action<DustyEngine.Scene.Scene>? OnSceneChanged;
    
    
    public static Action<MeshRenderer> AddRenderer = _ => { };
    public static Action<MeshRenderer> RemoveRenderer = _ => { };


    private static readonly List<DustyEngine.Scene.Scene> SceneList = [];
    private static DustyEngine.Scene.Scene? _currentScene;
    public static string? ProjectPath;
    
    public static DustyEngine.Scene.Scene? CurrentScene
    {
        get
        {
            if (_currentScene != null || SceneList.Count <= 0) return _currentScene;

            _currentScene = SceneList[0];
            Debug.Log($"[SceneManager] Auto-set current scene to: {_currentScene.Name}", Debug.LogLevel.FatalError,
                true);

            return _currentScene;
        }
        private set => _currentScene = value;
    }
    public static DustyEngine.Scene.Scene? FindScene(string name) => SceneList.FirstOrDefault(s => s.Name == name);
    public static DustyEngine.Scene.Scene? FindScene(uint index) => index >= SceneList.Count ? null : SceneList[(int)index];
    public static IReadOnlyList<DustyEngine.Scene.Scene> GetAllScenes() => SceneList.AsReadOnly();
    public static bool OpenScene(string scenePath)
    {
        ProjectScriptService.Reload(ProjectPath, throwOnError: true);
        return LoadSceneByPath(scenePath);
    }
    public static DustyEngine.Scene.Scene? CloneViaSerialization(DustyEngine.Scene.Scene original)
    {
        var json = SceneSerializer.SerializeScene(original);
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("Failed to serialize scene for snapshot", Debug.LogLevel.Error);
            return null;
        }

        var clonedScene = SceneSerializer.DeserializeScene(json);
        if (clonedScene != null)
        {
            clonedScene.Path = original.Path;
        }

        return clonedScene;
    }
    public static void RestoreScene(DustyEngine.Scene.Scene snapshot)
    {
        if (CurrentScene == null)
        {
            Debug.Log("No current scene", Debug.LogLevel.Error);
            return;
        }
        
        var currentObjects = CurrentScene.GameObjects.ToList();
        foreach (var obj in currentObjects)
        {
            GameObjectHierarchyService.Remove(CurrentScene, obj);
        }

        var newObjects = snapshot.GameObjects.ToList();

        CurrentScene.GameObjects.Clear();

        foreach (var obj in newObjects)
            GameObjectHierarchyService.Add(CurrentScene, obj, null);
        
        OnSceneChanged?.Invoke(CurrentScene);
    }
    public static bool LoadSceneByPath(string? scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.Log("Scene path is null or empty", Debug.LogLevel.Error);
            return false;
        }

        var absolutePath = PathUtility.GetAbsolutePath(scenePath);

        if (!File.Exists(absolutePath))
        {
            Debug.Log($"Scene file not found: {absolutePath}", Debug.LogLevel.Error, true);

            if (string.IsNullOrWhiteSpace(ProjectPath))
                return false;

            var fileName = Path.GetFileName(scenePath);
            var assetsFolder = Path.Combine(ProjectPath, "Assets");

            Debug.Log($"Searching for scene file: {fileName} in Assets folder...", Debug.LogLevel.Info, true);

            var foundPath = SceneLoader.FindSceneInAssets(assetsFolder, fileName);
            if (foundPath == null)
            {
                Debug.Log($"Scene file '{fileName}' not found anywhere in Assets", Debug.LogLevel.Error, true);
                return false;
            }

            Debug.Log($"Found scene at new location: {foundPath}", Debug.LogLevel.Info, true);

            scenePath = PathUtility.GetRelativePath(foundPath);
        }

        var loadedScene = SceneLoader.LoadFromFile(scenePath);
        if (loadedScene == null)
        {
            Debug.Log("Scene deserialize error", Debug.LogLevel.Error);
            return false;
        }

        if (FindScene(loadedScene.Name) == null)
        {
            SceneList.Add(loadedScene);
            Debug.Log($"[SceneManager] Added scene: {loadedScene.Name}", Debug.LogLevel.Info, true);
        }

        CurrentScene = loadedScene;
        OnSceneChanged?.Invoke(CurrentScene);

        Debug.Log(
            $"[SceneManager] Current scene set to: {CurrentScene.Name} (Path: {CurrentScene.Path})",
            Debug.LogLevel.Info,
            true
        );

        return true;
    }
}