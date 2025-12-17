using DustyEngine;
using DustyEngineEditor.Panels.ConsolePanel;
using DustyEngineEditor.Panels.HierarchyPanel;
using DustyEngineEditor.Panels.ViewPortPanel;
using DustyEngineEditor.Panels.ViewPortPanel.RemoteRenderer;
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
    private readonly ViewPortPanel _viewPortPanel;
    private readonly IRenderablePanel[] _renderablePanels;
    private readonly IRemoteRenderer _remoteRenderer;

    public RendererUI(IRemoteRenderer remoteRenderer)
    {
        _remoteRenderer = remoteRenderer;
        _inputHandler = new InputHandler(remoteRenderer);
        _viewPortPanel = new ViewPortPanel(_inputHandler);
        _renderablePanels =
            [new HierarchyPanel(), _viewPortPanel, new ProjectFilePanel(), new InspectorPanel(), new ConsolePanel()];

        _viewPortPanel.OnStartClicked = () =>
        {
            Console.WriteLine("START CLICKED");
            _remoteRenderer.PlayEngine();
        };

        _viewPortPanel.OnStopClicked = () =>
        {
            Console.WriteLine("STOP CLICKED");
            _remoteRenderer.StopEngine();
        };
    }

    public void Update(KeyboardState keyboardState)
    {
        if (_viewPortPanel.IsRemoteWindowFocused)
        {
            _inputHandler.ProcessFunctionKeys(keyboardState);
        }
    }

    public void Render(int texture, int textureWidth, int textureHeight, ref int framesDisplayed)
    {
        RenderTopMenuBar();

        ImGui.DockSpaceOverViewport();

        _viewPortPanel.UpdateData(texture, textureWidth, textureHeight, framesDisplayed);

        foreach (var panel in _renderablePanels)
        {
            panel.Render();
        }

        framesDisplayed = _viewPortPanel.GetFramesDisplayed();
    }

    private void RenderTopMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Save"))
                {
                    ConsolePanel.Log("Saved");
                }

                if (ImGui.MenuItem("Exit"))
                {
                    // Close app
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
}
