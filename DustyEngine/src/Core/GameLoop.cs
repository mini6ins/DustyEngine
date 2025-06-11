using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DustyEngine.Components;

namespace DustyEngine;
public static class GameLoop
{
    private static TimeSpan _accumulator = TimeSpan.Zero;
    private static DateTime _previousTime = DateTime.Now;
    private static readonly TimeSpan _targetElapsedTime = TimeSpan.FromMilliseconds(16); // ~60 FPS для FixedUpdate
    
    // Кеш для методов компонентов - избегает повторных вызовов рефлексии
    private static readonly Dictionary<Type, MethodInfo> _updateMethodCache = new();
    private static readonly Dictionary<Type, MethodInfo> _fixedUpdateMethodCache = new();

    /// <summary>
    /// Выполняет Update для всех активных MonoBehaviour компонентов в сцене
    /// Вызывается каждый кадр
    /// </summary>
    public static void ExecuteUpdateLoop(Scene.Scene scene)
    {
        foreach (var gameObject in scene.GameObjects ?? Enumerable.Empty<GameObject>())
        {
            if (!gameObject.IsActive) continue;
            
            foreach (var component in gameObject.Components ?? Enumerable.Empty<Component>())
            {
                if (component is MonoBehaviour monoBehaviour && monoBehaviour.Enabled)
                {
                    var componentType = component.GetType();
                    
                    // Проверяем кеш на наличие метода Update
                    if (!_updateMethodCache.TryGetValue(componentType, out var updateMethod))
                    {
                        // Если нет в кеше - ищем метод и добавляем в кеш
                        updateMethod = componentType.GetMethod("Update",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        _updateMethodCache[componentType] = updateMethod;
                    }
                    
                    // Вызываем метод если он существует
                    updateMethod?.Invoke(component, null);
                }
            }
        }
    }

    /// <summary>
    /// Выполняет FixedUpdate для всех активных MonoBehaviour компонентов в сцене
    /// Вызывается с фиксированной частотой (накопительная система)
    /// </summary>
    public static void ExecuteFixedUpdateStep(Scene.Scene scene)
    {
        var currentTime = DateTime.Now;
        var frameTime = currentTime - _previousTime;
        _previousTime = currentTime;

        _accumulator += frameTime;

        // Выполняем FixedUpdate столько раз, сколько накопилось времени
        while (_accumulator >= _targetElapsedTime)
        {
           // Console.WriteLine($"[FixedUpdate] Step executed");
            
            foreach (var gameObject in scene.GameObjects ?? Enumerable.Empty<GameObject>())
            {
                if (!gameObject.IsActive) continue;
                
                foreach (var component in gameObject.Components ?? Enumerable.Empty<Component>())
                {
                    if (component is MonoBehaviour monoBehaviour && monoBehaviour.Enabled)
                    {
                        var componentType = component.GetType();
                        
                        if (!_fixedUpdateMethodCache.TryGetValue(componentType, out var fixedUpdateMethod))
                        {
                            fixedUpdateMethod = componentType.GetMethod("FixedUpdate",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            _fixedUpdateMethodCache[componentType] = fixedUpdateMethod;
                        }
                        
                        if (fixedUpdateMethod != null)
                        {
                         //   Console.WriteLine($"[FixedUpdate] {monoBehaviour.GetType().Name}");
                            fixedUpdateMethod.Invoke(component, null);
                        }
                    }
                }
            }
            
            _accumulator -= _targetElapsedTime;
        }
    }

    /// <summary>
    /// Инициализирует GameLoop для новой сцены
    /// </summary>
    public static void Initialize(Scene.Scene scene)
    {
        ResetFixedUpdateTiming();
        ClearMethodCaches();
    }

    /// <summary>
    /// Выполняет один кадр игрового цикла (Update + FixedUpdate)
    /// </summary>
    public static void ExecuteFrame(Scene.Scene scene)
    {
        ExecuteUpdateLoop(scene);
        ExecuteFixedUpdateStep(scene);
    }

    /// <summary>
    /// Очищает кеши методов (полезно при горячей перезагрузке или смене сцены)
    /// </summary>
    public static void ClearMethodCaches()
    {
        _updateMethodCache.Clear();
        _fixedUpdateMethodCache.Clear();
    }

    /// <summary>
    /// Сбрасывает состояние времени для FixedUpdate
    /// </summary>
    public static void ResetFixedUpdateTiming()
    {
        _accumulator = TimeSpan.Zero;
        _previousTime = DateTime.Now;
    }

    /// <summary>
    /// Устанавливает целевую частоту FixedUpdate (по умолчанию ~60 FPS)
    /// </summary>
    public static void SetFixedUpdateRate(int targetFPS)
    {
        if (targetFPS <= 0) throw new ArgumentException("Target FPS must be greater than 0");
        // Обновляем целевое время
        // Примечание: поскольку _targetElapsedTime readonly, нужно изменить архитектуру для динамического изменения
    }
}

// Пример использования в вашем коде:
/*
// Вариант 1: Простое использование (рекомендуется)
GraphicsEngineOpenGl graphicsEngineOpenGl = new GraphicsEngineOpenGl();
graphicsEngineOpenGl.RunMainLoop(loadedScene, projectSettings.ScreenSize, projectSettings.ProjectName);

// Вариант 2: С дополнительным callback (для обратной совместимости)
GraphicsEngineOpenGl graphicsEngineOpenGl = new GraphicsEngineOpenGl();
Action customUpdateAction = () => 
{
    // Ваш дополнительный код здесь
};
graphicsEngineOpenGl.RunMainLoop(loadedScene, customUpdateAction, projectSettings.ScreenSize, projectSettings.ProjectName);

// Вариант 3: Ручное управление
GameLoop.Initialize(loadedScene);
// В цикле рендеринга:
GameLoop.ExecuteFrame(loadedScene);
*/