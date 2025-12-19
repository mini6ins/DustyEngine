// using DustyEngineEditor.Panels.ConsolePanel;
// using DustyEngineEditor.Panels.HierarchyPanel;
// using DustyEngineEditor.Panels.ViewPortPanel;
// using DustyEngineEditor.Panels.ViewPortPanel.Themes;
// using ImGuiNET;
// using OpenTK.Windowing.GraphicsLibraryFramework;
//
// namespace DustyEngineEditor;
//
// internal interface IRenderablePanel
// {
//     void Render();
// }
//
// internal class RendererUI : IDisposable
// {
//     private readonly InputHandler? _inputHandler;
//     private readonly List<IRenderablePanel>? _renderablePanels;
//     private readonly ViewportPanel? _viewportPanel;
//
//     public RendererUI()
//     {
//         _inputHandler = new InputHandler();
//         _viewportPanel = new ViewportPanel(_inputHandler);
//
//         _renderablePanels =
//         [
//             new HierarchyPanel(),
//             _viewportPanel,
//             new ProjectFilePanel(),
//             new InspectorPanel(),
//             new ConsolePanel()
//         ];
//     }
//
//     public void Update(KeyboardState keyboardState, float deltaTime)
//     {
//         _viewportPanel?.Update(deltaTime);
//     }
//
//     public void Render()
//     {
//         RenderTopMenuBar();
//
//         ImGui.DockSpaceOverViewport();
//
//         foreach (var panel in _renderablePanels!)
//             panel.Render();
//     }
//
//     private static void RenderTopMenuBar()
//     {
//         if (!ImGui.BeginMainMenuBar())
//             return;
//
//         // ===== File =====
//         if (ImGui.BeginMenu("File"))
//         {
//             if (ImGui.MenuItem("Save"))
//                 ConsolePanel.Log("Saved");
//
//             // ----- Settings -----
//             if (ImGui.BeginMenu("Settings"))
//             {
//                 // ----- Themes -----
//                 if (ImGui.BeginMenu("Themes"))
//                 {
//                     var isDark = ThemeSelector.CurrentTheme == EditorTheme.Dark;
//                     var isLight = ThemeSelector.CurrentTheme == EditorTheme.Light;
//                     var isClassic = ThemeSelector.CurrentTheme == EditorTheme.LegacyClassic;
//                     var isGruvbox = ThemeSelector.CurrentTheme == EditorTheme.Gruvbox;
//
//                     if (ImGui.MenuItem("Dark", "", isDark, !isDark))
//                         ThemeSelector.ApplyTheme(EditorTheme.Dark);
//
//                     if (ImGui.MenuItem("Light", "", isLight, !isLight))
//                         ThemeSelector.ApplyTheme(EditorTheme.Light);
//
//                     if (ImGui.MenuItem("Legacy classic", "", isClassic, !isClassic))
//                         ThemeSelector.ApplyTheme(EditorTheme.LegacyClassic);
//
//                     if (ImGui.MenuItem("Gruvbox", "", isGruvbox, !isGruvbox))
//                         ThemeSelector.ApplyTheme(EditorTheme.Gruvbox);
//
//                     ImGui.EndMenu();
//                 }
//
//                 ImGui.EndMenu();
//             }
//
//             if (ImGui.MenuItem("Exit"))
//             {
//                 // TODO: Close app
//             }
//
//             ImGui.EndMenu();
//         }
//
//         // ===== Help =====
//         if (ImGui.BeginMenu("Help"))
//         {
//             ImGui.MenuItem("About");
//             ImGui.EndMenu();
//         }
//
//         ImGui.EndMainMenuBar();
//     }
//
//     public void Dispose()
//     {
//         _viewportPanel?.Dispose();
//     }
// }
