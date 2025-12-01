using DustyEngine.Components;
using DustyEngine.Scene;
using GraphicsEngine;
using GraphicsEngineOpenGL;

namespace DustyEngine
{
    public class DustyEngine
    {
        public static string ProjectFolderPath { get; set; } = null!;
        private static ProjectSettings _settings = new(); 
        private static IRenderer _graphicsEngineOpenGl = null!;

        private static Action<MeshRenderer> _addRenderer = (renderer) => { _graphicsEngineOpenGl.AddRenderer(renderer); };

        public static void StartEngine(string path, RenderMode renderMode)
        {
            Debug.ClearLogs();

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

            Debug.Log("Project settings loaded", Debug.LogLevel.Info, false);

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

            GameLoop.Initialize(loadedScene!);
            Time.Init();
            _graphicsEngineOpenGl = new GraphicsEngineOpenGl();

            SceneManager.AddRenderer2 += _addRenderer;

            _graphicsEngineOpenGl.RunMainLoop(loadedScene!, GameLoopAction,
                _settings.ScreenSize.ToOpenTK(), _settings.ProjectName, _settings.PathToVertShader,
                _settings.PathToFragShader, _settings.Vsync, renderMode);
            return;

            void GameLoopAction()
            {
                if (renderMode == RenderMode.Editor) return;
                GameLoop.ExecuteFrame(loadedScene!);
                Time.Tick();
            }
        }
    }
}