using DustyEngine;
using DustyEngine.Scene;
using Editor;
using GraphicsEngine;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SceneSystem.Scene;
using Vector2i = OpenTK.Mathematics.Vector2i;

namespace WindowEngine;

public class Window : IDisposable
{
    private readonly GameWindow _window;
    private readonly GraphicsRenderer _renderer;

    public GraphicsRenderer Renderer => _renderer;
    public RenderMode RenderMode { get; private set; }
    
    private readonly Action<Scene> _sceneChangedHandler;

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

        _sceneChangedHandler = scene => _renderer.LoadScene(scene);

        var currentScene = SceneManager.CurrentScene;

        SceneManager.OnSceneChanged += _sceneChangedHandler;

        if (currentScene != null)
        {
            _renderer.LoadScene(currentScene);
        }

        var cursorState = renderMode == RenderMode.EditorStop
            ? CursorState.Normal
            : CursorState.Grabbed;

        _window = renderMode == RenderMode.EditorStop
            ? new EditorWindow(GameWindowSettings.Default, nativeWindowSettings, vsync, _renderer, projectPath)
            : new StandaloneWindow(GameWindowSettings.Default, nativeWindowSettings, vsync, cursorState, _renderer);

        _window.UpdateFrame += _ => updateCallback(RenderMode);
    }

    public void Run() => _window.Run();

    public void ChangePlayMode(RenderMode renderMode)
    {
        RenderMode = renderMode;
        _renderer.SetRenderMode(renderMode);
        Debug.Log($"Play mode changed to: {RenderMode}", Debug.LogLevel.Info, true);
    }

    public void Dispose()
    {
        SceneManager.OnSceneChanged -= _sceneChangedHandler;
        _window.Dispose();
    }
}