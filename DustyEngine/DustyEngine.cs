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
    private static ProjectSettings? _settings;
    private static Window _window = null!;

    private static readonly Action<MeshRenderer> AddRenderer = meshRenderer =>
        _window.Renderer?.AddRenderer(meshRenderer);

    private static readonly Action<MeshRenderer> RemoveRenderer =
        meshRenderer => _window.Renderer?.RemoveRendererByComponent(meshRenderer);

    public static void StartEngine(string path, RenderMode renderMode)
    {
        Debug.ClearLogs();

        _settings = ProjectSettings.LoadProject(path);
        if (_settings == null)
        {
            Debug.Log("Can't load project settings", Debug.LogLevel.FatalError, true);
            return;
        }

        ProjectFolderPath = path;
        SceneManager.ProjectPath = ProjectFolderPath;
        SetupDebug(renderMode);

        if (!LoadScene(_settings.PathToScenes.FirstOrDefault())) return;

        if (renderMode == RenderMode.Standalone)
            GameLoop.StartLifeCycle();

        CreateWindow(renderMode);
    }

    private static void CreateWindow(RenderMode renderMode)
    {
        SceneManager.AddRenderer += AddRenderer;
        SceneManager.RemoveRenderer += RemoveRenderer;

        _window = new Window(
           GameLoop. ExecuteLifeCycle,
            _settings.ScreenSize.ToOpenTK(),
            _settings.ProjectName,
            _settings.PathToVertShader,
            _settings.PathToFragShader,
            _settings.Vsync,
            renderMode,
            ProjectFolderPath);

        _window.Run();
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
            RendererUI.OnProjectSave += () => ProjectSettings.SaveProject(_settings);
            ProjectSetiingPanel.OnSaveProjectSettings += () => ProjectSettings.SaveProjectSettings(_settings);
            ProjectFilePanel.OnSceneOpened += OpenScene;
            ViewportPanel.OnPlayModeChanged += ChangePlayMode;
        }

        Debug.Log("Project settings loaded");
        Debug.Log($"Initial currentLogLevel: {Debug.GetLogLevel()}", Debug.LogLevel.Info, true);
        Debug.Log("Test INFO", Debug.LogLevel.Info, true);
        Debug.Log("Test WARNING", Debug.LogLevel.Warning, true);
        Debug.Log("Test ERROR", Debug.LogLevel.Error, true);
        Debug.Log("Test FATAL", Debug.LogLevel.FatalError, true);
    }

    private static Scene.Scene? _sceneSnapshot;

    private static void ChangePlayMode(RenderMode renderMode)
    {
        if (renderMode == RenderMode.EditorRun)
        {
            _sceneSnapshot = SceneManager.CloneScene(SceneManager.CurrentScene!);
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

            GameLoop.StartLifeCycle();
        }
        else
        {
            _window.ChangePlayMode(RenderMode.EditorStop);

            if (_sceneSnapshot != null)
            {
                SceneManager.RestoreScene(_sceneSnapshot);
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


    private static bool LoadScene(string? scenePath)
    {
        if (SceneSerializer.LoadScene(out var loadedScene, scenePath) == null)
        {
            Debug.Log("Scene deserialize error", Debug.LogLevel.Error);
            return false;
        }

        SceneManager.LoadScene(loadedScene!);
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

        RendererUI.OnProjectSave -= () => ProjectSettings.SaveProject(_settings);
        ProjectSetiingPanel.OnSaveProjectSettings -= () => ProjectSettings.SaveProjectSettings(_settings);

        ProjectFilePanel.OnSceneOpened -= OpenScene;
        ViewportPanel.OnPlayModeChanged -= ChangePlayMode;
    }
}
