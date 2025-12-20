using System.Numerics;
using DustyEngine;
using DustyEngine.Scene;
using DustyEngineEditor.Panels.HierarchyPanel;
using ImGuiNET;

namespace GraphicsEngineOpenGL.Editor.Panels.HierarchyPanel;

internal class HierarchyPanel : IRenderablePanel
{
    private GameObject? _selected;

    private readonly Scene _scene;

    public HierarchyPanel() => _scene = SceneManager.CurrentScene;

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Hierarchy Panel"))
        {
            ImGui.Text("Scene: "  + _scene.Name);
            ImGui.Separator();
        }

        DrawSceneHierarchy();
        ImGui.End();
    }

    private void DrawSceneHierarchy()
    {
        foreach (var gameObject in _scene.GameObjects)
            DrawGameObjectNode(gameObject,true);
    }

    private void DrawGameObjectNode(GameObject gameObject, bool parentActive)
    {
        var selfActive = gameObject.IsActive;
        var activeInHierarchy = parentActive && selfActive;

        var hasChildren = gameObject.Children.Count > 0;

        ImGui.PushID(gameObject.GetHashCode());

        if (!activeInHierarchy)
        {
            ImGui.PushStyleColor(ImGuiCol.Text,
                ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * new Vector4(1, 1, 1, 0.4f));
        }

        DrawTreeNode(
            gameObject.Name,
            hasChildren
                ? () =>
                {
                    foreach (var child in gameObject.Children)
                        DrawGameObjectNode(child, activeInHierarchy);
                }
                : null,
            gameObject
        );

        if (!activeInHierarchy)
            ImGui.PopStyleColor();

        ImGui.PopID();
    }

    private void DrawTreeNode(string label, Action? drawChildren, GameObject gameObject)
    {
        var hasChildren = drawChildren != null;

        var flags = ImGuiTreeNodeFlags.SpanFullWidth;

        if (!hasChildren)
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

        if (_selected == gameObject)
            flags |= ImGuiTreeNodeFlags.Selected;

        var opened = ImGui.TreeNodeEx(label, flags);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            _selected = gameObject;
            InspectorPanel.InspectorPanel.SetSelectedGameObject(_selected);
        }

        if (!hasChildren || !opened) return;

        drawChildren!();
        ImGui.TreePop();
    }
}
