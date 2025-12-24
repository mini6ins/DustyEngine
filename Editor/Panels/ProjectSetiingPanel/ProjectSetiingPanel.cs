using System.Numerics;
using ImGuiNET;

namespace Editor.Panels.ProjectSetiingPanel;

public class ProjectSetiingPanel : IRenderablePanel
{
    public bool ShowPanel;
    public static List<string>? ScenePaths { get; set; }

    public static Action? OnSaveProjectSettings;

    public void Render()
    {
        if (!ShowPanel) return;

        ImGui.SetNextWindowSize(new Vector2(300, 200), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(200, 150), ImGuiCond.FirstUseEver);

        ImGui.Begin("ProjectSetting", ref ShowPanel,
            ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse);
        ImGui.Text("Scene Paths:");
        ImGui.TextDisabled("Add scenes here to use them in project");
        ImGui.Separator();

        if (ScenePaths != null)
            for (int i = 0; i < ScenePaths.Count; i++)
            {
                ImGui.PushID(i);

                ImGui.Text(i + ".");
                ImGui.SameLine();
                var tmp = ScenePaths[i];
                if (ImGui.InputText("##item", ref tmp, 256))
                    ScenePaths[i] = tmp;

                ImGui.SameLine();

                if (ImGui.Button("X"))
                {
                    ScenePaths.RemoveAt(i);
                    ImGui.PopID();
                    break;
                }

                ImGui.PopID();
            }

        if (ImGui.Button("+ Add scene path"))
            ScenePaths.Add("");

        ImGui.Separator();

        if (ImGui.Button("Save project settings"))
            OnSaveProjectSettings?.Invoke();


        ImGui.End();
    }
}
