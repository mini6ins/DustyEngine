using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Scene;
using GraphicsEngine;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace GraphicsEngineOpenGL;

public class GraphicsEngineOpenGl : IRenderer
{
    private GameWindow? _window;

    public void RunMainLoop(Scene scene, Action updateCallback, Vector2i resolution, string programName,
        string vertShaderPath, string fragShaderPath, bool vsync, RenderMode renderMode)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(resolution.X, resolution.Y),
            Title = programName,
        };

        if (renderMode == RenderMode.Editor)
        {
            _window = new EditorWindow(GameWindowSettings.Default, nativeWindowSettings,
                vertShaderPath, fragShaderPath, programName, vsync);
        }
        else
        {
            _window = new Window(GameWindowSettings.Default, nativeWindowSettings,
                vertShaderPath, fragShaderPath, programName, vsync, CursorState.Normal, renderMode);
        }

        _window.UpdateFrame += _ => { updateCallback?.Invoke(); };
        _window.Run();
    }

    public void AddRenderer(MeshRenderer meshRenderer)
    {
        if (_window is EditorWindow editorWindow)
            editorWindow.GraphicsRenderer.AddRenderer(meshRenderer);
        else if (_window is Window window)
            window.GraphicsRenderer.AddRenderer(meshRenderer);
    }

    public bool RemoveRenderer(int objectId)
    {
        return false;
    }
}
