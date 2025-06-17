using System.Text.Json;
using DustyEngine.Core.Converters;
using DustyEngine.Scene;

namespace DustyEngine;

public static class SceneSerializer
{
        public static bool LoadScene(out Scene.Scene? loadedScene, string scenePath)
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

        public static bool SaveScene(Scene.Scene sceneToSave, string scenePath)
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
}