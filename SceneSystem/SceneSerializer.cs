using System.Text.Json;
using DustyEngine.Core;
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

            string absolutePath = PathUtility.GetAbsolutePath(scenePath);

            if (!File.Exists(absolutePath))
            {
                Debug.Log($"Scene file not found: {absolutePath}", Debug.LogLevel.FatalError);
                return null;
            }

            loadedScene = JsonSerializer.Deserialize<Scene.Scene>(
                File.ReadAllText(absolutePath),
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
                loadedScene.Path = PathUtility.GetRelativePath(absolutePath);
                Debug.Log($"Scene successfully loaded! Path: {loadedScene.Path}");
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

            string absolutePath = PathUtility.GetAbsolutePath(scenePath);

            sceneToSave.Path = PathUtility.GetRelativePath(scenePath);

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

            string? directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(absolutePath, json);

            Debug.Log($"Scene successfully saved to: {sceneToSave.Path}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error saving scene: {ex.Message}", Debug.LogLevel.FatalError);
            return false;
        }
    }

    public static string SerializeSceneToJson(Scene.Scene scene)
    {
        try
        {
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

            return JsonSerializer.Serialize(scene, options);
        }
        catch (Exception ex)
        {
            Debug.Log($"Error serializing scene: {ex.Message}", Debug.LogLevel.FatalError);
            return string.Empty;
        }
    }

    public static Scene.Scene? DeserializeSceneFromJson(string json)
    {
        try
        {
            var loadedScene = JsonSerializer.Deserialize<Scene.Scene>(
                json,
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

            return loadedScene;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error deserializing scene: {ex.Message}", Debug.LogLevel.FatalError);
            return null;
        }
    }
}
