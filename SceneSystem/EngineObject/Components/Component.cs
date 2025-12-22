using System.Text.Json.Serialization;
using DustyEngine.Engine.Math.Vectors;
using DustyEngine.Scene;
using SceneSystem.Attributes;
using SceneSystem.EngineObject.GameObject;

namespace DustyEngine.Components;

public class Component : EngineObject
{
    [JsonIgnore] [HideInInspector] public GameObject? Parent { get; set; }

    [HideInInspector]
    public override string Name
    {
        get => Parent?.Name ?? "<No GameObject>";
        set
        {
            if (Parent != null)
                Parent.Name = value;
        }
    }

    [JsonIgnore] [HideInInspector] public GameObject? GameObject => Parent;

    [HideInInspector] [JsonIgnore] public Transform? transform => GameObject?.GetComponent<Transform>();

    protected Component()
    {
        Id = SceneManager.GenerateComponentId();
        Debug.Log("Set Id for component: " + Id, Debug.LogLevel.Info, true);
    }


    protected T? GetComponent<T>() where T : Component => Parent?.GetComponent<T>();
    protected bool HasComponent<T>() where T : Component => Parent?.GetComponent<T>() != null;
    protected uint GetComponentId() => Id;

    protected void Instantiate(GameObject? gameObject)
    {
        if (gameObject == null)
        {
            Debug.Log($"[Component: {Name}] [ERROR] Cannot instantiate null GameObject!", Debug.LogLevel.Error, false);
            return;
        }

        Debug.Log($"[Component: {Name}] Before Instantiate: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);

        SceneManager.AddGameObjectRecursively(gameObject, null);

        Debug.Log($"[Component: {Name}] After Instantiate: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);
    }

    protected void Instantiate(GameObject gameObject, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        if (gameObject == null)
        {
            Debug.Log($"[Component: {Name}] [ERROR] Cannot instantiate null GameObject!", Debug.LogLevel.Error, false);
            return;
        }

        Debug.Log(
            $"[Component: {Name}] Before Instantiate with Transform: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);

        var targetTransform = gameObject.GetComponent<Transform>();
        if (targetTransform != null)
        {
            targetTransform.LocalPosition = position;
            targetTransform.LocalRotation = rotation;
            targetTransform.LocalScale = scale;
        }
        else
        {
            Debug.Log(
                $"[Component: {Name}] [WARNING] GameObject [{gameObject.Name}] has no Transform component! Transform values ignored.",
                Debug.LogLevel.Warning, false);
        }

        SceneManager.AddGameObjectRecursively(gameObject, null);

        Debug.Log(
            $"[Component: {Name}] After Instantiate with Transform: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);
    }

    protected void Instantiate(GameObject gameObject, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (gameObject == null)
        {
            Debug.Log($"[Component: {Name}] [ERROR] Cannot instantiate null GameObject!", Debug.LogLevel.Error, false);
            return;
        }

        Debug.Log(
            $"[Component: {Name}] Before Instantiate with Quaternion Transform: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);

        var targetTransform = gameObject.GetComponent<Transform>();
        if (targetTransform != null)
        {
            targetTransform.LocalPosition = position;
            targetTransform.LocalRotationQuat = rotation;
            targetTransform.LocalScale = scale;
        }
        else
        {
            Debug.Log(
                $"[Component: {Name}] [WARNING] GameObject [{gameObject.Name}] has no Transform component! Transform values ignored.",
                Debug.LogLevel.Warning, false);
        }

        SceneManager.AddGameObjectRecursively(gameObject, null);

        Debug.Log(
            $"[Component: {Name}] After Instantiate with Quaternion Transform: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);
    }

    protected void Instantiate(GameObject gameObject, Transform transformData)
    {
        if (transformData == null)
        {
            Instantiate(gameObject);
            return;
        }

        Instantiate(gameObject, transformData.LocalPosition, transformData.LocalRotation, transformData.LocalScale);
    }

    protected void Instantiate(GameObject gameObject, GameObject? parent)
    {
        if (gameObject == null)
        {
            Debug.Log($"[Component: {Name}] [ERROR] Cannot instantiate null GameObject!", Debug.LogLevel.Error, false);
            return;
        }

        Debug.Log(
            $"[Component: {Name}] Before Instantiate with Parent: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);

        SceneManager.AddGameObjectRecursively(gameObject, parent);

        Debug.Log(
            $"[Component: {Name}] After Instantiate with Parent: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);
    }

    protected void Instantiate(GameObject gameObject, GameObject? parent, Vector3 position, Vector3 rotation,
        Vector3 scale)
    {
        if (gameObject == null)
        {
            Debug.Log($"[Component: {Name}] [ERROR] Cannot instantiate null GameObject!", Debug.LogLevel.Error, false);
            return;
        }

        Debug.Log(
            $"[Component: {Name}] Before Instantiate with Parent and Transform: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);

        var targetTransform = gameObject.GetComponent<Transform>();
        if (targetTransform != null)
        {
            targetTransform.LocalPosition = position;
            targetTransform.LocalRotation = rotation;
            targetTransform.LocalScale = scale;
        }
        else
        {
            Debug.Log(
                $"[Component: {Name}] [WARNING] GameObject [{gameObject.Name}] has no Transform component! Transform values ignored.",
                Debug.LogLevel.Warning, false);
        }

        SceneManager.AddGameObjectRecursively(gameObject, parent);

        Debug.Log(
            $"[Component: {Name}] After Instantiate with Parent and Transform: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);
    }

    protected void Instantiate(GameObject gameObject, GameObject? parent, Vector3 position, Quaternion rotation,
        Vector3 scale)
    {
        if (gameObject == null)
        {
            Debug.Log($"[Component: {Name}] [ERROR] Cannot instantiate null GameObject!", Debug.LogLevel.Error, false);
            return;
        }

        Debug.Log(
            $"[Component: {Name}] Before Instantiate with Parent and Quaternion Transform: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);

        var targetTransform = gameObject.GetComponent<Transform>();
        if (targetTransform != null)
        {
            targetTransform.LocalPosition = position;
            targetTransform.LocalRotationQuat = rotation;
            targetTransform.LocalScale = scale;
        }
        else
        {
            Debug.Log(
                $"[Component: {Name}] [WARNING] GameObject [{gameObject.Name}] has no Transform component! Transform values ignored.",
                Debug.LogLevel.Warning, false);
        }

        SceneManager.AddGameObjectRecursively(gameObject, parent);

        Debug.Log(
            $"[Component: {Name}] After Instantiate with Parent and Quaternion Transform: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);
    }

    protected void Destroy(GameObject gameObject)
    {
        if (gameObject == null)
        {
            Debug.Log($"[Component: {Name}] [WARNING] Attempted to destroy null GameObject!", Debug.LogLevel.Warning,
                false);
            return;
        }

        Debug.Log($"[Component: {Name}] Before Destroy: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);

        SceneManager.RemoveGameObjectRecursively(gameObject);

        Debug.Log($"[Component: {Name}] After Destroy: GameObjects={SceneManager.GetTotalObjectsCount()}",
            Debug.LogLevel.Info, true);
    }

    protected void DestroyImmediate(GameObject gameObject)
    {
        if (gameObject == null)
        {
            Debug.Log($"[Component: {Name}] [WARNING] Attempted to destroy null GameObject immediately!",
                Debug.LogLevel.Warning, false);
            return;
        }

        Debug.Log($"[Component: {Name}] Destroying GameObject [{gameObject.Name}] immediately", Debug.LogLevel.Info,
            true);

        SceneManager.RemoveGameObjectRecursively(gameObject);
    }
}
