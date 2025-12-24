using DustyEngine.Components;
using DustyEngine.Scene;
using Editor;
using Editor.Panels.ConsolePanel;
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

        if (!LoadScene()) return;

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
        return false;
    }

    private static void SetupDebug(RenderMode renderMode)
    {
        Debug.EnableDebugMode(_settings.Debug);
        Debug.SetLogLevel(_settings.LogLevel);
        Debug.EnableConsoleLogging(_settings.LogToConsole);
        Debug.EnableFileLogging(_settings.LogToFile);

        var onDebugEnabled = Debug.EnableDebugMode;

        if (renderMode == RenderMode.Editor)
        {
            ConsolePanel.InitializeConsoleInterceptor(onDebugEnabled, _settings.Debug);
            RendererUI.OnProjectSave += () =>
                SceneSerializer.SaveScene(SceneManager.CurrentScene, _settings.PathToScenes.FirstOrDefault());
        }

        Debug.Log("Project settings loaded");

        Debug.Log($"Initial currentLogLevel:  {Debug.GetLogLevel()}", Debug.LogLevel.Info, true);
        Debug.Log("Test INFO", Debug.LogLevel.Info, true);
        Debug.Log("Test WARNING", Debug.LogLevel.Warning, true);
        Debug.Log("Test ERROR", Debug.LogLevel.Error, true);
        Debug.Log("Test FATAL", Debug.LogLevel.FatalError, true);
    }

    private static void StartLifeCycle()
    {
        foreach (var method in new[] { "OnEnable", "Start" })
        {
            foreach (var gameObject in SceneManager.CurrentScene!.GameObjects)
            {
                SceneManager.InvokeRecursive(gameObject, method);
            }
        }

        GameLoop.Initialize(SceneManager.CurrentScene!);
        Time.Init();
    }

    private static void ExecuteLifeCycle()
    {
        GameLoop.ExecuteFrame(SceneManager.CurrentScene!);
        Time.Tick();
    }

    private static bool LoadScene()
    {
        var scenePath = _settings.PathToScenes.FirstOrDefault();
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

    public void Dispose()
    {
        SceneManager.AddRenderer -= AddRenderer;
        SceneManager.RemoveRenderer -= RemoveRenderer;

        RendererUI.OnProjectSave -= () => SceneSerializer.SaveScene(SceneManager.CurrentScene, _settings.PathToScenes.FirstOrDefault());
    }
}
