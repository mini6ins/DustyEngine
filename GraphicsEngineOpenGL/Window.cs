using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class Window : GameWindow
{
    private bool _initialized;

    private readonly RpcController? _rpcManager;

    public readonly GraphicsRenderer GraphicsRenderer;

    private bool IsEditorMode => GraphicsRenderer?.IsEditorMode ?? false;

    public Window(GameWindowSettings gws, NativeWindowSettings nws,
        string vertShaderPath, string fragShaderPath, string windowName, bool isVsync = true,
        CursorState cursorState = CursorState.Normal, RenderMode renderMode = RenderMode.Editor)
        : base(gws, nws)
    {
        Title = windowName;
        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;
        CursorState = cursorState;
        
        GraphicsRenderer = new GraphicsRenderer(vertShaderPath, fragShaderPath, nws.ClientSize.X, nws.ClientSize.Y, renderMode);

        if (renderMode == RenderMode.Editor)
            _rpcManager = new RpcController(nws.ClientSize.X, nws.ClientSize.Y);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GraphicsRenderer.Load();

        if (IsEditorMode)
        {
            GraphicsRenderer.InitializeEditorCamera();
            _rpcManager?.Start();
            Input.Input.EnableRpcInput();
        }

        _initialized = true;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (!IsEditorMode)
        {
            Input.Input.Update(KeyboardState);
            Input.Input.UpdateMouseState(MouseState);
        }
        else
        {
            GraphicsRenderer.UpdateEditorCamera((float)args.Time);
        }

        GraphicsRenderer.HandleDebugInput();
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (!IsEditorMode)
            Input.Input.UpdateMouse(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        GraphicsRenderer.Render();

        if (IsEditorMode && _rpcManager?.ConnectedClients > 0)
        {
            _rpcManager.CaptureFrame(FramebufferSize.X, FramebufferSize.Y);
        }

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        if (!_initialized) return;

        _rpcManager?.Dispose();

        if (IsEditorMode)
        {
            Input.Input.DisableRpcInput();
        }

        GraphicsRenderer.Dispose();

        _initialized = false;
        base.OnUnload();
    }
}