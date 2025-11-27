using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl : IRenderer
{
    private Window? _window;

    public void RunMainLoop(Scene scene, Action updateCallback, Vector2i resolution, string programName,
        string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(resolution.X, resolution.Y),
            Title = programName,
        };


      


        _window = new Window(GameWindowSettings.Default, nativeWindowSettings, scene, vertShaderPath, fragShaderPath,
            programName, vsync, CursorState.Normal, renderMode);

        
        _window.IsVisible = false;
        
        _window.UpdateFrame += _ => { updateCallback?.Invoke(); };
        _window.Run();
    }


    public void AddRenderer(MeshRenderer meshRenderer) => _window?.AddRenderer(meshRenderer);
    public bool RemoveRenderer(int objectId) => _window?.RemoveRenderer(objectId) ?? false;
}