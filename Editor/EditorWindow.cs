using Editor.ImGuI;
using Editor.ImGuI.Impl;
using Editor.Panels.ViewPortPanel;
using GraphicsEngine;
using ImGuiNET;
using InputSystem;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Editor;

public class EditorWindow : GameWindow
{
    public static GraphicsRenderer GraphicsRenderer { get; private set; } = null!;
    public static string ProjectPath { get; private set; } = null!;

    public EditorWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, bool vsync, GraphicsRenderer graphicsRenderer, string projectPath) : base(gameWindowSettings,
        nativeWindowSettings)
    {
        Title = nativeWindowSettings.Title;
        VSync = vsync ? VSyncMode.On : VSyncMode.Off;
        CursorState = CursorState.Normal;

        GraphicsRenderer = graphicsRenderer;
        ProjectPath = projectPath;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        EditorImGuiHelper.ImGuiInit(this);

        GraphicsRenderer?.Load();
        GraphicsRenderer?.ResizeViewport(FramebufferSize.X, FramebufferSize.Y);

    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (Input.IsRpcInputActive && ViewportPanel.IsScenePanelActive)
        {
            EditorInputHandler.UpdateKeyboardInput(KeyboardState);
            EditorInputHandler.UpdateMouseInput(MouseState);
        }

        GraphicsRenderer?.Update((float)args.Time, KeyboardState, MouseState);
    }


    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GraphicsRenderer?.Render();
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
        GraphicsRenderer?.Dispose();

        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();
        ImGui.DestroyContext();
        base.OnUnload();
    }
}
