using DustyEngine.Components;

namespace DustyEngine.Scene;

public class Scene
{
    public string Name { get; set; }
    public List<GameObject> GameObjects { get; set; } = new List<GameObject>();
    public List<Component> Components { get; set; } = new List<Component>();

    public void Instantiate(GameObject gameObject)
    {
        Debug.Log($"[Scene: {Name}] Before Instantiate: GameObjects={GetTotalObjectsCount()}", Debug.LogLevel.Info, true);

        AddGameObjectRecursively(gameObject, null);

        Debug.Log($"[Scene: {Name}] After Instantiate: GameObjects={GetTotalObjectsCount()}", Debug.LogLevel.Info, true);
    }

    public void Instantiate(GameObject gameObject, Transform transform)
    {
        Debug.Log($"[Scene: {Name}] Before Instantiate: GameObjects={GetTotalObjectsCount()}", Debug.LogLevel.Info, true);

        Transform targetTransform = gameObject.GetComponent<Transform>();
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

        AddGameObjectRecursively(gameObject, null);

        Debug.Log($"[Scene: {Name}] After Instantiate: GameObjects={GetTotalObjectsCount()}", Debug.LogLevel.Info, true);
    }
    
    public void Instantiate(GameObject gameObject, GameObject? parent)
    {
        Debug.Log($"[Scene: {Name}] Before Instantiate: GameObjects={GetTotalObjectsCount()}", Debug.LogLevel.Info, true);

        AddGameObjectRecursively(gameObject, parent);

        Debug.Log($"[Scene: {Name}] After Instantiate: GameObjects={GetTotalObjectsCount()}", Debug.LogLevel.Info, true);
    }

    public void Destroy(GameObject gameObject)
    {
        Debug.Log($"[Scene: {Name}] Before Destroy: GameObjects={GetTotalObjectsCount()}", Debug.LogLevel.Info, true);

        RemoveGameObjectRecursively(gameObject);

        Debug.Log($"[Scene: {Name}] After Destroy: GameObjects={GetTotalObjectsCount()}", Debug.LogLevel.Info, true);
    }

    private void AddGameObjectRecursively(GameObject gameObject, GameObject? parent)
    {
        if (parent == null)
        {
            if (!GameObjects.Contains(gameObject))
            {
                GameObjects.Add(gameObject);
                Debug.Log($"[Scene: {Name}] Added GameObject [{gameObject.Name}] to Scene", Debug.LogLevel.Info, true);
            }
        }
        else
        {
            parent.Children.Add(gameObject);
            gameObject.Parent = parent;
            Debug.Log($"[Scene: {Name}] Added GameObject [{gameObject.Name}] under Parent [{parent.Name}]", Debug.LogLevel.Info, true);
        }

        gameObject.InvokeMethodInComponents("OnEnable");
        gameObject.InvokeMethodInComponents("Start");

        foreach (var child in gameObject.Children)
        {
            AddGameObjectRecursively(child, gameObject);
        }
    }

    private void RemoveGameObjectRecursively(GameObject gameObject)
    {
        foreach (var child in gameObject.Children)
        {
            RemoveGameObjectRecursively(child);
        }

        if (GameObjects.Remove(gameObject))
        {
            gameObject.Destroy();
            Debug.Log($"[Scene: {Name}] Removed GameObject [{gameObject.Name}] from Scene", Debug.LogLevel.Info, true);
        }
        else if (gameObject.Parent != null)
        {
            gameObject.Parent.Children.Remove(gameObject);
            Debug.Log($"[Scene: {Name}] Removed GameObject [{gameObject.Name}] from Parent [{gameObject.Parent.Name}]", Debug.LogLevel.Info, true);
        }
        else
        {
            Debug.Log($"[Scene: {Name}] [WARNING] GameObject [{gameObject.Name}] not found in scene or parent!", Debug.LogLevel.Warning, false);
        }
    }

    private int GetTotalObjectsCount()
    {
        int count = 0;

        foreach (var gameObject in GameObjects)
        {
            count += CountChildrenRecursively(gameObject);
        }

        return count;
    }

    private int CountChildrenRecursively(GameObject gameObject)
    {
        int count = 1;

        foreach (var child in gameObject.Children)
        {
            count += CountChildrenRecursively(child);
        }

        return count;
    }
}
