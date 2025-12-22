namespace Editor.Panels.ProjectFilePanel;

internal class ProjectFileManager
{
    private readonly string _rootAssets;

    public string CurrentPath { get; private set; }
    public string? ClipboardPath { get; set; }
    public string? SelectedPath { get; set; }

    public ProjectFileManager(string? projectPath)
    {
        _rootAssets = Path.Combine(projectPath ?? ".", "Assets");
        Directory.CreateDirectory(_rootAssets);
        CurrentPath = _rootAssets;
    }

    public bool CanNavigateUp()
    {
        var parent = Directory.GetParent(CurrentPath)?.FullName;
        if (string.IsNullOrEmpty(parent)) return false;

        var parentFull = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var rootFull = Path.GetFullPath(_rootAssets).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return parentFull.StartsWith(rootFull, StringComparison.Ordinal);
    }

    public void NavigateUp()
    {
        var parent = Directory.GetParent(CurrentPath)?.FullName;

        if (string.IsNullOrEmpty(parent)) return;

        var parentFull = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) +
                         Path.DirectorySeparatorChar;
        var rootFull = Path.GetFullPath(_rootAssets).TrimEnd(Path.DirectorySeparatorChar) +
                       Path.DirectorySeparatorChar;

        if (!parentFull.StartsWith(rootFull, StringComparison.Ordinal)) return;

        CurrentPath = parent;
        SelectedPath = null;
    }

    public void NavigateToFolder(string folderPath)
    {
        var targetFull = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar) +
                         Path.DirectorySeparatorChar;
        var rootFull = Path.GetFullPath(_rootAssets).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!targetFull.StartsWith(rootFull, StringComparison.Ordinal) || !Directory.Exists(folderPath)) return;

        CurrentPath = folderPath;
        SelectedPath = null;
    }

    public void RenameItem(string oldPath, string newName, bool isFolder)
    {
        try
        {
            var parentDir = Path.GetDirectoryName(oldPath)!;
            string newPath;

            if (isFolder)
            {
                newPath = Path.Combine(parentDir, newName);
                if (Directory.Exists(newPath))
                {
                    Console.WriteLine("Folder already exists");
                    return;
                }

                Directory.Move(oldPath, newPath);
            }
            else
            {
                var ext = Path.GetExtension(oldPath);
                newPath = newName.Contains('.')
                    ? Path.Combine(parentDir, newName)
                    : Path.Combine(parentDir, newName + ext);

                if (File.Exists(newPath))
                {
                    Console.WriteLine("File already exists");
                    return;
                }

                File.Move(oldPath, newPath);
            }

            Console.WriteLine($"Renamed: {oldPath} -> {newPath}");

            if (SelectedPath == oldPath)
                SelectedPath = newPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error renaming: {ex.Message}");
        }
    }

    public void DeleteItem(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Console.WriteLine($"Deleted folder: {path}");
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
                Console.WriteLine($"Deleted file: {path}");
            }

            if (SelectedPath == path)
                SelectedPath = null;

            if (ClipboardPath == path)
                ClipboardPath = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting: {ex.Message}");
        }
    }

    public void PasteClipboard(string targetFolder)
    {
        if (string.IsNullOrEmpty(ClipboardPath))
        {
            Console.WriteLine("Clipboard is empty");
            return;
        }

        try
        {
            if (!File.Exists(ClipboardPath) && !Directory.Exists(ClipboardPath))
            {
                Console.WriteLine("Clipboard item no longer exists");
                ClipboardPath = null;
                return;
            }

            var fileName = Path.GetFileName(ClipboardPath);
            var destPath = Path.Combine(targetFolder, fileName);

            if (Directory.Exists(ClipboardPath))
            {
                var sourceFullPath = Path.GetFullPath(ClipboardPath).TrimEnd(Path.DirectorySeparatorChar) +
                                     Path.DirectorySeparatorChar;
                var targetFullPath = Path.GetFullPath(targetFolder).TrimEnd(Path.DirectorySeparatorChar) +
                                     Path.DirectorySeparatorChar;

                if (targetFullPath.StartsWith(sourceFullPath, StringComparison.Ordinal))
                {
                    Console.WriteLine("Cannot paste folder into itself");
                    return;
                }
            }

            if (Path.GetDirectoryName(ClipboardPath) == targetFolder || File.Exists(destPath) ||
                Directory.Exists(destPath))
            {
                destPath = GenerateUniquePath(targetFolder, fileName);
            }

            if (Directory.Exists(ClipboardPath))
            {
                CopyDirectory(ClipboardPath, destPath);
                Console.WriteLine($"Copied: {ClipboardPath} -> {destPath}");
            }
            else
            {
                File.Copy(ClipboardPath, destPath);
                Console.WriteLine($"Copied: {ClipboardPath} -> {destPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error pasting: {ex.Message}");
        }
    }

    public void MoveItem(string sourcePath, string targetFolder)
    {
        try
        {
            var targetFullPath = Path.GetFullPath(targetFolder).TrimEnd(Path.DirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            var rootFullPath = Path.GetFullPath(_rootAssets).TrimEnd(Path.DirectorySeparatorChar) +
                               Path.DirectorySeparatorChar;

            if (!targetFullPath.StartsWith(rootFullPath, StringComparison.Ordinal))
            {
                Console.WriteLine("Cannot move outside Assets folder");
                return;
            }

            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(targetFolder, fileName);

            if (Directory.Exists(sourcePath))
            {
                var sourceFullPath = Path.GetFullPath(sourcePath).TrimEnd(Path.DirectorySeparatorChar) +
                                     Path.DirectorySeparatorChar;

                if (targetFullPath.StartsWith(sourceFullPath, StringComparison.Ordinal))
                {
                    Console.WriteLine("Cannot move folder into itself");
                    return;
                }
            }

            if (Path.GetDirectoryName(sourcePath) == targetFolder)
            {
                Console.WriteLine("Already in target folder");
                return;
            }

            if (File.Exists(destPath) || Directory.Exists(destPath))
                destPath = GenerateUniquePath(targetFolder, fileName);

            if (Directory.Exists(sourcePath))
                Directory.Move(sourcePath, destPath);
            else
                File.Move(sourcePath, destPath);

            Console.WriteLine($"Moved: {sourcePath} -> {destPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error moving item: {ex.Message}");
        }
    }

    public string CreateNewFolder()
    {
        const string baseName = "New Folder";
        var newPath = GenerateUniquePath(CurrentPath, baseName);

        Directory.CreateDirectory(newPath);
        return newPath;
    }

    public string CreateNewScene()
    {
        const string baseName = "New Scene";
        const string ext = ".json";
        var newPath = GenerateUniquePath(CurrentPath, baseName + ext);

        File.WriteAllText(newPath, "{}");
        return newPath;
    }

    public string CreateNewScript()
    {
        const string baseName = "New Script";
        const string ext = ".cs";
        var newPath = GenerateUniquePath(CurrentPath, baseName + ext);

        File.WriteAllText(newPath, "using System;\n\npublic class NewClass\n{\n}\n");
        return newPath;
    }

    private static string GenerateUniquePath(string directory, string fileName)
    {
        var newPath = Path.Combine(directory, fileName);

        if (!File.Exists(newPath) && !Directory.Exists(newPath))
            return newPath;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var counter = 1;

        do
        {
            fileName = $"{nameWithoutExt} ({counter}){ext}";
            newPath = Path.Combine(directory, fileName);
            counter++;
        } while (File.Exists(newPath) || Directory.Exists(newPath));

        return newPath;
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
}
