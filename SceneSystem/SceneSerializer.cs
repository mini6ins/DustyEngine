using System.Text.Json;
using DustyEngine.Core;
using SceneSystem.Converters;

namespace DustyEngine;

public static class SceneSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters =
        {
            new ComponentConverter(),
            new SceneConverter()
        }
    };


    public static string SerializeScene(Scene.Scene scene) => JsonSerializer.Serialize(scene, _options);

    public static Scene.Scene? DeserializeScene(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Scene.Scene>(json, _options);
        }
        catch (Exception ex)
        {
            Debug.Log($"Error deserializing scene: {ex.Message}", Debug.LogLevel.FatalError);
            return null;
        }
    }
}