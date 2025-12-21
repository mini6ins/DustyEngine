using System.Numerics;
using ImGuiNET;

namespace GraphicsEngineOpenGL.Editor.Panels.ViewPortPanel;

internal class ViewportPanel : IRenderablePanel
{
    public static bool IsScenePanelActive;
    private static bool _isPlayMode;
    public static Action<bool>? OnPlayModeChanged;

    public void Render()
    {
        ImGui.Begin("Scene Viewport");
        IsScenePanelActive = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        DrawTopPanel();
        ImGui.Separator();
        DrawTexture();

        ImGui.End();
    }

    private static void DrawTopPanel()
    {
        ImGui.BeginDisabled(_isPlayMode);
        if (ImGui.Button("Play"))
        {
            _isPlayMode = true;
            OnPlayModeChanged?.Invoke(_isPlayMode);
        }


        ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.BeginDisabled(!_isPlayMode);
        if (ImGui.Button("Stop"))
        {
            _isPlayMode = false;
            OnPlayModeChanged?.Invoke(_isPlayMode);
        }

        ImGui.EndDisabled();
    }

    private static void DrawTexture()
    {
        var size = ImGui.GetContentRegionAvail();
        if (!(size.X > 0) || !(size.Y > 0)) return;

        GraphicsEngineOpenGl.Renderer?.ResizeViewport((int)size.X, (int)size.Y);

        if (GraphicsEngineOpenGl.Renderer != null)
            ImGui.Image(GraphicsEngineOpenGl.Renderer.ViewportTexture, size, new Vector2(0, 1), new Vector2(1, 0));
    }
}
