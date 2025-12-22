using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace WindowEngine;

public class StandaloneWindow : GameWindow
{
    public StandaloneWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, bool vsync, CursorState cursorState) : base(gameWindowSettings, nativeWindowSettings)
    {
        Title = nativeWindowSettings.Title;
        VSync = vsync ? VSyncMode.On : VSyncMode.Off;
        CursorState = cursorState;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        Window._renderer?.Load();
        Window._renderer?.ResizeViewport(FramebufferSize.X, FramebufferSize.Y);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);

        if (Window.RenderMode == RenderMode.Standalone)
            Window._renderer?.ResizeViewport(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        Window._renderer?.Update((float)args.Time, KeyboardState, MouseState);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        Window._renderer?.OnMouseMove(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        Window._renderer?.Render();

        if (Window.RenderMode == RenderMode.Standalone)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
            Window._renderer?.PresentToScreen(FramebufferSize.X, FramebufferSize.Y);
        }

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        Window._renderer?.Dispose();
        base.OnUnload();
    }
}
