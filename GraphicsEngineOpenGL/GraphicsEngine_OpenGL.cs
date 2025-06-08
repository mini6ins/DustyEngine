using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl
{
    public void RunMainLoop(Scene scene, Action updateCallback, Vector2 resolution,
        string programName)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        var nativeWindowSettings = new NativeWindowSettings()
        {
            Size = new Vector2i((int)resolution.X, (int)resolution.Y),
            Title = programName,
        };

        List<MeshRenderer> allRenderers = new();
        foreach (var obj in scene.GameObjects)
        {
            CollectMeshRenderers(obj, allRenderers);
        }

        
        Debug.Log($"Total Meshes: {allRenderers.Count}", Debug.LogLevel.Info, true);

        
        using var window = new Window(GameWindowSettings.Default, nativeWindowSettings, allRenderers, programName);

        window.UpdateFrame += (e) => { updateCallback?.Invoke(); };
        window.Run();
    }
    
    
    private static void CollectMeshRenderers(GameObject obj, List<MeshRenderer> renderers)
    {
        // Добавляем все компоненты типа MeshRenderer
        foreach (var component in obj.Components)
        {
            if (component is MeshRenderer meshRenderer)
            {
                renderers.Add(meshRenderer);
            }
        }

        // Рекурсивно обрабатываем детей
        foreach (var child in obj.Children)
        {
            CollectMeshRenderers(child, renderers);
        }
    }


}