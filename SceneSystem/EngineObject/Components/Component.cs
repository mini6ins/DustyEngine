using System.Text.Json.Serialization;

namespace DustyEngine.Components;

public class Component : EngineObject
{
    public GameObject Parent { get; set; }
    public override string Name
    {
        get => Parent?.Name ?? "<No GameObject>";
        set
        {
            if (Parent != null)
                Parent.Name = value;
        }
    }
    
    public GameObject GameObject => Parent;
   [JsonIgnore] public Transform transform => GameObject.GetComponent<Transform>();
    
    public T? GetComponent<T>() where T : Component
    {
        return Parent?.GetComponent<T>();
    }
}