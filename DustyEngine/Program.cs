using System.Text.Json;
using DustyEngine;
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
        public static ProjectSettings settings = new ProjectSettings();
        private static GraphicsEngineOpenGL.GraphicsEngineOpenGl graphicsEngineOpenGl;

        public static Action<MeshRenderer> AddRenderer = (renderer) => { graphicsEngineOpenGl.AddRenderer(renderer); };
     


        static void Main(string[] args)
        {
            Debug.ClearLogs();

            ProjectFolderPath = "C:\\Users\\maksym\\Desktop\\GameTestEngine";

            ProjectSettings projectSettings = new ProjectSettings
            {
                ProjectName = "My Game",
                Version = 1.0f,
                PathToScenes = new List<String>
                {
                    "C:\\Users\\maksym\\Desktop\\GameTestEngine\\Assets\\DustyEngineTestScene.json",
                },
                PathToVertShader = "C:\\Users\\maksym\\Desktop\\GameTestEngine\\Assets\\shaders\\shader.vert",
                PathToFragShader = "C:\\Users\\maksym\\Desktop\\GameTestEngine\\Assets\\shaders\\shader.frag",
                Debug = true,
                LogLevel = Debug.LogLevel.Info,
                LogToConsole = true,
                LogToFile = true,
                ScreenSize = new Vector2(800, 600),
                Vsync = true,
            };

            SerializeProjectSettings(projectSettings);

            Debug.Log("Starting Dusty Engine", Debug.LogLevel.Info, false);

            if (ProjectFolderPath != null)
                Debug.Log("Project folder path: " + ProjectFolderPath, Debug.LogLevel.Info, true);
            else
                Debug.Log("Project folder path is null", Debug.LogLevel.FatalError, false);

            DeserializeProjectSettings();

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
                        Path = "C:\\Users\\maksym\\Desktop\\GameTestEngine\\Assets\\cube.obj",
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
                        LocalScale = new Vector3(0.1f, 0.1f, 0.1f),
                    },
                    new MeshRenderer
                    {
                        Path = "C:\\Users\\maksym\\Desktop\\GameTestEngine\\Assets\\TeddyBear.obj",
                    },
                }
            };
            try
            {
                var moveBoxScript = ComponentConverter.LoadOrCompileComponent(
                    "C:\\Users\\maksym\\Desktop\\GameTestEngine\\Assets\\MoveBoxCode.cs"
                );
                if (moveBoxScript != null)
                {
                    obj1.Components.Add(moveBoxScript);
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

            scene.GameObjects[0].AddChild(obj1);

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
                        AspectRatio = projectSettings.ScreenSize.X / (float)projectSettings.ScreenSize.Y,
                    }
                }
            };
            try
            {
                var playerScript = ComponentConverter.LoadOrCompileComponent(
                    "C:\\Users\\maksym\\Desktop\\GameTestEngine\\Assets\\Player.cs"
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

            SaveScene(scene,
                "C:\\Users\\maksym\\Desktop\\GameTestEngine\\Assets\\DustyEngineTestScene.json");
            if (LoadScene(out var loadedScene, projectSettings.PathToScenes.FirstOrDefault())) return;
            foreach (var method in new[] { "OnEnable", "Start" })
            {
                foreach (var gameObject in loadedScene.GameObjects)
                {
                    InvokeRecursive(gameObject, method);
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
                projectSettings.ScreenSize, projectSettings.ProjectName, projectSettings.PathToVertShader,
                projectSettings.PathToFragShader, projectSettings.Vsync);
        }


        private static void DeserializeProjectSettings()
        {
            string filePath = Path.Combine(ProjectFolderPath, "Settings/project_settings.json");

            if (!File.Exists(filePath))
            {
                Debug.Log("Project settings file not found", Debug.LogLevel.FatalError, true);
            }
            else
            {
                string fileContent = File.ReadAllText(filePath);
                settings = JsonSerializer.Deserialize<ProjectSettings>(fileContent);

                if (settings == null)
                {
                    Debug.Log("Project settings could not be loaded", Debug.LogLevel.FatalError, true);
                }
            }
        }

        private static void SerializeProjectSettings(ProjectSettings projectSettings)
        {
            string json = JsonSerializer.Serialize(projectSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(ProjectFolderPath, "Settings/project_settings.json"), json);
        }

        private static bool LoadScene(out Scene.Scene? loadedScene, string scenePath)
        {
            loadedScene = new Scene.Scene();
            try
            {
                Debug.Log($"Starting scene loading from: {scenePath}", Debug.LogLevel.Info, true);

                if (!File.Exists(scenePath))
                {
                    Debug.Log($"Scene file not found: {scenePath}", Debug.LogLevel.FatalError, false);
                    return true;
                }

                loadedScene = JsonSerializer.Deserialize<Scene.Scene>(
                    File.ReadAllText(scenePath),
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        IncludeFields = true,
                        Converters =
                        {
                            new ComponentConverter(),
                            new SceneConverter()
                        }
                    });

                Debug.Log("Scene successfully loaded!", Debug.LogLevel.Info, false);
            }
            catch (Exception ex)
            {
                Debug.Log($"Error loading scene: {ex.Message}", Debug.LogLevel.FatalError, false);
            }

            SceneManager.AddScene(loadedScene);
            return false;
        }

        private static bool SaveScene(Scene.Scene sceneToSave, string scenePath)
        {
            try
            {
                Debug.Log($"Saving scene to: {scenePath}", Debug.LogLevel.Info, true);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true,
                    Converters =
                    {
                        new ComponentConverter(),
                        new SceneConverter()
                    }
                };

                string json = JsonSerializer.Serialize(sceneToSave, options);
                File.WriteAllText(scenePath, json);

                Debug.Log("Scene successfully saved!", Debug.LogLevel.Info, false);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log($"Error saving scene: {ex.Message}", Debug.LogLevel.FatalError, false);
                return false;
            }
        }

        private static void InvokeRecursive(GameObject gameObject, string methodName)
        {
            if (gameObject.IsActive)
            {
                gameObject.InvokeMethodInComponents(methodName);
            }

            foreach (var child in gameObject.Children)
            {
                InvokeRecursive(child, methodName);
            }
        }
    }
}

public class ProjectSettings
{
    public string ProjectName { get; set; }
    public float Version { get; set; }
    public List<string> PathToScenes { get; set; }
    public string PathToFragShader { get; set; }
    public string PathToVertShader { get; set; }
    public bool Debug { get; set; }
    public Debug.LogLevel LogLevel { get; set; }
    public bool LogToConsole { get; set; }
    public bool LogToFile { get; set; }
    public Vector2 ScreenSize { get; set; }
    public bool Vsync { get; set; }
}