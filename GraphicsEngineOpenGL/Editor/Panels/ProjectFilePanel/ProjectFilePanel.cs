using System.Numerics;
using System.Text;
using DustyEngineEditor.Panels.HierarchyPanel;
using ImGuiNET;

namespace GraphicsEngineOpenGL.Editor.Panels.ProjectFilePanel;

internal class ProjectFilePanel : IRenderablePanel
{
    private readonly ProjectFileManager _fileManager;

    private string? _renamingPath;
    private string _renameBuffer = "";

    private string? _draggedPath;
    private double _lastClickTime;
    private string? _lastClickedPath;


    public ProjectFilePanel()
    {
        _fileManager = new ProjectFileManager(GraphicsEngineOpenGl.ProjectPath);
        IconLoader.InitIcons();
    }

    public void Render()
    {
        ImGui.SetNextWindowSize(new Vector2(420, 260), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Project", ImGuiWindowFlags.None))
        {
            ImGui.End();
            return;
        }

        ImGui.TextDisabled("Path:");
        ImGui.SameLine();
        ImGui.Text(_fileManager.CurrentPath);

        ImGui.Separator();

        HandleGlobalHotkeys();
        DrawGrid(_fileManager.CurrentPath);
        DrawContextMenu();

        ImGui.End();
    }

    private void HandleGlobalHotkeys()
    {
        var io = ImGui.GetIO();
        var ctrlPressed = io.KeyCtrl;

        if (!string.IsNullOrEmpty(_renamingPath))
            return;

        if (ctrlPressed && ImGui.IsKeyPressed(ImGuiKey.V) && !string.IsNullOrEmpty(_fileManager.ClipboardPath))
            _fileManager.PasteClipboard(_fileManager.CurrentPath);

        if (string.IsNullOrEmpty(_fileManager.SelectedPath))
            return;

        if (ctrlPressed && ImGui.IsKeyPressed(ImGuiKey.C))
        {
            _fileManager.ClipboardPath = _fileManager.SelectedPath;
            Console.WriteLine($"Copied: {_fileManager.ClipboardPath}");
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Delete))
            _fileManager.DeleteItem(_fileManager.SelectedPath);

        HandleF2Rename();
    }

    private void HandleF2Rename()
    {
        if (!Enum.IsDefined(typeof(ImGuiKey), "F2")) return;

        if (ImGui.IsKeyPressed((ImGuiKey)Enum.Parse(typeof(ImGuiKey), "F2")))
            StartRenaming(_fileManager.SelectedPath!);
    }

    private void DrawGrid(string path)
    {
        const float iconSize = 48f;
        const float padding = 12f;
        const float cellSize = iconSize + padding * 2;

        var width = ImGui.GetContentRegionAvail().X;
        var columns = (int)(width / cellSize);
        if (columns < 1) columns = 1;

        ImGui.Columns(columns, "project_grid", false);

        if (!Directory.Exists(path))
        {
            ImGui.TextDisabled("Directory not found.");
            ImGui.Columns(1);
            return;
        }

        if (_fileManager.CanNavigateUp())
            DrawItem(true, "..", "..");

        foreach (var entry in Directory.GetFileSystemEntries(path))
        {
            var attr = File.GetAttributes(entry);
            var isDir = attr.HasFlag(FileAttributes.Directory);
            DrawItem(isDir, Path.GetFileName(entry), entry);
        }

        ImGui.Columns(1);
    }

    private void DrawItem(bool isFolder, string label, string fullPath)
    {
        const float icon = 48f;
        var tileW = ImGui.GetColumnWidth() - ImGui.GetStyle().ItemSpacing.X - 10;

        const float tileH = 80f;

        ImGui.PushID(fullPath);
        var start = ImGui.GetCursorScreenPos();

        var tex = GetIconForItem(isFolder, label);
        ImGui.Image(tex, new Vector2(icon, icon));

        if (_renamingPath == fullPath && label != "..")
            DrawRenameInput(tileW, fullPath, isFolder);
        else
            ImGui.TextWrapped(label);

        DrawTileButton(start, tileW, tileH, isFolder, label, fullPath);
        DrawDragDrop(isFolder, label, fullPath);
        DrawItemContextMenu(isFolder, label, fullPath);
        DrawSelectionHighlight(start, tileW, tileH, label, fullPath);

        ImGui.PopID();
        ImGui.NextColumn();
    }

    private void DrawRenameInput(float tileW, string fullPath, bool isFolder)
    {
        ImGui.SetNextItemWidth(tileW);
        ImGui.SetKeyboardFocusHere();

        var enter = ImGui.InputText("##rename", ref _renameBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue);

        if (enter || ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            if (enter && !string.IsNullOrWhiteSpace(_renameBuffer))
            {
                _fileManager.RenameItem(fullPath, _renameBuffer, isFolder);
            }

            _renamingPath = null;
        }

        if (!ImGui.IsItemActive() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            _renamingPath = null;
    }

    private void DrawTileButton(Vector2 start, float tileW, float tileH, bool isFolder, string label, string fullPath)
    {
        ImGui.SetCursorScreenPos(start);

        if (!ImGui.InvisibleButton("##tile", new Vector2(tileW, tileH))) return;

        if (_renamingPath != null) return;
        _fileManager.SelectedPath = fullPath;

        var currentTime = ImGui.GetTime();
        var isDoubleClick = _lastClickedPath == fullPath && (currentTime - _lastClickTime) < 0.3;

        _lastClickedPath = fullPath;
        _lastClickTime = currentTime;

        if (!isDoubleClick || !isFolder) return;

        if (label == "..")
            _fileManager.NavigateUp();
        else
            _fileManager.NavigateToFolder(fullPath);
    }

    private void DrawDragDrop(bool isFolder, string label, string fullPath)
    {
        if (label != ".." && ImGui.BeginDragDropSource())
        {
            _draggedPath = fullPath;

            unsafe
            {
                var bytes = Encoding.UTF8.GetBytes(fullPath);
                fixed (byte* ptr = bytes)
                {
                    ImGui.SetDragDropPayload("PROJECT_ITEM", (IntPtr)ptr, (uint)bytes.Length);
                }
            }

            ImGui.Text($"Moving: {label}");
            ImGui.EndDragDropSource();
        }

        if (!isFolder || !ImGui.BeginDragDropTarget()) return;

        var targetFolder = label == ".."
            ? Directory.GetParent(_fileManager.CurrentPath)?.FullName ?? _fileManager.CurrentPath
            : fullPath;

        unsafe
        {
            var payload = ImGui.AcceptDragDropPayload("PROJECT_ITEM");
            if (payload.NativePtr != null && _draggedPath != null)
            {
                _fileManager.MoveItem(_draggedPath, targetFolder);
                _draggedPath = null;
            }
        }

        ImGui.EndDragDropTarget();
    }

    private void DrawItemContextMenu(bool isFolder, string label, string fullPath)
    {
        if (!ImGui.BeginPopupContextItem("##item_ctx"))
            return;

        _fileManager.SelectedPath = fullPath;

        if (label != "..")
        {
            if (ImGui.MenuItem("Copy", "Ctrl+C"))
            {
                _fileManager.ClipboardPath = fullPath;
                Console.WriteLine($"Copied: {_fileManager.ClipboardPath}");
            }

            var hasClipboard = !string.IsNullOrEmpty(_fileManager.ClipboardPath);
            if (!hasClipboard) ImGui.BeginDisabled();

            if (ImGui.MenuItem("Paste", "Ctrl+V"))
            {
                var pasteTarget = isFolder ? fullPath : _fileManager.CurrentPath;
                _fileManager.PasteClipboard(pasteTarget);
            }

            if (!hasClipboard) ImGui.EndDisabled();

            ImGui.Separator();

            if (ImGui.MenuItem("Rename", "F2"))
                StartRenaming(fullPath);

            if (ImGui.MenuItem("Delete", "Del"))
                _fileManager.DeleteItem(fullPath);
        }
        else
        {
            ImGui.BeginDisabled();
            ImGui.MenuItem("Copy");
            ImGui.MenuItem("Paste");
            ImGui.MenuItem("Rename");
            ImGui.MenuItem("Delete");
            ImGui.EndDisabled();
        }

        ImGui.EndPopup();
    }

    private void DrawSelectionHighlight(Vector2 start, float tileW, float tileH, string label, string fullPath)
    {
        var isSelected = _fileManager.SelectedPath == fullPath && label != "..";
        var isHovered = ImGui.IsItemHovered();

        if (!isSelected && !isHovered) return;

        var draw = ImGui.GetWindowDrawList();
        var color = isSelected
            ? ImGui.GetColorU32(ImGuiCol.ButtonActive)
            : ImGui.GetColorU32(ImGuiCol.ButtonHovered);

        draw.AddRect(start, start + new Vector2(tileW, tileH), color, 6f, ImDrawFlags.None, 2f);
    }

    private void DrawContextMenu()
    {
        if (!ImGui.BeginPopupContextWindow("##project_ctx",
                ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems))
            return;

        if (ImGui.BeginMenu("Create"))
        {
            if (ImGui.MenuItem("Folder"))
            {
                var newPath = _fileManager.CreateNewFolder();
                StartRenaming(newPath);
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.MenuItem("Scene"))
            {
                var newPath = _fileManager.CreateNewScene();
                StartRenaming(newPath);
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.MenuItem("Script"))
            {
                var newPath = _fileManager.CreateNewScript();
                StartRenaming(newPath);
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();

        var hasClipboard = !string.IsNullOrEmpty(_fileManager.ClipboardPath);
        if (!hasClipboard) ImGui.BeginDisabled();

        if (ImGui.MenuItem("Paste", "Ctrl+V"))
            _fileManager.PasteClipboard(_fileManager.CurrentPath);

        if (!hasClipboard) ImGui.EndDisabled();

        ImGui.EndPopup();
    }

    private void StartRenaming(string path)
    {
        _renamingPath = path;
        _renameBuffer = Path.GetFileName(path);

        if (File.Exists(path) && !Directory.Exists(path))
            _renameBuffer = Path.GetFileNameWithoutExtension(path);

        Console.WriteLine($"Renaming: {path}");
    }

    private static int GetIconForItem(bool isFolder, string name)
    {
        if (isFolder) return IconLoader.FolderIcon;

        var ext = Path.GetExtension(name).ToLowerInvariant();

        return ext switch
        {
            ".cs" => IconLoader.CSharpIcon,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" => IconLoader.ImageIcon,
            ".obj" => IconLoader.ObjIcon,
            ".json" => IconLoader.SceneIcon,
            _ => IconLoader.FileIcon
        };
    }
}
