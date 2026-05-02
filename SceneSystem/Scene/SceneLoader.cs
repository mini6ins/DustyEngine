using DustyEngine;
using DustyEngine.Core;

namespace SceneSystem.Scene;

public static class SceneLoader
{
    public static DustyEngine.Scene.Scene? LoadFromFile(string scenePath)
    {
        try
        {
            Debug.Log($"Starting scene loading from: {scenePath}", Debug.LogLevel.Info, true);

            string absolutePath = PathUtility.GetAbsolutePath(scenePath);

            if (!File.Exists(absolutePath))
            {
                Debug.Log($"Scene file not found: {absolutePath}", Debug.LogLevel.FatalError);
                return null;
            }

            string json = File.ReadAllText(absolutePath);
            var scene = SceneSerializer.DeserializeScene(json);

            if (scene != null)
            {
                scene.Path = PathUtility.GetRelativePath(absolutePath);
                Debug.Log($"Scene successfully loaded! Path: {scene.Path}");
            }

            return scene;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error loading scene: {ex.Message}", Debug.LogLevel.FatalError);
            return null;
        }
    }
    public static bool SaveToFile(DustyEngine.Scene.Scene sceneToSave, string scenePath)
    {
        try
        {
            Debug.Log($"Saving scene to: {scenePath}", Debug.LogLevel.Info, true);

            string absolutePath = PathUtility.GetAbsolutePath(scenePath);

            string json = SceneSerializer.SerializeScene(sceneToSave);

            string? directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(absolutePath, json);

            Debug.Log($"Scene successfully saved to: {scenePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error saving scene: {ex.Message}", Debug.LogLevel.FatalError);
            return false;
        }
    }
    public static string? FindSceneInAssets(string directory, string fileName)
    {
        try
        {
            var files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error searching for scene: {ex.Message}", Debug.LogLevel.Warning, false);
            return null;
        }
    }
}