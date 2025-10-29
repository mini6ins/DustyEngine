using DustyEngine;
using ImGui_OpenTK.Backends;
using ImGuiNET;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL;

public class ImGuiManager
{
    private bool _initialized;

    public void Initialize(GameWindow window)
    {
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.StyleColorsDark();

        ImguiImplOpenTK4.Init(window);
        ImguiImplOpenGL3.Init();

        _initialized = true;
        Debug.Log("ImGui initialized", Debug.LogLevel.Info, true);
    }

    public void NewFrame()
    {
        if (!_initialized) return;

        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();
    }

    public void Render()
    {
        if (!_initialized) return;

        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
    }

    public void Shutdown()
    {
        if (!_initialized) return;

        ImguiImplOpenGL3.Shutdown();
        ImguiImplOpenTK4.Shutdown();

        _initialized = false;
        Debug.Log("ImGui shutdown", Debug.LogLevel.Info, true);
    }
}