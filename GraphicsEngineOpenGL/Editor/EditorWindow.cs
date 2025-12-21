using GraphicsEngineOpenGL.Editor.Panels.ViewPortPanel;
using ImGui_OpenTK.Backends;
using ImGuiNET;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL.Editor;

public class EditorWindow : GameWindow
{
    private readonly GraphicsRenderer? _graphicsRenderer;

    public EditorWindow(WindowSettings windowSettings) : base(windowSettings.GameWindowSettings,
        windowSettings.NativeWindowSettings)
    {
        Title = windowSettings.NativeWindowSettings.Title;
        VSync = windowSettings.VSync ? VSyncMode.On : VSyncMode.Off;
        CursorState = CursorState.Normal;

        _graphicsRenderer = GraphicsEngineOpenGl.Renderer;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        EditorImGuiHelper.ImGuiInit(this);

        _graphicsRenderer?.Load();
        _graphicsRenderer?.ResizeViewport(FramebufferSize.X, FramebufferSize.Y);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (Input.Input.IsRpcInputActive && ViewportPanel.IsScenePanelActive)
        {
            EditorInputHandler.UpdateKeyboardInput(KeyboardState);
            EditorInputHandler.UpdateMouseInput(MouseState);
        }

        _graphicsRenderer?.Update((float)args.Time, KeyboardState, MouseState);
    }


    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _graphicsRenderer?.Render();
        EditorImGuiHelper.ImGuiRender(this);
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        _graphicsRenderer?.Dispose();

        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();
        ImGui.DestroyContext();

        base.OnUnload();
    }
}
