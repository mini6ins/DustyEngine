using DustyEngine.Components;
using DustyEngine.Core.Converters;
using DustyEngine.Engine.Math.Vectors;
using DustyEngine.Scene;
using GraphicsEngineOpenGL;

namespace DustyEngine
{
    internal static class Program
    {
        public static string ProjectFolderPath { get; set; }
        private static ProjectSettings settings = new ProjectSettings();
        private static GraphicsEngineOpenGl graphicsEngineOpenGl;

        private static Action<MeshRenderer> AddRenderer = (renderer) => { graphicsEngineOpenGl.AddRenderer(renderer); };

        public static void StartEngine(string[] args)
        {
            Debug.ClearLogs();

            if (args.Length == 0)
            {
                Debug.Log("No project path provided", Debug.LogLevel.FatalError, true);
                return;
            }

            ProjectFolderPath = args[0];

            ProjectSettings.DeserializeProjectSettings(ProjectFolderPath);

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
            foreach (var method in new[] { "OnEnable", "Start" })
            {
                foreach (var gameObject in loadedScene.GameObjects)
                {
                    SceneManager.InvokeRecursive(gameObject, method);
                }
            }


            GameLoop.Initialize(loadedScene);
            Time.Init();
            graphicsEngineOpenGl = new GraphicsEngineOpenGl();

            SceneManager.AddRenderer2 += AddRenderer;

            Action gameLoopAction = () =>
            {
                GameLoop.ExecuteFrame(loadedScene);
                Time.Tick();
            };


            graphicsEngineOpenGl.RunMainLoop(loadedScene, gameLoopAction,
                settings.ScreenSize, settings.ProjectName, settings.PathToVertShader,
                settings.PathToFragShader, settings.Vsync);
        }

       private static void Main(string[] args)
        {
            Debug.ClearLogs();

            ProjectFolderPath = "/home/maksym/github/DustyEngine/TestProject";

            settings = new ProjectSettings
            {
                ProjectName = "My Game",
                Version = 1.0f,
                PathToScenes = new List<String>
                {
                    "/home/maksym/github/DustyEngine/TestProject/Assets/DustyEngineTestScene.json",
                },
                PathToVertShader = "/home/maksym/github/DustyEngine/TestProject/Assets/shaders/shader.vert",
                PathToFragShader = "/home/maksym/github/DustyEngine/TestProject/Assets/shaders/shader.frag",
                Debug = true,
                LogLevel = Debug.LogLevel.Info,
                LogToConsole = true,
                LogToFile = true,
                ScreenSize = new Vector2(800, 600),
                Vsync = true,
            };

            ProjectSettings.SerializeProjectSettings(settings, ProjectFolderPath);

            Debug.Log("Starting Dusty Engine", Debug.LogLevel.Info, false);

            if (ProjectFolderPath != null)
                Debug.Log("Project folder path: " + ProjectFolderPath, Debug.LogLevel.Info, true);
            else
                Debug.Log("Project folder path is null", Debug.LogLevel.FatalError, false);

            ProjectSettings.DeserializeProjectSettings(ProjectFolderPath);

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

            var scene = new Scene.Scene
            {
                Name = "DustyEngineTestScene"
            };

            GameObject obj0 = new GameObject
            {
                Name = "TestGameObject0",
                Components =
                {
                    new Transform
                    {
                        LocalPosition = new Vector3(0, 0, 0),
                        LocalRotation = new Vector3(0, 0, 0),
                        LocalScale = new Vector3(1, 1, 1),
                    },
                    new MeshRenderer
                    {
                        Path = "/home/maksym/github/DustyEngine/TestProject/Assets/cube.obj",
                    },
                }
            };


            scene.GameObjects.Add(obj0);

            GameObject obj1 = new GameObject
            {
                Name = "TestGameObject1",
                Components =
                {
                    new Transform
                    {
                        LocalPosition = new Vector3(5, 0, 0),
                        LocalRotation = new Vector3(0, 0, 0),
                        LocalScale = new Vector3(5f, 5f, 5f),
                    },
                    new MeshRenderer
                    {
                        Path = "/home/maksym/github/DustyEngine/TestProject/Assets/TeddyBear.obj",
                    },
                }
            };
            try
            {
                var moveBoxScript = ComponentConverter.LoadOrCompileComponent(
                    "/home/maksym/github/DustyEngine/TestProject/Assets/MoveBoxCode.cs"
                );
                if (moveBoxScript != null)
                {
                    obj0.Components.Add(moveBoxScript);
                    Debug.Log("moveBoxScript component loaded successfully", Debug.LogLevel.Info, true);
                }
                else
                {
                    Debug.Log("moveBoxScript component could not be loaded, continuing without it",
                        Debug.LogLevel.Warning, true);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to load moveBoxScript component: {ex.Message}", Debug.LogLevel.Warning, true);
                Debug.Log("Continuing without moveBoxScript component", Debug.LogLevel.Info, true);
            }

        //    scene.GameObjects[0].AddChild(obj1);
scene.GameObjects.Add(obj1);
            GameObject cameraObject = new GameObject
            {
                Name = "Camera",
                Components =
                {
                    new Transform
                    {
                        LocalPosition = new Vector3(0, 0, 10),
                    },
                    new Camera
                    {
                        AspectRatio = settings.ScreenSize.X / (float)settings.ScreenSize.Y,
                    }
                }
            };
            try
            {
                var playerScript = ComponentConverter.LoadOrCompileComponent(
                    "/home/maksym/github/DustyEngine/TestProject/Assets/Player.cs"
                );
                if (playerScript != null)
                {
                    cameraObject.Components.Add(playerScript);
                    Debug.Log("Player component loaded successfully", Debug.LogLevel.Info, true);
                }
                else
                {
                    Debug.Log("Player component could not be loaded, continuing without it", Debug.LogLevel.Warning,
                        true);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to load Player component: {ex.Message}", Debug.LogLevel.Warning, true);
                Debug.Log("Continuing without Player component", Debug.LogLevel.Info, true);
            }

            scene.GameObjects.Add(cameraObject);

            SceneSerializer.SaveScene(scene,
                "/home/maksym/github/DustyEngine/TestProject/Assets/DustyEngineTestScene.json");
            if (SceneSerializer.LoadScene(out var loadedScene, settings.PathToScenes.FirstOrDefault())) return;
            foreach (var method in new[] { "OnEnable", "Start" })
            {
                foreach (var gameObject in loadedScene.GameObjects)
                {
                    SceneManager.InvokeRecursive(gameObject, method);
                }
            }


            GameLoop.Initialize(loadedScene);
            Time.Init();
            graphicsEngineOpenGl = new GraphicsEngineOpenGl();

            SceneManager.AddRenderer2 += AddRenderer;

            Action gameLoopAction = () =>
            {
                GameLoop.ExecuteFrame(loadedScene);
                Time.Tick();
            };


            graphicsEngineOpenGl.RunMainLoop(loadedScene, gameLoopAction,
                settings.ScreenSize, settings.ProjectName, settings.PathToVertShader,
                settings.PathToFragShader, settings.Vsync);
        }
    }
}