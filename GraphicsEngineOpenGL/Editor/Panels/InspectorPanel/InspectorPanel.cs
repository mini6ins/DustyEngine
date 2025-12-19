using System.Numerics;
using GraphicsEngineOpenGL;
using ImGuiNET;

namespace DustyEngineEditor.Panels.HierarchyPanel;

internal class InspectorPanel : IRenderablePanel
{
    private static string _selectedNode = "";
    public static void SetSelectedNode(string selectedNode) => _selectedNode = selectedNode;

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Inspector  Panel"))
        {
            ImGui.Text("Item: " + _selectedNode);
            ImGui.Separator();
        }

        ImGui.End();
    }
}
