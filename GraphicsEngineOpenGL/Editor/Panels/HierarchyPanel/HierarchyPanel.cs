using System.Numerics;
using DustyEngine.Scene;
using GraphicsEngineOpenGL;
using ImGuiNET;

namespace DustyEngineEditor.Panels.HierarchyPanel;

internal class HierarchyPanel : IRenderablePanel
{

    public HierarchyPanel()
    {
        // Scene scene = Editor.RemoteRenderer!.GetCurrentScene().Result; // ⚠️ Опасно!
        // ConsolePanel.ConsolePanel.Log("scene: " + scene.Name);
    }

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Hierarchy Panel"))
        {
            ImGui.Separator();
        }

        DrawSceneHierarchy();



        ImGui.End();
    }


    private string? _selectedNode;


    private void DrawSceneHierarchy()
    {
        DrawTreeNode("Node A", () =>
        {
            DrawTreeNode("A - item 1");

            DrawTreeNode("Node B", () =>
            {
                DrawTreeNode("B - item 1");
                DrawTreeNode("B - item 2");

                DrawTreeNode("Node C", () => { DrawTreeNode("C - item"); });
            });
        });
    }


    private void DrawTreeNode(string label, Action? children = null)
    {
        var hasChildren = children != null;

        var flags = ImGuiTreeNodeFlags.SpanFullWidth;

        if (hasChildren)
            flags |= ImGuiTreeNodeFlags.OpenOnArrow;
        else
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

        if (_selectedNode == label)
            flags |= ImGuiTreeNodeFlags.Selected;

        var open = ImGui.TreeNodeEx(label, flags);

        if (ImGui.IsItemClicked())
        {
            _selectedNode = label;
            InspectorPanel.SetSelectedNode(_selectedNode);
        }

        if (!hasChildren || !open) return;

        children!.Invoke();
        ImGui.TreePop();
    }
}
