using DustyEngineEditor.Panels.RemoteRenderer;
using DustyEngineEditor.Panels.SettingPanel;
using DustyEngineEditor.Panels.ViewPortPanel;
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

    public RendererUI(IRemoteRenderer remoteRenderer)
    {
        _inputHandler = new InputHandler(remoteRenderer);
        _viewPortPanel = new ViewPortPanel(_inputHandler);
        _renderablePanels = [new SettingPanel(), _viewPortPanel];
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
        ImGui.DockSpaceOverViewport();

        _viewPortPanel.UpdateData(texture, textureWidth, textureHeight, framesDisplayed);

        foreach (var panel in _renderablePanels)
        {
            panel.Render();
        }

        framesDisplayed = _viewPortPanel.GetFramesDisplayed();
    }
}