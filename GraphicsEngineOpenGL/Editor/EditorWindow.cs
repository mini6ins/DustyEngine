using System.Numerics;
using ImGuiNET;
using ImGui_OpenTK.Backends;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class EditorWindow : GameWindow
{
    private GraphicsRenderer GraphicsRenderer { get; }

    public EditorWindow(WindowSettings windowSettings) : base(windowSettings.GameWindowSettings, windowSettings.NativeWindowSettings)
    {
        Title = windowSettings.NativeWindowSettings.Title;
        VSync = windowSettings.VSync? VSyncMode.On : VSyncMode.Off;
        CursorState = CursorState.Normal;

        GraphicsRenderer = windowSettings.Renderer;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        ImGui.CreateContext();
        ImguiImplOpenTK4.Init(this);
        ImguiImplOpenGL3.Init();

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        GraphicsRenderer.Load();
        GraphicsRenderer.ResizeViewport(FramebufferSize.X, FramebufferSize.Y);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (Input.Input.IsRpcInputActive)
        {
            UpdateKeyboardInput();
            UpdateMouseInput();
        }

        GraphicsRenderer.Update((float)args.Time, KeyboardState, MouseState);
    }

    private void UpdateKeyboardInput()
    {
        var keys = new[]
        {
            (OpenTK.Windowing.GraphicsLibraryFramework.Keys.W, "W"),
            (OpenTK.Windowing.GraphicsLibraryFramework.Keys.A, "A"),
            (OpenTK.Windowing.GraphicsLibraryFramework.Keys.S, "S"),
            (OpenTK.Windowing.GraphicsLibraryFramework.Keys.D, "D"),
            (OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space, "SPACE"),
            (OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift, "LEFTSHIFT"),
            (OpenTK.Windowing.GraphicsLibraryFramework.Keys.F1, "F1"),
            (OpenTK.Windowing.GraphicsLibraryFramework.Keys.F2, "F2"),
        };

        foreach (var (key, name) in keys)
        {
            if (KeyboardState.IsKeyDown(key))
                Input.Input.RpcKeyDown(name);
            else
                Input.Input.RpcKeyUp(name);
        }
    }

    private float _lastMouseX;
    private float _lastMouseY;
    private bool _firstMouseMove = true;

    private void UpdateMouseInput()
    {
        var mousePos = MouseState.Position;

        if (_firstMouseMove)
        {
            _lastMouseX = mousePos.X;
            _lastMouseY = mousePos.Y;
            _firstMouseMove = false;
            return;
        }

        float deltaX = mousePos.X - _lastMouseX;
        float deltaY = mousePos.Y - _lastMouseY;

        if (System.Math.Abs(deltaX) > 0.001f || System.Math.Abs(deltaY) > 0.001f)
            Input.Input.RpcMouseMove(deltaX, deltaY);

        _lastMouseX = mousePos.X;
        _lastMouseY = mousePos.Y;

        if (MouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle))
            Input.Input.RpcMouseDown(Utils.MouseButton.Middle);
        else
            Input.Input.RpcMouseUp(Utils.MouseButton.Middle);

        if (MouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
            Input.Input.RpcMouseDown(Utils.MouseButton.Left);
        else
            Input.Input.RpcMouseUp(Utils.MouseButton.Left);

        if (MouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right))
            Input.Input.RpcMouseDown(Utils.MouseButton.Right);
        else
            Input.Input.RpcMouseUp(Utils.MouseButton.Right);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GraphicsRenderer.Render();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();

        var viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowViewport(viewport.ID);


        ImGuiWindowFlags windowFlags =
            ImGuiWindowFlags.MenuBar |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus;


        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

        ImGui.Begin("DockSpace", windowFlags);
        ImGui.PopStyleVar(3);

        var dockspaceId = ImGui.GetID("MainDockSpace");
        ImGui.DockSpace(dockspaceId, new Vector2(0, 0), ImGuiDockNodeFlags.None);

        RenderEditorUI();

        ImGui.End();

        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

        SwapBuffers();
    }

    private void RenderEditorUI()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Exit")) Close();
                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }

        ImGui.Begin("Scene Viewport");

        var size = ImGui.GetContentRegionAvail();
        if (size.X > 0 && size.Y > 0)
        {
            GraphicsRenderer.ResizeViewport((int)size.X, (int)size.Y);

            ImGui.Image(
                GraphicsRenderer.ViewportTexture,
                size,
                new Vector2(0, 1),
                new Vector2(1, 0));
        }

        ImGui.End();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        GraphicsRenderer.Dispose();

        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();
        ImGui.DestroyContext();

        base.OnUnload();
    }
}
