using System.Text.Json;
using System.Text.Json.Serialization;
using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Core;
using DustyEngine.Scene;
using SceneSystem.EngineObject.GameObject;

namespace SceneSystem.Converters;

public class SceneConverter : JsonConverter<DustyEngine.Scene.Scene>
{
    public override DustyEngine.Scene.Scene Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);

        var scene = new DustyEngine.Scene.Scene();

        if (doc.RootElement.TryGetProperty("Name", out var nameElement))
            scene.Name = nameElement.GetString();

        if (doc.RootElement.TryGetProperty("Path", out var pathElement))
            scene.Path = pathElement.GetString() ?? string.Empty;

        if (doc.RootElement.TryGetProperty("GameObjects", out var gameObjectsElement))
            scene.GameObjects = DeserializeGameObjects(gameObjectsElement, null, options);

        if (doc.RootElement.TryGetProperty("Components", out var componentsElement))
        {
            scene.Components = JsonSerializer.Deserialize<List<Component>>(
                componentsElement.GetRawText(),
                options) ?? [];
        }

        return scene;
    }
    
    private static List<GameObject> DeserializeGameObjects(JsonElement element, GameObject? parent,
        JsonSerializerOptions options)
    {
        var gameObjects = new List<GameObject>();

        foreach (var objElement in element.EnumerateArray())
        {
            var gameObject = new GameObject();

            if (objElement.TryGetProperty("Name", out var nameElement))
                gameObject.Name = nameElement.GetString();

            if (objElement.TryGetProperty("ActiveSelf", out var isActiveElement))
                gameObject.ActiveSelf = isActiveElement.GetBoolean();

            gameObject.Parent = parent;

            if (objElement.TryGetProperty("Components", out var componentsElement))
            {
                var components = JsonSerializer.Deserialize<List<Component>>(componentsElement.GetRawText(), options);

                Debug.Log($"GameObject {gameObject.Name}: components count = {components?.Count ?? 0}",
                    Debug.LogLevel.Info, true);

                if (components != null)
                    foreach (var component in components)
                    {
                        Debug.Log($"Add component {component.GetType().FullName} to {gameObject.Name}",
                            Debug.LogLevel.Info, true);

                        gameObject.AddComponent(component);
                    }
            }

            if (objElement.TryGetProperty("Children", out var childrenElement))
                gameObject.Children = DeserializeGameObjects(childrenElement, gameObject, options);

            gameObjects.Add(gameObject);
        }

        return gameObjects;
    }

    public override void Write(Utf8JsonWriter writer, DustyEngine.Scene.Scene value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Name", value.Name);
        writer.WriteString("Path", PathUtility.GetRelativePath(value.Path));

        writer.WritePropertyName("GameObjects");
        JsonSerializer.Serialize(writer, value.GameObjects, options);

        writer.WritePropertyName("Components");
        JsonSerializer.Serialize(writer, value.Components, options);

        writer.WriteEndObject();
    }
}