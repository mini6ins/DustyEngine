using System.Numerics;
using DustyEngine.Scene;
using ImGuiNET;
using SceneSystem.EngineObject.GameObject;

namespace Editor.Panels.HierarchyPanel;

internal class HierarchyPanel : IRenderablePanel
{
    private GameObject? _selected;
    private GameObject? _copiedGameObject;
    private readonly Scene _scene;

    private readonly Queue<Action> _deferredActions = new();

    public HierarchyPanel() => _scene = SceneManager.CurrentScene;

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(360, 150), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Hierarchy Panel"))
        {
            ImGui.Text("Scene: " + _scene.Name);
            ImGui.Separator();
        }

        DrawSceneHierarchy();
        ImGui.End();

        ExecuteDeferredActions();
    }

    private void DrawSceneHierarchy()
    {
        foreach (var gameObject in _scene.GameObjects.ToList())
            DrawGameObjectNode(gameObject, true);
    }

    private void DrawGameObjectNode(GameObject gameObject, bool parentActive)
    {
        var selfActive = gameObject.ActiveSelf;
        var activeInHierarchy = parentActive && selfActive;

        var hasChildren = gameObject.Children.Count > 0;

        ImGui.PushID(gameObject.GetHashCode());

        if (!activeInHierarchy)
        {
            ImGui.PushStyleColor(ImGuiCol.Text,
                ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * new Vector4(1, 1, 1, 0.4f));
        }

        DrawTreeNode(gameObject.Name, hasChildren ? () =>
                {
                    foreach (var child in gameObject.Children.ToList())
                        DrawGameObjectNode(child, activeInHierarchy);
                }
                : null, gameObject
        );

        if (!activeInHierarchy)
            ImGui.PopStyleColor();

        ImGui.PopID();
    }

    private void DrawTreeNode(string label, Action? drawChildren, GameObject gameObject)
    {
        var hasChildren = drawChildren != null;

        var flags = ImGuiTreeNodeFlags.SpanFullWidth;

        if (!hasChildren) flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        if (_selected == gameObject) flags |= ImGuiTreeNodeFlags.Selected;

        var opened = ImGui.TreeNodeEx(label, flags);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            _selected = gameObject;
            InspectorPanel.InspectorPanel.SetSelectedGameObject(_selected);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _selected = gameObject;
            ImGui.OpenPopup("object_menu");
        }

        if (ImGui.BeginPopup("object_menu"))
        {
            if (ImGui.BeginMenu("Create"))
            {
                if (ImGui.MenuItem("Empty GameObject"))
                {
                    var parent = _selected;

                    _deferredActions.Enqueue(() =>
                    {
                        SceneManager.AddGameObjectRecursively(new GameObject("Empty GameObject"), parent);
                    });
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndMenu();
            }

            if (ImGui.MenuItem("Delete", "Del"))
            {
                var toDelete = _selected;
                _deferredActions.Enqueue(() =>
                {
                    if (toDelete != null)
                        SceneManager.RemoveGameObjectRecursively(toDelete);
                });
            }

            ImGui.EndPopup();
        }

        if (!hasChildren || !opened) return;

        drawChildren!();
        ImGui.TreePop();
    }

    private void ExecuteDeferredActions()
    {
        while (_deferredActions.Count > 0)
        {
            var action = _deferredActions.Dequeue();
            action();
        }
    }
}
