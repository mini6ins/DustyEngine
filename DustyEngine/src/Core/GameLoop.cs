using System.Reflection;
using DustyEngine.Components;

namespace DustyEngine;

public static class GameLoop
{
    private static TimeSpan accumulator = TimeSpan.Zero;
    private static DateTime previousTime = DateTime.Now;
    private static readonly TimeSpan TargetElapsedTime = TimeSpan.FromMilliseconds(16);

    private static readonly Dictionary<Type, MethodInfo> UpdateMethodCache = new();
    private static readonly Dictionary<Type, MethodInfo> FixedUpdateMethodCache = new();
    
    private static List<GameObject> TraverseGameObjects(IEnumerable<GameObject> rootObjects)
    {
        var result = new List<GameObject>();
        TraverseGameObjectsRecursive(rootObjects, result);
        return result;
    }

    private static void TraverseGameObjectsRecursive(IEnumerable<GameObject> objects, List<GameObject> result)
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            result.Add(obj);
            
            var children = obj.Children?.ToList() ?? new List<GameObject>();
            TraverseGameObjectsRecursive(children, result);
        }
    }

    private static void ExecuteUpdateLoop(Scene.Scene scene)
    {
        var gameObjectsSnapshot = TraverseGameObjects(scene.GameObjects?.ToList() ?? new List<GameObject>());
        
        foreach (var gameObject in gameObjectsSnapshot)
        {
            if (!gameObject.IsActive) continue;
            
            var componentsSnapshot = gameObject.Components?.ToList() ?? new List<Component>();
            
            foreach (var component in componentsSnapshot)
            {
                if (component is MonoBehaviour monoBehaviour && monoBehaviour.Enabled)
                {
                    var componentType = component.GetType();

                    if (!UpdateMethodCache.TryGetValue(componentType, out var updateMethod))
                    {
                        updateMethod = componentType.GetMethod("Update",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        UpdateMethodCache[componentType] = updateMethod;
                    }

                    try
                    {
                        updateMethod?.Invoke(component, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[GameLoop] Error in Update for {componentType.Name}: {ex.Message}", Debug.LogLevel.Error, false);
                    }
                }
            }
        }
    }

    private static void ExecuteFixedUpdateStep(Scene.Scene scene)
    {
        var currentTime = DateTime.Now;
        var frameTime = currentTime - previousTime;
        previousTime = currentTime;

        accumulator += frameTime;

        while (accumulator >= TargetElapsedTime)
        {
            var gameObjectsSnapshot = TraverseGameObjects(scene.GameObjects?.ToList() ?? new List<GameObject>());
            
            foreach (var gameObject in gameObjectsSnapshot)
            {
                if (!gameObject.IsActive) continue;
                
                var componentsSnapshot = gameObject.Components?.ToList() ?? new List<Component>();
                
                foreach (var component in componentsSnapshot)
                {
                    if (component is MonoBehaviour monoBehaviour && monoBehaviour.Enabled)
                    {
                        var componentType = component.GetType();

                        if (!FixedUpdateMethodCache.TryGetValue(componentType, out var fixedUpdateMethod))
                        {
                            fixedUpdateMethod = componentType.GetMethod("FixedUpdate",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            FixedUpdateMethodCache[componentType] = fixedUpdateMethod;
                        }

                        try
                        {
                            fixedUpdateMethod?.Invoke(component, null);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"[GameLoop] Error in FixedUpdate for {componentType.Name}: {ex.Message}", Debug.LogLevel.Error, false);
                        }
                    }
                }
            }

            accumulator -= TargetElapsedTime;
        }
    }

    public static void Initialize(Scene.Scene scene)
    {
        ResetFixedUpdateTiming();
        ClearMethodCaches();
    }

    public static void ExecuteFrame(Scene.Scene scene)
    {
        ExecuteUpdateLoop(scene);
        ExecuteFixedUpdateStep(scene);
    }

    public static void ClearMethodCaches()
    {
        UpdateMethodCache.Clear();
        FixedUpdateMethodCache.Clear();
    }

    public static void ResetFixedUpdateTiming()
    {
        accumulator = TimeSpan.Zero;
        previousTime = DateTime.Now;
    }
}