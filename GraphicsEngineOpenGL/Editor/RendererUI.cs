using DustyEngineEditor.Panels.ConsolePanel;
using GraphicsEngineOpenGL.Editor.Panels.HierarchyPanel;
using GraphicsEngineOpenGL.Editor.Panels.InspectorPanel;
using GraphicsEngineOpenGL.Editor.Panels.ProjectFilePanel;
using GraphicsEngineOpenGL.Editor.Panels.ViewPortPanel;
using GraphicsEngineOpenGL.Editor.Panels.ViewPortPanel.Themes;
using ImGuiNET;
using OpenTK.Windowing.Desktop;

namespace GraphicsEngineOpenGL.Editor;

internal interface IRenderablePanel
{
    void Render();
}

public  class RendererUI
{
    public static Action? OnProjectSave;
    private readonly List<IRenderablePanel>? _renderablePanels =
    [
        new ViewportPanel(),
        new ProjectFilePanel(),
        new ConsolePanel(),
        new HierarchyPanel(),
        new InspectorPanel(),
    ];


    public void Render(GameWindow window)
    {
        RenderTopMenuBar( window);

        ImGui.DockSpaceOverViewport();

        foreach (var panel in _renderablePanels!)
            panel.Render();
    }

    private static void RenderTopMenuBar(GameWindow window)
    {
        if (!ImGui.BeginMainMenuBar())
            return;

        // ===== File =====
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Save"))
                OnProjectSave?.Invoke();

            // ----- Settings -----
            if (ImGui.BeginMenu("Settings"))
            {
                // ----- Themes -----
                if (ImGui.BeginMenu("Themes"))
                {
                    var isDark = ThemeSelector.CurrentTheme == EditorTheme.Dark;
                    var isLight = ThemeSelector.CurrentTheme == EditorTheme.Light;
                    var isClassic = ThemeSelector.CurrentTheme == EditorTheme.LegacyClassic;
                    var isGruvbox = ThemeSelector.CurrentTheme == EditorTheme.Gruvbox;

                    if (ImGui.MenuItem("Dark", "", isDark, !isDark))
                        ThemeSelector.ApplyTheme(EditorTheme.Dark);

                    if (ImGui.MenuItem("Light", "", isLight, !isLight))
                        ThemeSelector.ApplyTheme(EditorTheme.Light);

                    if (ImGui.MenuItem("Legacy classic", "", isClassic, !isClassic))
                        ThemeSelector.ApplyTheme(EditorTheme.LegacyClassic);

                    if (ImGui.MenuItem("Gruvbox", "", isGruvbox, !isGruvbox))
                        ThemeSelector.ApplyTheme(EditorTheme.Gruvbox);

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Exit"))
            {
                window.Close();
            }

            ImGui.EndMenu();
        }

        // ===== Help =====
        if (ImGui.BeginMenu("Help"))
        {
            ImGui.MenuItem("About");
            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }

}
