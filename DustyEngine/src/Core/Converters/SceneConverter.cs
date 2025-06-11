using System.Text.Json;
using System.Text.Json.Serialization;
using DustyEngine.Components;

namespace DustyEngine.Json.Converters;

public class SceneConverter : JsonConverter<Scene.Scene>
{
    public override Scene.Scene Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var scene = new Scene.Scene();

            if (doc.RootElement.TryGetProperty("Name", out var nameElement))
            {
                scene.Name = nameElement.GetString();
            }

            if (doc.RootElement.TryGetProperty("GameObjects", out var gameObjectsElement))
            {
                scene.GameObjects = DeserializeGameObjects(gameObjectsElement, null, options);
            }

            if (doc.RootElement.TryGetProperty("Components", out var componentsElement))
            {
                scene.Components = JsonSerializer.Deserialize<List<Component>>(componentsElement.GetRawText(), options);
            }

            return scene;
        }
    }

    private List<GameObject> DeserializeGameObjects(JsonElement element, GameObject parent,
        JsonSerializerOptions options)
    {
        var gameObjects = new List<GameObject>();

        foreach (var objElement in element.EnumerateArray())
        {
            var gameObject = new GameObject();

            if (objElement.TryGetProperty("Name", out var nameElement))
            {
                gameObject.Name = nameElement.GetString();
            }

            if (objElement.TryGetProperty("IsActive", out var isActiveElement))
            {
                gameObject.IsActive = isActiveElement.GetBoolean();
            }

            gameObject.Parent = parent;

            if (objElement.TryGetProperty("Components", out var componentsElement))
            {
                var components = JsonSerializer.Deserialize<List<Component>>(componentsElement.GetRawText(), options);
                foreach (var component in components)
                {
                    gameObject.AddComponent(component);
                }
            }

            if (objElement.TryGetProperty("Children", out var childrenElement))
            {
                gameObject.Children = DeserializeGameObjects(childrenElement, gameObject, options);
            }

            gameObjects.Add(gameObject);
        }

        return gameObjects;
    }


    public override void Write(Utf8JsonWriter writer, Scene.Scene value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Name", value.Name);

        writer.WritePropertyName("GameObjects");
        JsonSerializer.Serialize(writer, value.GameObjects, options);

        writer.WritePropertyName("Components");
        JsonSerializer.Serialize(writer, value.Components, options);

        writer.WriteEndObject();
    }
}