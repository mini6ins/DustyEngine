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
    private GraphicsRenderer? _renderer;

    public static RenderMode RenderMode;

    public void RunMainLoop(
        Scene scene,
        Action updateCallback,
        Vector2i resolution,
        string programTitle,
        string vertShaderPath,
        string fragShaderPath,
        bool vsync,
        RenderMode renderMode)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        RenderMode = renderMode;
        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(resolution.X, resolution.Y),
            Title = programTitle,
        };

        _renderer = new GraphicsRenderer(vertShaderPath, fragShaderPath, nativeWindowSettings.ClientSize.X,
            nativeWindowSettings.ClientSize.Y);


        var cursorState = RenderMode == RenderMode.Editor ? CursorState.Normal : CursorState.Hidden;
        var windowSettings = new WindowSettings(GameWindowSettings.Default,  nativeWindowSettings, vsync, cursorState, _renderer );

        _window = RenderMode == RenderMode.Editor ? new EditorWindow(windowSettings) : new StandaloneWindow(windowSettings);

        _window.UpdateFrame += _ => updateCallback.Invoke();
        _window.Run();
    }

    public void AddRenderer(MeshRenderer meshRenderer) => _renderer?.AddRenderer(meshRenderer);
    public bool RemoveRenderer(int objectId) => _renderer != null && _renderer.RemoveRenderer(objectId);
}


public enum RenderMode
{
    Standalone,
    Editor
}


public class WindowSettings(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, bool vSync, CursorState cursorState, GraphicsRenderer renderer)
{
    public readonly GameWindowSettings GameWindowSettings = gameWindowSettings;
    public readonly NativeWindowSettings NativeWindowSettings = nativeWindowSettings;
    public bool VSync = vSync;
    public CursorState CursorState = cursorState;
    public readonly GraphicsRenderer Renderer = renderer;
}
