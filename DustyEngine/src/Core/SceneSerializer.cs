using System.Text.Json;
using DustyEngine.Core.Converters;

namespace DustyEngine;

public static class SceneSerializer
{
    public static Scene.Scene? LoadScene(out Scene.Scene? loadedScene, string scenePath)
    {
        loadedScene = new Scene.Scene();
        try
        {
            Debug.Log($"Starting scene loading from: {scenePath}", Debug.LogLevel.Info, true);

            if (!File.Exists(scenePath))
            {
                Debug.Log($"Scene file not found: {scenePath}", Debug.LogLevel.FatalError);
                return null;
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

            if (loadedScene != null)
            {
                loadedScene.Path = scenePath;
                Debug.Log($"Scene successfully loaded! Path: {scenePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error loading scene: {ex.Message}", Debug.LogLevel.FatalError, false);
        }

        return loadedScene;
    }

    public static bool SaveScene(Scene.Scene sceneToSave, string scenePath)
    {
        try
        {
            Debug.Log($"Saving scene to: {scenePath}", Debug.LogLevel.Info, true);

            sceneToSave.Path = scenePath;

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

            var json = JsonSerializer.Serialize(sceneToSave, options);
            File.WriteAllText(scenePath, json);

            Debug.Log($"Scene successfully saved to: {scenePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error saving scene: {ex.Message}", Debug.LogLevel.FatalError);
            return false;
        }
    }
}
