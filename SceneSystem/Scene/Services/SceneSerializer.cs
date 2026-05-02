using System.Text.Json;
using DustyEngine;
using SceneSystem.Converters;

namespace SceneSystem;

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


    public static string SerializeScene(DustyEngine.Scene.Scene scene) => JsonSerializer.Serialize(scene, _options);

    public static DustyEngine.Scene.Scene? DeserializeScene(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DustyEngine.Scene.Scene>(json, _options);
        }
        catch (Exception ex)
        {
            Debug.Log($"Error deserializing scene: {ex.Message}", Debug.LogLevel.FatalError);
            return null;
        }
    }
}