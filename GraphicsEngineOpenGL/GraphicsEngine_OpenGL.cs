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

    public void RunMainLoop(
        Scene scene,
        Action updateCallback,
        Vector2i resolution,
        string programName,
        string vertShaderPath,
        string fragShaderPath,
        bool vsync,
        RenderMode renderMode)
    {
        Debug.Log("GraphicsEngineOpenGl is working", Debug.LogLevel.Info, true);

        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(resolution.X, resolution.Y),
            Title = programName,
        };

        // 1) создаём ОДИН renderer тут
        _renderer = new GraphicsRenderer(
            vertShaderPath,
            fragShaderPath,
            nativeWindowSettings.ClientSize.X,
            nativeWindowSettings.ClientSize.Y,
            renderMode);

        // 2) создаём нужное окно и ПЕРЕДАЁМ renderer
        _window = renderMode == RenderMode.Editor
            ? new EditorWindow(GameWindowSettings.Default, nativeWindowSettings, programName, vsync, _renderer)
            : new StandaloneWindow(GameWindowSettings.Default, nativeWindowSettings, programName, vsync, CursorState.Normal, renderMode, _renderer);

        // внешний апдейт твоего движка
        _window.UpdateFrame += _ => updateCallback?.Invoke();

        _window.Run();
    }

    public void AddRenderer(MeshRenderer meshRenderer)
    {
        _renderer?.AddRenderer(meshRenderer);
    }

    public bool RemoveRenderer(int objectId)
    {
        return _renderer != null && _renderer.RemoveRenderer(objectId);
    }
}
