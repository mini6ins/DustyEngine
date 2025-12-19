using ImGuiNET;
using ImGui_OpenTK.Backends;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class EditorWindow : GameWindow
{
    public readonly GraphicsRenderer GraphicsRenderer;

    public EditorWindow(
        GameWindowSettings gws,
        NativeWindowSettings nws,
        string vertShaderPath,
        string fragShaderPath,
        string windowName,
        bool isVsync = true)
        : base(gws, nws)
    {
        Title = windowName;
        VSync = isVsync ? VSyncMode.On : VSyncMode.Off;
        CursorState = CursorState.Normal;

        GraphicsRenderer = new GraphicsRenderer(
            vertShaderPath,
            fragShaderPath,
            nws.ClientSize.X,
            nws.ClientSize.Y,
            RenderMode.Editor);
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
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // RPC input (как было)
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

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        // В editor режиме обычно мышь в Input обновляется через RPC,
        // но твой GraphicsRenderer.OnMouseMove оставим как было.
        GraphicsRenderer.OnMouseMove(e.X, e.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // 1) РЕНДЕР СЦЕНЫ → FBO (текстура)
        GraphicsRenderer.Render();

        // 2) РЕНДЕР UI → default framebuffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
        windowFlags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse;
        windowFlags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));

        ImGui.Begin("DockSpace", windowFlags);
        ImGui.PopStyleVar(3);

        var dockspaceId = ImGui.GetID("MainDockSpace");
        ImGui.DockSpace(dockspaceId, new System.Numerics.Vector2(0.0f, 0.0f), ImGuiDockNodeFlags.None);

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
                if (ImGui.MenuItem("New Scene")) { }
                if (ImGui.MenuItem("Open Scene")) { }
                if (ImGui.MenuItem("Save Scene")) { }
                ImGui.Separator();
                if (ImGui.MenuItem("Exit")) Close();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo")) { }
                if (ImGui.MenuItem("Redo")) { }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Hierarchy")) { }
                if (ImGui.MenuItem("Inspector")) { }
                if (ImGui.MenuItem("Console")) { }
                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        // ВАЖНО: Scene viewport берёт ТЕКСТУРУ из GraphicsRenderer (FBO)
        GraphicsRenderer.RenderImGuiViewport();

        ImGui.Begin("Hierarchy");
        ImGui.Text("Scene Objects");
        ImGui.Separator();
        ImGui.End();

        ImGui.Begin("Inspector");
        ImGui.Text("Object Properties");
        ImGui.Separator();
        ImGui.End();

        ImGui.Begin("Console");
        ImGui.Text("Debug Console");
        ImGui.Separator();
        ImGui.End();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        // resize UI backbuffer
        GL.Viewport(0, 0, e.Width, e.Height);

        // сцену НЕ ресайзим тут, потому что в Editor размер сцены = размер ImGui viewport-а,
        // и ты уже делаешь Resize внутри GraphicsRenderer.RenderImGuiViewport().
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
