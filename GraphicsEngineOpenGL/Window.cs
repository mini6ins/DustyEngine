using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class Window : GameWindow
{
    public readonly GraphicsRenderer GraphicsRenderer;
    private readonly RenderMode _renderMode;

    public Window(
        GameWindowSettings gws,
        NativeWindowSettings nws,
        string vertShaderPath,
        string fragShaderPath,
        string windowName,
        bool isVsync = true,
        CursorState cursorState = CursorState.Normal,
        RenderMode renderMode = RenderMode.Editor)
        : base(gws, nws)
    {
        Title = windowName;
        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;
        CursorState = cursorState;
        _renderMode = renderMode;

        GraphicsRenderer =
            new GraphicsRenderer(vertShaderPath, fragShaderPath, nws.ClientSize.X, nws.ClientSize.Y, renderMode);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GraphicsRenderer.Load();

        // на всякий — под размер окна
        GraphicsRenderer.ResizeViewport(FramebufferSize.X, FramebufferSize.Y);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

        // в Standalone надо ресайзить FBO под окно
        if (_renderMode == RenderMode.Standalone)
            GraphicsRenderer.ResizeViewport(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        GraphicsRenderer.Update((float)args.Time, KeyboardState, MouseState);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        GraphicsRenderer.OnMouseMove(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // 1) Рендер всегда -> FBO (текстуру)
        GraphicsRenderer.Render();

        // 2) В Standalone выводим FBO на экран
        if (_renderMode == RenderMode.Standalone)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
            GraphicsRenderer.PresentToScreen(FramebufferSize.X, FramebufferSize.Y);
        }

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        GraphicsRenderer.Dispose();
        base.OnUnload();
    }
}
