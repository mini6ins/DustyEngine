using System.Text.Json.Serialization;
using DustyEngine.Components;
using SceneSystem.Attributes;

namespace DustyEngine;

[HideInAddComponentMenu]
public class Behaviour : Component
{
    public bool Enabled { get; set; } = true;

    [HideInInspector]  [JsonIgnore] public bool IsActiveAndEnabled => Parent?.IsActive == true && Enabled;

    public void SetActive(bool active)
    {
        if (!Parent.IsActive) return;
        var method = GetType().GetMethod(active ? "OnEnable" : "OnDisable")!;
        method.Invoke(this, null);

        Debug.Log($"{GetType().Name} is {(active ? "active" : "inactive")} on GameObject: {Parent.Name}",
            Debug.LogLevel.Info, true);
        Enabled = active;
    }
}
