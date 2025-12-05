using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;


namespace GraphicsEngineOpenGL;

public class Window : GameWindow
{
    private readonly RpcController? _rpcManager;
    public readonly GraphicsRenderer GraphicsRenderer;

    public Window(GameWindowSettings gws, NativeWindowSettings nws,
        string vertShaderPath, string fragShaderPath, string windowName, bool isVsync = true,
        CursorState cursorState = CursorState.Normal, RenderMode renderMode = RenderMode.Editor)
        : base(gws, nws)
    {
        Title = windowName;
        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;
        CursorState = cursorState;

        GraphicsRenderer =
            new GraphicsRenderer(vertShaderPath, fragShaderPath, nws.ClientSize.X, nws.ClientSize.Y, renderMode);

        if (renderMode == RenderMode.Editor)
            _rpcManager = new RpcController(nws.ClientSize.X, nws.ClientSize.Y);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GraphicsRenderer.Load();
        _rpcManager?.Start();
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

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GraphicsRenderer.Render();

        GraphicsRenderer.CaptureFrameIfNeeded(_rpcManager, FramebufferSize.X, FramebufferSize.Y);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        _rpcManager?.Dispose();
        GraphicsRenderer.Dispose();

        base.OnUnload();
    }
}
