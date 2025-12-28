using System.Numerics;
using System.Text;
using DustyEngine.Core;
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

        ImGui.SetNextWindowSize(new Vector2(500, 300), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(200, 150), ImGuiCond.FirstUseEver);

        ImGui.Begin("ProjectSetting", ref ShowPanel,
            ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse);
        ImGui.Text("Scene Paths:");
        ImGui.TextDisabled("Add scenes here to use them in project");
        ImGui.TextDisabled("You can drag .json files from Project panel");
        ImGui.Separator();

        if (ScenePaths != null)
        {
            for (int i = 0; i < ScenePaths.Count; i++)
            {
                ImGui.PushID(i);

                ImGui.Text($"{i}.");
                ImGui.SameLine();

                var tmp = ScenePaths[i];
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 30);

                if (ImGui.InputText("##item", ref tmp, 512))
                {
                    ScenePaths[i] = tmp;
                }

                if (ImGui.BeginDragDropTarget())
                {
                    unsafe
                    {
                        var payload = ImGui.AcceptDragDropPayload("PROJECT_ITEM");
                        if (payload.NativePtr != null)
                        {
                            var dataPtr = (byte*)payload.Data;
                            var dataSize = (int)payload.DataSize;

                            var bytes = new byte[dataSize];
                            for (int j = 0; j < dataSize; j++)
                            {
                                bytes[j] = dataPtr[j];
                            }

                            var droppedPath = Encoding.UTF8.GetString(bytes);

                            if (droppedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            {
                                var relativePath = PathUtility.GetRelativePath(droppedPath);
                                ScenePaths[i] = relativePath;
                                DustyEngine.Debug.Log($"Added scene path: {relativePath}",
                                    DustyEngine.Debug.LogLevel.Info, true);
                            }
                            else
                            {
                                DustyEngine.Debug.Log("Only .json scene files can be added",
                                    DustyEngine.Debug.LogLevel.Warning, true);
                            }
                        }
                    }

                    ImGui.EndDragDropTarget();
                }

                ImGui.SameLine();

                if (ImGui.Button("X"))
                {
                    ScenePaths.RemoveAt(i);
                    ImGui.PopID();
                    break;
                }

                ImGui.PopID();
            }
        }

        ImGui.Separator();

        if (ImGui.Button("+ Add scene path"))
        {
            ScenePaths?.Add("");
        }

        ImGui.SameLine();

        if (ImGui.Button("+ Add from Project"))
        {
            ScenePaths?.Add("");
        }

        if (ImGui.BeginDragDropTarget())
        {
            unsafe
            {
                var payload = ImGui.AcceptDragDropPayload("PROJECT_ITEM");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (byte*)payload.Data;
                    var dataSize = (int)payload.DataSize;

                    var bytes = new byte[dataSize];
                    for (int j = 0; j < dataSize; j++)
                    {
                        bytes[j] = dataPtr[j];
                    }

                    var droppedPath = Encoding.UTF8.GetString(bytes);

                    if (droppedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var relativePath = PathUtility.GetRelativePath(droppedPath);

                        if (ScenePaths != null && !ScenePaths.Contains(relativePath))
                        {
                            ScenePaths.Add(relativePath);
                            DustyEngine.Debug.Log($"Added scene path: {relativePath}",
                                DustyEngine.Debug.LogLevel.Info, true);
                        }
                        else
                        {
                            DustyEngine.Debug.Log("Scene path already exists",
                                DustyEngine.Debug.LogLevel.Warning, true);
                        }
                    }
                    else
                    {
                        DustyEngine.Debug.Log("Only .json scene files can be added",
                            DustyEngine.Debug.LogLevel.Warning, true);
                    }
                }
            }

            ImGui.EndDragDropTarget();
        }

        ImGui.Separator();

        if (ImGui.Button("Save project settings"))
        {
            OnSaveProjectSettings?.Invoke();
        }

        ImGui.End();
    }
}
