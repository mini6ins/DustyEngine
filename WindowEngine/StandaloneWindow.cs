using GraphicsEngine;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace WindowEngine;

public class StandaloneWindow : GameWindow
{
    private readonly GraphicsRenderer _renderer;

    public StandaloneWindow(
        GameWindowSettings gameWindowSettings,
        NativeWindowSettings nativeWindowSettings,
        bool vsync,
        CursorState cursorState,
        GraphicsRenderer renderer) : base(gameWindowSettings, nativeWindowSettings)
    {
        _renderer = renderer;
        Title = nativeWindowSettings.Title;
        VSync = vsync ? VSyncMode.On : VSyncMode.Off;
        CursorState = cursorState;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        _renderer.Load();
        _renderer.ResizeViewport(FramebufferSize.X, FramebufferSize.Y);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _renderer.ResizeViewport(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        _renderer.Update((float)args.Time, KeyboardState, MouseState);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        _renderer.OnMouseMove(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _renderer.Render();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        _renderer.PresentToScreen(FramebufferSize.X, FramebufferSize.Y);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        _renderer.Dispose();
        base.OnUnload();
    }
}
