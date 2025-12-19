using DustyEngineEditor.Panels.ConsolePanel;
using DustyEngineEditor.Panels.HierarchyPanel;
using DustyEngineEditor.Panels.ViewPortPanel;
using DustyEngineEditor.Panels.ViewPortPanel.RemoteRenderer;
using DustyEngineEditor.Panels.ViewPortPanel.Themes;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DustyEngineEditor;

internal interface IRenderablePanel
{
    void Render();
}

internal class RendererUI
{
    private readonly InputHandler _inputHandler;

    private readonly List<IRenderablePanel> _renderablePanels;
    private readonly ViewportPanel _viewportPanel;

    public RendererUI()
    {
        if (Editor.RemoteRenderer == null) return;

        _inputHandler = new InputHandler(Editor.RemoteRenderer);

        _viewportPanel = new ViewportPanel(_inputHandler);

        _renderablePanels =
            [new HierarchyPanel(), _viewportPanel, new ProjectFilePanel(), new InspectorPanel(), new ConsolePanel()];


        _viewportPanel.OnStartClicked = () => Editor.RemoteRenderer.PlayEngine();
        _viewportPanel.OnStopClicked = () => Editor.RemoteRenderer.StopEngine();
    }

    public void Update(KeyboardState keyboardState)
    {
        if (_viewportPanel.IsRemoteWindowFocused)
        {
            _inputHandler.ProcessFunctionKeys(keyboardState);
        }
    }

    public void Render(int texture, int textureWidth, int textureHeight, ref int framesDisplayed)
    {
        RenderTopMenuBar();

        ImGui.DockSpaceOverViewport();

        _viewportPanel.UpdateData(texture, textureWidth, textureHeight, framesDisplayed);

        foreach (var panel in _renderablePanels)
        {
            panel.Render();
        }

        framesDisplayed = _viewportPanel.GetFramesDisplayed();
    }

    private static void RenderTopMenuBar()
    {
        if (!ImGui.BeginMainMenuBar())
            return;

        // ===== File =====
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Save"))
                ConsolePanel.Log("Saved");

            // ----- Settings -----
            if (ImGui.BeginMenu("Settings"))
            {
                // ----- Themes -----
                if (ImGui.BeginMenu("Themes"))
                {

                    var isDark    = ThemeSelector.CurrentTheme == EditorTheme.Dark;
                    var isLight   = ThemeSelector.CurrentTheme == EditorTheme.Light;
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
                // Close app
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
