using DustyEngineEditor.Panels.ViewPortPanel.Themes;
using ImGui_OpenTK.Backends;
using ImGuiNET;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DustyEngineEditor;

public class EditorWindow() : GameWindow(GameWindowSettings.Default, new NativeWindowSettings
{
    ClientSize = new Vector2i(1024, 768),
    Title = "DustyEngineEditor " + Editor.EditorVersion + "v",
    API = ContextAPI.OpenGL,
    APIVersion = new Version(3, 3),
    Flags = ContextFlags.Default
})
{
    private RendererUI _renderer = null!;

    protected override void OnLoad()
    {
        base.OnLoad();

        GLLoader.LoadBindings(new GLFWBindingsContext());

        GL.ClearColor(0.15f, 0.15f, 0.15f, 1.0f);

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        ImguiImplOpenTK4.Init(this);
        ImguiImplOpenGL3.Init();


        ThemeSelector.ApplyTheme(EditorTheme.Dark);
        IconLoader.InitIcons();

        _renderer = new RendererUI();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
            Close();

        _renderer.Update(KeyboardState, (float)args.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();

        _renderer.Render();

        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        _renderer.Dispose();

        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();
        ImGui.DestroyContext();
    }
}
