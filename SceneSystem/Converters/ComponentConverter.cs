using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DustyEngine.Components;
using DustyEngine.Core;
using SceneSystem.Attributes;

namespace SceneSystem.Converters;

public class ComponentConverter : JsonConverter<Component>
{
    private static readonly Dictionary<string, Type> BuiltInComponentTypes = new()
    {
        ["Transform"] = typeof(Transform),
        ["Camera"] = typeof(Camera),
        ["MeshRenderer"] = typeof(MeshRenderer),
    };

    public override Component Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var typeName = root.GetProperty("Type").GetString() ?? string.Empty;

        if (typeName == "Script")
            return ReadScriptComponent(root, options);

        if (!BuiltInComponentTypes.TryGetValue(typeName, out var componentType))
            throw new JsonException($"Unknown built-in component type '{typeName}'.");

        var instance = (Component)JsonSerializer.Deserialize(
            root.GetRawText(),
            componentType,
            CreateChildOptions(options))!;

        ResolveObjPathInComponent(instance);
        return instance;
    }

    private static Component ReadScriptComponent(JsonElement root, JsonSerializerOptions options)
    {
        if (!root.TryGetProperty("ScriptClass", out var scriptClassElement))
            throw new JsonException("Script component requires 'ScriptClass'.");

        var scriptClass = scriptClassElement.GetString() ?? string.Empty;
        var scriptType = ScriptAssemblyManager.ResolveScriptType(scriptClass);

        if (scriptType == null)
            throw new JsonException($"Unknown script class '{scriptClass}'.");

        var instance = (Component)JsonSerializer.Deserialize(
            root.GetRawText(),
            scriptType,
            CreateChildOptions(options))!;

        ResolveObjPathInComponent(instance);
        return instance;
    }

    public override void Write(Utf8JsonWriter writer, Component value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var type = value.GetType();

        if (IsBuiltInComponent(type))
        {
            writer.WriteString("Type", type.Name);
        }
        else
        {
            writer.WriteString("Type", "Script");
            writer.WriteString("ScriptClass", type.FullName ?? type.Name);
        }

        var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var member in members)
        {
            if (member.Name.Contains("k__BackingField"))
                continue;

            if (HasJsonIgnore(member, type))
                continue;

            var shouldSerialize = false;

            switch (member)
            {
                case FieldInfo field:
                    shouldSerialize = field.IsPublic || field.GetCustomAttribute<SerializeFieldAttribute>() != null;
                    break;

                case PropertyInfo prop:
                    if (prop.CanRead && prop.GetMethod?.IsPublic == true)
                        shouldSerialize = true;

                    if (prop.GetCustomAttribute<SerializeFieldAttribute>() != null && prop.CanRead)
                        shouldSerialize = true;
                    break;
            }

            if (!shouldSerialize)
                continue;

            object? valueToWrite;

            try
            {
                valueToWrite = member switch
                {
                    FieldInfo f => f.GetValue(value),
                    PropertyInfo p => p.GetValue(value),
                    _ => null
                };
            }
            catch
            {
                continue;
            }

            if (valueToWrite == null)
                continue;

            try
            {
                writer.WritePropertyName(member.Name);

                var toSerialize = valueToWrite;
                if (valueToWrite is string s && ShouldRelativizeObjPath(member, s))
                    toSerialize = PathUtility.GetRelativePath(s);

                JsonSerializer.Serialize(writer, toSerialize, options);
            }
            catch
            {
            }
        }

        writer.WriteEndObject();
    }

    private static JsonSerializerOptions CreateChildOptions(JsonSerializerOptions options)
    {
        var childOptions = new JsonSerializerOptions(options);

        for (int i = childOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (childOptions.Converters[i] is ComponentConverter)
            {
                childOptions.Converters.RemoveAt(i);
            }
        }

        childOptions.Converters.Add(new ComponentConverter());
        return childOptions;
    }

    private static bool IsBuiltInComponent(Type type)
    {
        return BuiltInComponentTypes.Values.Contains(type);
    }

    private static bool HasJsonIgnore(MemberInfo member, Type declaringType)
    {
        if (member.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            return true;

        return member switch
        {
            PropertyInfo prop => declaringType
                .GetProperty(prop.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?
                .GetCustomAttribute<JsonIgnoreAttribute>() != null,

            FieldInfo field => declaringType
                .GetField(field.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?
                .GetCustomAttribute<JsonIgnoreAttribute>() != null,

            _ => false
        };
    }

    private static void ResolveObjPathInComponent(Component component)
    {
        var type = component.GetType();

        var prop = type.GetProperty("Path", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(string) && prop.CanRead && prop.CanWrite)
        {
            var s = (string?)prop.GetValue(component);
            if (!string.IsNullOrWhiteSpace(s) && s.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                prop.SetValue(component, PathUtility.GetAbsolutePath(s));
        }

        var field = type.GetField("Path", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(string))
        {
            var s = (string?)field.GetValue(component);
            if (!string.IsNullOrWhiteSpace(s) && s.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                field.SetValue(component, PathUtility.GetAbsolutePath(s));
        }
    }

    private static bool ShouldRelativizeObjPath(MemberInfo member, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!string.Equals(member.Name, "Path", StringComparison.OrdinalIgnoreCase))
            return false;

        return value.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) && Path.IsPathRooted(value);
    }
}