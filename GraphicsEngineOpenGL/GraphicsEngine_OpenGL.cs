using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl
{
    public void RunMainLoop(Scene scene, Action updateCallback, Vector2 resolution, string programName,
        string vertShaderPath, string fragShaderPath, bool vsync)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i((int)resolution.X, (int)resolution.Y),
            Title = programName,
        };

        List<MeshRenderer> allRenderers = new();
        foreach (var obj in scene.GameObjects)
        {
            SceneManager.CollectMeshRenderers(obj, allRenderers);
        }

        Debug.Log($"Total Meshes: {allRenderers.Count}", Debug.LogLevel.Info, true);

        using var window = new Window(GameWindowSettings.Default, nativeWindowSettings, allRenderers, vertShaderPath,
            fragShaderPath, programName, SceneManager.FindCamera(scene),
            vsync, CursorState.Grabbed);

        window.UpdateFrame += (e) => updateCallback?.Invoke();

        window.Run();
    }
}