using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class StandaloneWindow : GameWindow
{
    private GraphicsRenderer? GraphicsRenderer { get; }


    public StandaloneWindow(WindowSettings windowSettings) : base(windowSettings.GameWindowSettings,
        windowSettings.NativeWindowSettings)
    {
        Title = windowSettings.NativeWindowSettings.Title;
        VSync = windowSettings.VSync ? VSyncMode.On : VSyncMode.Off;
        CursorState = windowSettings.CursorState;

        GraphicsRenderer = GraphicsEngineOpenGl.Renderer;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GraphicsRenderer?.Load();
        GraphicsRenderer?.ResizeViewport(FramebufferSize.X, FramebufferSize.Y);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);

        if (GraphicsEngineOpenGl.RenderMode == RenderMode.Standalone)
            GraphicsRenderer?.ResizeViewport(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        GraphicsRenderer?.Update((float)args.Time, KeyboardState, MouseState);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        GraphicsRenderer?.OnMouseMove(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GraphicsRenderer?.Render();

        if (GraphicsEngineOpenGl.RenderMode == RenderMode.Standalone)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
            GraphicsRenderer?.PresentToScreen(FramebufferSize.X, FramebufferSize.Y);
        }

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        GraphicsRenderer?.Dispose();
        base.OnUnload();
    }
}
