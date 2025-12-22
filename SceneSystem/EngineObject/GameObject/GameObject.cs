using System.Reflection;
using System.Text.Json.Serialization;
using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using SceneSystem.Attributes;

namespace SceneSystem.EngineObject.GameObject;

public sealed class GameObject : DustyEngine.EngineObject
{
    [HideInInspector] [JsonIgnore]  public int Id { get; }
    public bool ActiveSelf { get; set; } = true;

    [JsonIgnore] public bool ActiveInHierarchy => ActiveSelf && (Parent?.ActiveInHierarchy ?? true);


    public List<GameObject> Children { get; set; } = [];
    public List<Component> Components { get; set; } = [];

    [JsonIgnore] public GameObject? Parent { get; set; }


    public GameObject(string name = "New GameObject")
    {
        Id = SceneManager.GenerateGameObjectId();
        Debug.Log("Set Id: " + Id, Debug.LogLevel.Info, true);
        Name = name;
    }

    public void SetActive(bool active)
    {
        if (ActiveSelf == active)
            return;

        ActiveSelf = active;

        InvokeMethodInComponents(ActiveInHierarchy ? "OnEnable" : "OnDisable");

        foreach (var child in Children)
            child.OnParentActivityChanged();
    }

    private void OnParentActivityChanged()
    {
        InvokeMethodInComponents(ActiveInHierarchy ? "OnEnable" : "OnDisable");

        foreach (var child in Children)
            child.OnParentActivityChanged();
    }


    public void AddComponent(Component component)
    {
        if (component == null) throw new ArgumentNullException(nameof(component));

        Components.Add(component);
        component.Parent = this;
        Debug.Log($"Added component [{component.GetType().Name}] to GameObject [{Name}]", Debug.LogLevel.Info, true);
    }

    public void AddChild(GameObject child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void Destroy()
    {
        InvokeMethodInComponents("OnDestroy");
        Components.Clear();
    }

    public T? GetComponent<T>() where T : Component
    {
        return Components.Count == 0 ? null : Components.OfType<T>().FirstOrDefault();
    }

    public void InvokeMethodInComponents(string methodName)
    {
        Debug.Log($"[{Name}] has {Components?.Count ?? 0} components.", Debug.LogLevel.Info, true);

        if (Components == null || Components.Count == 0)
        {
            Debug.Log($"[{Name}] has no components. Skipping {methodName} execution.", Debug.LogLevel.Warning, true);
            return;
        }

        foreach (var component in Components)
        {
            component.Parent = this;

            if (component is MonoBehaviour monoBehaviour)
            {
                if (!monoBehaviour.Enabled || !ActiveInHierarchy)
                {
                    Debug.Log(
                        $"Skipping {methodName} on [{component.GetType().Name}] in [{Name}]: GameObject or component is inactive",
                        Debug.LogLevel.Info, true);
                    continue;
                }
            }

            try
            {
                var method = component.GetType().GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                    continue;


                var isLifecycleMethod = methodName is "OnEnable" or "OnDisable" or "OnDestroy";

                if ((isLifecycleMethod && component is Behaviour) ||
                    (!isLifecycleMethod && component is MonoBehaviour))
                {
                    Debug.Log($"Executing [{component.GetType().Name}.{methodName}] on [{Name}]", Debug.LogLevel.Info,
                        true);
                    method.Invoke(component, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error executing [{methodName}] in [{component.GetType().Name}]: {ex.Message}",
                    Debug.LogLevel.Error, true);
            }
        }
    }
}
