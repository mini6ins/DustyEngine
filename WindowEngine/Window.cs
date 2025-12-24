using DustyEngine;
using Editor;
using GraphicsEngine;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace WindowEngine;

public enum RenderMode
{
    Standalone,
    Editor
}


public class Window : IDisposable
{
    private GameWindow? _window;

    public GraphicsRenderer? Renderer => _renderer;
    public static GraphicsRenderer? _renderer;

    private static RenderMode RenderMode;

    public Action? LoadScene;

    public void RunMainLoop(Action updateCallback, Vector2i resolution, string programTitle, string vertShaderPath,
        string fragShaderPath, bool vsync, RenderMode renderMode, string projectPath)
    {
        Debug.Log("WindowEngine  is working", Debug.LogLevel.Info, true);

        RenderMode = renderMode;
        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(resolution.X, resolution.Y),
            Title = programTitle
        };

        _renderer = new GraphicsRenderer(vertShaderPath, fragShaderPath, nativeWindowSettings.ClientSize.X, nativeWindowSettings.ClientSize.Y, RenderMode == RenderMode.Editor);
        LoadScene += _renderer.LoadScene;
        var cursorState = RenderMode == RenderMode.Editor ? CursorState.Normal : CursorState.Hidden;

        _window = RenderMode == RenderMode.Editor
            ? new EditorWindow(GameWindowSettings.Default, nativeWindowSettings, vsync, _renderer, projectPath)
            : new StandaloneWindow(GameWindowSettings.Default, nativeWindowSettings, vsync, cursorState);

        if (renderMode == RenderMode.Standalone)
            _window.UpdateFrame += _ => updateCallback.Invoke();
        _window.Run();
    }

    public void Dispose()
    {
        _window?.Dispose();
        LoadScene -= _renderer!.LoadScene;
    }
}
