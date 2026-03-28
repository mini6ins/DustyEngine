using System.Text.Json.Serialization;
using SceneSystem.Attributes;

namespace DustyEngine;

public class EngineObject
{
    public virtual string? Name { get; set; } = null!;

    [HideInInspector] [JsonIgnore] public uint Id { get; protected init; }
}
