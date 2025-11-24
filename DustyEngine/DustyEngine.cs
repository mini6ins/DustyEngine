using DustyEngine.Components;
using DustyEngine.Scene;
using GraphicsEngineOpenGL;

namespace DustyEngine
{
    public class DustyEngine
    {
        public static string ProjectFolderPath { get; set; }
        private static ProjectSettings settings = new(); 
        private static IRenderer graphicsEngineOpenGl;

        private static Action<MeshRenderer> AddRenderer = (renderer) => { graphicsEngineOpenGl.AddRenderer(renderer); };

        public void StartEngine(string path, RenderMode renderMode)
        {
            Debug.ClearLogs();

            if (path.Length == 0)
            {
                Debug.Log("No project path provided", Debug.LogLevel.FatalError, true);
                return;
            }

            ProjectFolderPath = path;
            settings = ProjectSettings.DeserializeProjectSettings(ProjectFolderPath);
            
    
            Debug.EnableDebugMode(settings.Debug);
            Debug.SetLogLevel(settings.LogLevel);
            Debug.EnableConsoleLogging(settings.LogToConsole);
            Debug.EnableFileLogging(settings.LogToFile);

            Debug.Log("Project settings loaded", Debug.LogLevel.Info, false);

            Debug.Log($"Initial currentLogLevel:  {Debug.GetLogLevel()}", Debug.LogLevel.Info, true);
            Debug.Log("Test INFO", Debug.LogLevel.Info, true);
            Debug.Log("Test WARNING", Debug.LogLevel.Warning, true);
            Debug.Log("Test ERROR", Debug.LogLevel.Error, true);
            Debug.Log("Test FATAL", Debug.LogLevel.FatalError, true);

            if (SceneSerializer.LoadScene(out var loadedScene, settings.PathToScenes.FirstOrDefault())) return;

            if (renderMode == RenderMode.Standalone)
            {
                foreach (var method in new[] { "OnEnable", "Start" })
                {
                    foreach (var gameObject in loadedScene.GameObjects)
                    {
                        SceneManager.InvokeRecursive(gameObject, method);
                    }
                }
            }

            GameLoop.Initialize(loadedScene);
            Time.Init();
            graphicsEngineOpenGl = new GraphicsEngineOpenGl();

            SceneManager.AddRenderer2 += AddRenderer;

            Action gameLoopAction = () =>
            {
                if(renderMode == RenderMode.Editor) return;
                GameLoop.ExecuteFrame(loadedScene);
                Time.Tick();
            };
            
            graphicsEngineOpenGl.RunMainLoop(loadedScene, gameLoopAction,
                settings.ScreenSize.ToOpenTK(), settings.ProjectName, settings.PathToVertShader,
                settings.PathToFragShader, settings.Vsync, renderMode);
        }
    }
}