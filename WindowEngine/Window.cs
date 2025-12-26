using DustyEngine;
using Editor;
using GraphicsEngine;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace WindowEngine;

public class Window : IDisposable
{
    private GameWindow? _window;
    private GraphicsRenderer? _renderer;

    public GraphicsRenderer? Renderer => _renderer;
    public RenderMode RenderMode { get; private set; }
    public Action? LoadScene;

    public Window(
        Action<RenderMode> updateCallback,
        Vector2i resolution,
        string programTitle,
        string vertShaderPath,
        string fragShaderPath,
        bool vsync,
        RenderMode renderMode,
        string projectPath)
    {
        Debug.Log("WindowEngine is working", Debug.LogLevel.Info, true);

        RenderMode = renderMode;

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = resolution,
            Title = programTitle
        };

        _renderer = new GraphicsRenderer(
            vertShaderPath,
            fragShaderPath,
            nativeWindowSettings.ClientSize.X,
            nativeWindowSettings.ClientSize.Y,
            renderMode);

        LoadScene += _renderer.LoadScene;

        var cursorState = renderMode == RenderMode.EditorStop
            ? CursorState.Normal
            : CursorState.Hidden;

        _window = renderMode == RenderMode.EditorStop
            ? new EditorWindow(GameWindowSettings.Default, nativeWindowSettings, vsync, _renderer, projectPath)
            : new StandaloneWindow(GameWindowSettings.Default, nativeWindowSettings, vsync, cursorState, _renderer);

        _window.UpdateFrame += _ => updateCallback.Invoke(RenderMode);
    }

    public void Run() => _window?.Run();

    public void ChangePlayMode(RenderMode renderMode)
    {
        RenderMode = renderMode;
        _renderer?.SetRenderMode(renderMode);
        Debug.Log($"Play mode changed to: {RenderMode}", Debug.LogLevel.Info, true);
    }

    public void Dispose()
    {
        if (_renderer != null)
        {
            LoadScene -= _renderer.LoadScene;
        }
        _window?.Dispose();
    }
}
