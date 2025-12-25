using DustyEngine.Components;
using DustyEngine.Scene;
using Editor;
using Editor.Panels.ConsolePanel;
using Editor.Panels.HierarchyPanel;
using Editor.Panels.ProjectFilePanel;
using Editor.Panels.ProjectSetiingPanel;
using Editor.Panels.ViewPortPanel;
using GraphicsEngine;
using WindowEngine;

namespace DustyEngine;

public sealed class DustyEngine : IDisposable
{
    public static string ProjectFolderPath { get; set; } = null!;
    private static ProjectSettings _settings = new();

    private static Window _window = null!;

    private static readonly Action<MeshRenderer> AddRenderer = meshRenderer =>
        _window.Renderer?.AddRenderer(meshRenderer);

    private static readonly Action<MeshRenderer> RemoveRenderer =
        meshRenderer => _window.Renderer?.RemoveRendererByComponent(meshRenderer);


    public static void StartEngine(string path, RenderMode renderMode)
    {
        Debug.ClearLogs();

        if (LoadProject(path)) return;

        SetupDebug(renderMode);

        if (!LoadScene(_settings.PathToScenes.FirstOrDefault())) return;

        if (renderMode == RenderMode.Standalone)
            StartLifeCycle();

        CreateWindow(renderMode);
    }

    private static void CreateWindow(RenderMode renderMode)
    {
        _window = new Window();

        SceneManager.AddRenderer += AddRenderer;
        SceneManager.RemoveRenderer += RemoveRenderer;

        _window.RunMainLoop(ExecuteLifeCycle,
            _settings.ScreenSize.ToOpenTK(), _settings.ProjectName, _settings.PathToVertShader,
            _settings.PathToFragShader, _settings.Vsync, renderMode, ProjectFolderPath);
    }

    private static bool LoadProject(string path)
    {
        if (path.Length == 0)
        {
            Debug.Log("No project path provided", Debug.LogLevel.FatalError, true);
            return true;
        }

        ProjectFolderPath = path;
        _settings = ProjectSettings.DeserializeProjectSettings(ProjectFolderPath);
        ProjectSetiingPanel.ScenePaths = _settings.PathToScenes;

        return false;
    }

    private static void SaveProject()
    {
        SceneSerializer.SaveScene(SceneManager.CurrentScene, SceneManager.GetCurrentScenePath());
        SaveProjectSettings();
    }

    private static void SaveProjectSettings()
    {
        _settings.PathToScenes = ProjectSetiingPanel.ScenePaths;
        ProjectSettings.SerializeProjectSettings(_settings, ProjectFolderPath);
    }

    private static void SetupDebug(RenderMode renderMode)
    {
        Debug.EnableDebugMode(_settings.Debug);
        Debug.SetLogLevel(_settings.LogLevel);
        Debug.EnableConsoleLogging(_settings.LogToConsole);
        Debug.EnableFileLogging(_settings.LogToFile);

        var onDebugEnabled = Debug.EnableDebugMode;

        if (renderMode == RenderMode.EditorStop)
        {
            ConsolePanel.InitializeConsoleInterceptor(onDebugEnabled, _settings.Debug);
            RendererUI.OnProjectSave += SaveProject;
            ProjectSetiingPanel.OnSaveProjectSettings += SaveProjectSettings;
            ProjectFilePanel.OnSceneOpened += OpenScene;
            ViewportPanel.OnPlayModeChanged += ChangePlayMode;
        }

        Debug.Log("Project settings loaded");

        Debug.Log($"Initial currentLogLevel:  {Debug.GetLogLevel()}", Debug.LogLevel.Info, true);
        Debug.Log("Test INFO", Debug.LogLevel.Info, true);
        Debug.Log("Test WARNING", Debug.LogLevel.Warning, true);
        Debug.Log("Test ERROR", Debug.LogLevel.Error, true);
        Debug.Log("Test FATAL", Debug.LogLevel.FatalError, true);
    }

    private static Scene.Scene? _sceneSnapshot;

    private static void ChangePlayMode(bool state)
    {
        if (state)
        {
            _sceneSnapshot = CloneScene(SceneManager.CurrentScene!);
            _window.ChangePlayMode(RenderMode.EditorRun);

            if (_window.Renderer._sceneCameras != null && _window.Renderer._sceneCameras.Count > 0)
            {
                _window.Renderer.ActiveCamera = _window.Renderer._sceneCameras.First();
                Debug.Log("Switched to scene camera", Debug.LogLevel.Info, true);
            }
            else
            {
                Debug.Log("No scene cameras found!", Debug.LogLevel.Warning, true);
            }

            StartLifeCycle();
        }
        else
        {
            _window.ChangePlayMode(RenderMode.EditorStop);

            if (_sceneSnapshot != null)
            {
                RestoreScene(_sceneSnapshot);
                _sceneSnapshot = null;
            }

            if (_window.Renderer.EditorCamera != null)
            {
                _window.Renderer.ActiveCamera = _window.Renderer.EditorCamera;
                Debug.Log("Switched to editor camera", Debug.LogLevel.Info, true);
            }

            _window.LoadScene?.Invoke();
            HierarchyPanel.OnChangeScene?.Invoke();
        }
    }

    private static Scene.Scene? CloneScene(Scene.Scene original)
    {
        var json = SceneSerializer.SerializeSceneToJson(original);
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("Failed to serialize scene for snapshot", Debug.LogLevel.Error);
            return null;
        }

        var clonedScene = SceneSerializer.DeserializeSceneFromJson(json);
        if (clonedScene != null)
        {
            clonedScene.Path = original.Path;
        }

        return clonedScene;
    }

    private static void RestoreScene(Scene.Scene snapshot)
    {
        var currentObjects = SceneManager.CurrentScene!.GameObjects.ToList();
        foreach (var obj in currentObjects)
        {
            SceneManager.RemoveGameObjectRecursively(obj);
        }

        SceneManager.CurrentScene!.GameObjects.Clear();

        foreach (var obj in snapshot.GameObjects)
        {
            SceneManager.AddGameObjectRecursively(obj, null);
        }
    }
    private static void StartLifeCycle()
    {
        GameLoop.Initialize(SceneManager.CurrentScene!);
        Time.Init();

        foreach (var gameObject in SceneManager.CurrentScene!.GameObjects)
        {
            SceneManager.InvokeRecursive(gameObject, "OnEnable");
        }

        foreach (var gameObject in SceneManager.CurrentScene!.GameObjects)
        {
            SceneManager.InvokeRecursive(gameObject, "Start");
        }
    }

    private static void ExecuteLifeCycle(RenderMode renderMode)
    {
        if (renderMode is not (RenderMode.Standalone or RenderMode.EditorRun)) return;

        GameLoop.ExecuteFrame(SceneManager.CurrentScene!);
        Time.Tick();
    }

    private static bool LoadScene(string? scenePath)
    {
        var loadedScene = new Scene.Scene();
        if (SceneSerializer.LoadScene(out loadedScene, scenePath) == null)
        {
            Debug.Log("Scene deserialize error", Debug.LogLevel.Error);
            return false;
        }

        var loadScene = SceneSerializer.LoadScene(out loadedScene, scenePath);
        SceneManager.CurrentScene = loadScene;
        return true;
    }

    private static void OpenScene(string scenePath)
    {
        LoadScene(scenePath);
        _window.LoadScene?.Invoke();
        HierarchyPanel.OnChangeScene?.Invoke();
        Debug.Log($"Open scene: {scenePath}", Debug.LogLevel.Info, true);
    }

    public void Dispose()
    {
        SceneManager.AddRenderer -= AddRenderer;
        SceneManager.RemoveRenderer -= RemoveRenderer;


        RendererUI.OnProjectSave -= SaveProject;
        ProjectSetiingPanel.OnSaveProjectSettings -= SaveProjectSettings;

        ProjectFilePanel.OnSceneOpened -= OpenScene;

        ViewportPanel.OnPlayModeChanged -= ChangePlayMode;
    }
}
