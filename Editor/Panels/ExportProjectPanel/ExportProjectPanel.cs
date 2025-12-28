using System.Numerics;
using ImGuiNET;

namespace Editor.Panels.ExportProjectPanel;

public class ExportProjectPanel : IRenderablePanel
{
    public bool ShowPanel;
    public static Action<string>? OnExportProject;

    private string outPath = "/home/maksym/BuildDustyEngineProject";

    public void Render()
    {
        if (!ShowPanel) return;

        ImGui.SetNextWindowSize(new Vector2(500, 300), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(200, 150), ImGuiCond.FirstUseEver);

        ImGui.Begin("Export helper panel", ref ShowPanel,
            ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse);


        ImGui.InputText("Export folder path", ref outPath, 512);

        if (ImGui.Button("Export"))
            OnExportProject?.Invoke(outPath);

        ImGui.End();
    }
}
