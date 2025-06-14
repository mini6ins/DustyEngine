using System.Text.Json.Serialization;
using DustyEngine.Scene;

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
    
    public void Instantiate(GameObject gameObject)
    {
        Debug.Log($"[Scene: {Name}] Before Instantiate: GameObjects={SceneManager.GetTotalObjectsCount()}", Debug.LogLevel.Info, true);

        SceneManager.AddGameObjectRecursively(gameObject, null);

        Debug.Log($"[Scene: {Name}] After Instantiate: GameObjects={SceneManager.GetTotalObjectsCount()}", Debug.LogLevel.Info, true);
    }

    public void Instantiate(GameObject gameObject, Transform transform)
    {
        Debug.Log($"[Scene: {Name}] Before Instantiate: GameObjects={SceneManager.GetTotalObjectsCount()}", Debug.LogLevel.Info, true);

        var targetTransform = gameObject.GetComponent<Transform>();
        if (targetTransform != null)
        {
            targetTransform.LocalPosition = transform.LocalPosition;
            targetTransform.LocalRotation = transform.LocalRotation;
            targetTransform.LocalScale = transform.LocalScale;
        }
        else
        {
            Debug.Log($"[Scene: {Name}] [ERROR] GameObject [{gameObject.Name}] has no Transform component!", Debug.LogLevel.Error, false);
        }

        SceneManager.AddGameObjectRecursively(gameObject, null);

        Debug.Log($"[Scene: {Name}] After Instantiate: GameObjects={SceneManager.GetTotalObjectsCount()}", Debug.LogLevel.Info, true);
    }
    
    public void Instantiate(GameObject gameObject, GameObject? parent)
    {
        Debug.Log($"[Scene: {Name}] Before Instantiate: GameObjects={SceneManager.GetTotalObjectsCount()}", Debug.LogLevel.Info, true);

        SceneManager.AddGameObjectRecursively(gameObject, parent);

        Debug.Log($"[Scene: {Name}] After Instantiate: GameObjects={SceneManager.GetTotalObjectsCount()}", Debug.LogLevel.Info, true);
    }

    public void Destroy(GameObject gameObject)
    {
        Debug.Log($"[Scene: {Name}] Before Destroy: GameObjects={SceneManager.GetTotalObjectsCount()}", Debug.LogLevel.Info, true);

        SceneManager.RemoveGameObjectRecursively(gameObject);

        Debug.Log($"[Scene: {Name}] After Destroy: GameObjects={SceneManager.GetTotalObjectsCount()}", Debug.LogLevel.Info, true);
    }
}