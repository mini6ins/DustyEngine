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

    private static void ExecuteUpdateLoop(Scene.Scene scene)
    {
        foreach (var gameObject in scene.GameObjects ?? Enumerable.Empty<GameObject>())
        {
            if (!gameObject.IsActive) continue;

            foreach (var component in gameObject.Components ?? Enumerable.Empty<Component>())
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

                    updateMethod?.Invoke(component, null);
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
            foreach (var gameObject in scene.GameObjects ?? Enumerable.Empty<GameObject>())
            {
                if (!gameObject.IsActive) continue;

                foreach (var component in gameObject.Components ?? Enumerable.Empty<Component>())
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

                        if (fixedUpdateMethod != null)
                        {
                            fixedUpdateMethod.Invoke(component, null);
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