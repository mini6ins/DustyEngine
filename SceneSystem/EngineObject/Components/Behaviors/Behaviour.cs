using System.Reflection;
using System.Text.Json.Serialization;
using DustyEngine.Components;
using SceneSystem.Attributes;

namespace DustyEngine;

[HideInAddComponentMenu]
public class Behaviour : Component
{
    public bool Enabled { get; set; } = true;

    [HideInInspector] [JsonIgnore] 
    public bool IsActiveAndEnabled => Parent?.ActiveInHierarchy == true && Enabled;

    public void SetActive(bool active)
    {
        if (Enabled == active) return; 
        if (!Parent.ActiveInHierarchy) return;

        Enabled = active;

        var methodName = active ? "OnEnable" : "OnDisable";
        var method = GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        method?.Invoke(this, null); 

        Debug.Log(
            $"{GetType().Name} is {(active ? "active" : "inactive")} on GameObject: {Parent.Name}",
            Debug.LogLevel.Info, true
        );
    }
}