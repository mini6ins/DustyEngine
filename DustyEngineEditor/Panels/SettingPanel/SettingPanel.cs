using System.Numerics;
using ImGuiNET;

namespace DustyEngineEditor.Panels.SettingPanel;

internal class SettingPanel : IRenderablePanel
{
    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Test Panel"))
        {
            ImGui.Separator();

            if (ImGui.Button("Fullscreen (F11)"))
            {
            }
        }

        ImGui.End();
    }
}