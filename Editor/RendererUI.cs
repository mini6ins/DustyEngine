using Editor.Panels;
using Editor.Panels.ConsolePanel;
using Editor.Panels.HierarchyPanel;
using Editor.Panels.InspectorPanel;
using Editor.Panels.ProjectFilePanel;
using Editor.Panels.ProjectSetiingPanel;
using Editor.Panels.ViewPortPanel;
using Editor.Panels.ViewPortPanel.Themes;
using ImGuiNET;
using OpenTK.Windowing.Desktop;

namespace Editor;

public class RendererUI
{
    public static Action? OnProjectSave;

    private readonly ProjectSetiingPanel _projectSettingsPanel;

    private readonly List<IRenderablePanel>? _renderablePanels =
    [
        new ViewportPanel(),
        new ProjectFilePanel(),
        new ConsolePanel(),
        new HierarchyPanel(),
        new InspectorPanel(),
        new ProjectSetiingPanel(),
    ];

    public RendererUI()
    {
        _projectSettingsPanel =  new ProjectSetiingPanel();
        _renderablePanels?.Add(_projectSettingsPanel);
    }


    public void Render(GameWindow window)
    {
        RenderTopMenuBar(window);

        ImGui.DockSpaceOverViewport();

        foreach (var panel in _renderablePanels!)
            panel.Render();
    }

    private void RenderTopMenuBar(GameWindow window)
    {
        if (!ImGui.BeginMainMenuBar())
            return;

        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Save"))
                OnProjectSave?.Invoke();

            if (ImGui.BeginMenu("Settings"))
            {
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

                if (ImGui.MenuItem("Project Settings"))
                {
                    _projectSettingsPanel.ShowPanel = !_projectSettingsPanel.ShowPanel;
                }


                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Exit"))
            {
                window.Close();
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Help"))
        {
            ImGui.MenuItem("About");
            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }
}
