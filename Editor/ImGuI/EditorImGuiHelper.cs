using System.Numerics;
using Editor.ImGuI.Impl;
using ImGuiNET;
using OpenTK.Windowing.Desktop;

namespace Editor.ImGuI;

public static class EditorImGuiHelper
{
    private static RendererUI? _renderUI;

    public static void ImGuiInit(GameWindow window)
    {
        ImGui.CreateContext();
        ImguiImplOpenTK4.Init(window);
        ImguiImplOpenGL3.Init();

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        _renderUI = new RendererUI();
    }

    public static void ImGuiRender(GameWindow window)
    {
        ImguiImplOpenGL3.NewFrame();
        ImguiImplOpenTK4.NewFrame();
        ImGui.NewFrame();

        var viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowViewport(viewport.ID);


        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar |
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

        _renderUI?.Render(window);

        ImGui.End();

        ImGui.Render();
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
    }
}
