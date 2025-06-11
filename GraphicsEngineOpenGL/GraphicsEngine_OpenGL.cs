using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl
{


    // Основной метод с Action callback
    public void RunMainLoop(Scene scene, Action updateCallback, Vector2 resolution, string programName)
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

        using var window = new Window(GameWindowSettings.Default, nativeWindowSettings, allRenderers, programName, FindCamera(scene),
            true, CursorState.Grabbed);

        window.UpdateFrame += (e) => 
        { 
            // Выполняем переданный callback
            updateCallback?.Invoke();
        };
        
        
        window.Run();
    }

    private static void CollectMeshRenderers(GameObject obj, List<MeshRenderer> renderers)
    {
        foreach (var component in obj.Components)
        {
            if (component is MeshRenderer meshRenderer)
            {
                renderers.Add(meshRenderer);
            }
        }

        foreach (var child in obj.Children)
        {
            CollectMeshRenderers(child, renderers);
        }
    }
    
    private static Camera? FindCamera(Scene scene)
    {
        foreach (var obj in scene.GameObjects)
        {
            var camera = FindCameraRecursive(obj);
            if (camera != null)
                return camera;
        }
        return null;
    }

    private static Camera? FindCameraRecursive(GameObject obj)
    {
        foreach (var component in obj.Components)
        {
            if (component is Camera camera)
                return camera;
        }

        foreach (var child in obj.Children)
        {
            var result = FindCameraRecursive(child);
            if (result != null)
                return result;
        }

        return null;
    }
}