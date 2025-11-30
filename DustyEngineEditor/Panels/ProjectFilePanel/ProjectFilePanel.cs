using System.Numerics;
using ImGuiNET;

namespace DustyEngineEditor.Panels.HierarchyPanel;

internal class ProjectFilePanel : IRenderablePanel
{
    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("ProjectFile Panel"))
        {
            ImGui.Separator();
        }

        ImGui.End();
    }
}


