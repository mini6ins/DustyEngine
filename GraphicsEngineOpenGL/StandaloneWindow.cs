using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class StandaloneWindow : GameWindow
{
    private GraphicsRenderer GraphicsRenderer { get; }
    private readonly RenderMode _renderMode;

    public StandaloneWindow(GameWindowSettings gws, NativeWindowSettings nws, string windowName, bool isVsync,
        CursorState cursorState, RenderMode renderMode, GraphicsRenderer graphicsRenderer) : base(gws, nws)
    {
        Title = windowName;
        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;
        CursorState = cursorState;

        _renderMode = renderMode;
        GraphicsRenderer = graphicsRenderer;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GraphicsRenderer.Load();
        GraphicsRenderer.ResizeViewport(FramebufferSize.X, FramebufferSize.Y);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

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

        GraphicsRenderer.Render();

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
