using DustyEngine.Components;
using DustyEngine.Scene;
using DustyEngineEditor.Panels.ConsolePanel;
using GraphicsEngine;
using GraphicsEngineOpenGL;
using GraphicsEngineOpenGL.Editor;
using SceneSystem.EngineObject.GameObject;

namespace DustyEngine;

public sealed class DustyEngine :  IDisposable
{
    public static string ProjectFolderPath { get; set; } = null!;
    private static ProjectSettings _settings = new();
    private static IRenderer GraphicsEngineOpenGl = null!;

    private static readonly Action<MeshRenderer> AddRenderer = renderer => { GraphicsEngineOpenGl.AddRenderer(renderer); };
    private static readonly Action<GameObject> RemoveRenderer = gameObject => { GraphicsEngineOpenGl.RemoveRenderer(gameObject); };


    public static void StartEngine(string path, RenderMode renderMode)
    {
        Debug.ClearLogs();

        if(renderMode == RenderMode.Editor) ConsolePanel.InitializeConsoleInterceptor();

        if (path.Length == 0)
        {
            Debug.Log("No project path provided", Debug.LogLevel.FatalError, true);
            return;
        }

        ProjectFolderPath = path;
        _settings = ProjectSettings.DeserializeProjectSettings(ProjectFolderPath);


        Debug.EnableDebugMode(_settings.Debug);
        Debug.SetLogLevel(_settings.LogLevel);
        Debug.EnableConsoleLogging(_settings.LogToConsole);
        Debug.EnableFileLogging(_settings.LogToFile);

        Debug.Log("Project settings loaded");

        Debug.Log($"Initial currentLogLevel:  {Debug.GetLogLevel()}", Debug.LogLevel.Info, true);
        Debug.Log("Test INFO", Debug.LogLevel.Info, true);
        Debug.Log("Test WARNING", Debug.LogLevel.Warning, true);
        Debug.Log("Test ERROR", Debug.LogLevel.Error, true);
        Debug.Log("Test FATAL", Debug.LogLevel.FatalError, true);

        if (SceneSerializer.LoadScene(out var loadedScene, _settings.PathToScenes.FirstOrDefault())) return;

        if (renderMode == RenderMode.Standalone)
        {
            foreach (var method in new[] { "OnEnable", "Start" })
            {
                foreach (var gameObject in loadedScene!.GameObjects)
                {
                    SceneManager.InvokeRecursive(gameObject, method);
                }
            }
        }
        else
            RendererUI.OnProjectSave += () => SceneSerializer.SaveScene(SceneManager.CurrentScene, _settings.PathToScenes.FirstOrDefault());

        GameLoop.Initialize(loadedScene!);
        Time.Init();
        GraphicsEngineOpenGl = new Window();

        SceneManager.AddRenderer2 += AddRenderer;
        SceneManager.RemoveRenderer += RemoveRenderer;

        GraphicsEngineOpenGl.RunMainLoop(GameLoopAction,
            _settings.ScreenSize.ToOpenTK(), _settings.ProjectName, _settings.PathToVertShader,
            _settings.PathToFragShader, _settings.Vsync, renderMode, ProjectFolderPath);
        return;

        void GameLoopAction()
        {
            if (renderMode == RenderMode.Editor) return;
            GameLoop.ExecuteFrame(loadedScene!);
            Time.Tick();
        }
    }

    public void Dispose()
    {
        SceneManager.AddRenderer2 -= AddRenderer;
        SceneManager.RemoveRenderer -= RemoveRenderer;

        RendererUI.OnProjectSave -= () => SceneSerializer.SaveScene(SceneManager.CurrentScene, _settings.PathToScenes.FirstOrDefault());
    }
}
