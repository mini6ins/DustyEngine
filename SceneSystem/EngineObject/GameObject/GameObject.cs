using System.Reflection;
using System.Text.Json.Serialization;
using DustyEngine;
using DustyEngine.Components;

public class GameObject : EngineObject
{
    public bool IsActive { get; set; } = true;

    public List<GameObject> Children { get; set; } = new List<GameObject>();
    public List<Component> Components { get; set; } = new List<Component>();

    [JsonIgnore] public GameObject Parent { get; set; }

    public GameObject(string name = "New GameObject") => Name = name;
    
    public void SetActive(bool isActive)
    {
        InvokeMethodInComponents(isActive ? "OnEnable" : "OnDisable");
        Debug.Log($"{Name} is {(IsActive ? "active" : "inactive")}", Debug.LogLevel.Info, true);
        IsActive = isActive;
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
        if (Components == null || Components.Count == 0)
            return null;

        return Components.OfType<T>().FirstOrDefault();
    }

    public void InvokeMethodInComponents(string methodName)
    {
        Debug.Log($"[{Name}] has {Components?.Count ?? 0} components.", Debug.LogLevel.Info, true);

        if (Components == null || Components.Count == 0)
        {
            Debug.Log($"[{Name}] has no components. Skipping {methodName} execution.", Debug.LogLevel.Warning, true);
            return;
        }
        
        foreach (Component component in Components)
        {
            component.Parent = this;

            if (component is MonoBehaviour monoBehaviour)
            {
                if (!monoBehaviour.Enabled || !IsActive)
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

             
                bool isLifecycleMethod = methodName is "OnEnable" or "OnDisable" or "OnDestroy";
                
                if ((isLifecycleMethod && component is Behaviour) ||
                    (!isLifecycleMethod && component is MonoBehaviour))
                {
                    Debug.Log($"Executing [{component.GetType().Name}.{methodName}] on [{Name}]", Debug.LogLevel.Info, true);
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