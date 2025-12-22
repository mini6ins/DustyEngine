using DustyEngine;
using GraphicsEngine;
using GraphicsEngineOpenGL.Editor;
using GraphicsEngineOpenGL.Editor.Panels.ViewPortPanel;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Vector2i = OpenTK.Mathematics.Vector2i;


namespace WindowEngine;

public class Window : IRenderer, IDisposable
{
    private GameWindow? _window;
    public GraphicsRenderer? Renderer => _renderer;
    public static GraphicsRenderer? _renderer;

    public static RenderMode RenderMode;
    public static string ProjectPath;

    public void RunMainLoop(
        Action updateCallback,
        Vector2i resolution,
        string programTitle,
        string vertShaderPath,
        string fragShaderPath,
        bool vsync,
        RenderMode renderMode, string _projectPath)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);
        ProjectPath = _projectPath;
        RenderMode = renderMode;
        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(resolution.X, resolution.Y),
            Title = programTitle,
        };

        _renderer = new GraphicsRenderer(vertShaderPath, fragShaderPath, nativeWindowSettings.ClientSize.X,
            nativeWindowSettings.ClientSize.Y, RenderMode == RenderMode.Editor);


        var cursorState = RenderMode == RenderMode.Editor ? CursorState.Normal : CursorState.Hidden;
        var windowSettings = new WindowSettings(GameWindowSettings.Default, nativeWindowSettings, vsync, cursorState);

        _window = RenderMode == RenderMode.Editor ? new EditorWindow(GameWindowSettings.Default, nativeWindowSettings, vsync, _renderer, ProjectPath) : new StandaloneWindow(windowSettings);

        ViewportPanel.OnPlayModeChanged += ChangePlayMode;
        _window.UpdateFrame += _ => updateCallback.Invoke();
        _window.Run();
    }


    private static void ChangePlayMode(bool isPlayMode)
    {
        Debug.Log("Is play mode: " + isPlayMode, Debug.LogLevel.Info, true);
    }

    public void Dispose() => ViewportPanel.OnPlayModeChanged -= ChangePlayMode;
}

public enum RenderMode
{
    Standalone,
    Editor
}

public class WindowSettings(
    GameWindowSettings gameWindowSettings,
    NativeWindowSettings nativeWindowSettings,
    bool vSync,
    CursorState cursorState)
{
    public readonly GameWindowSettings GameWindowSettings = gameWindowSettings;
    public readonly NativeWindowSettings NativeWindowSettings = nativeWindowSettings;
    public bool VSync = vSync;
    public CursorState CursorState = cursorState;
}
