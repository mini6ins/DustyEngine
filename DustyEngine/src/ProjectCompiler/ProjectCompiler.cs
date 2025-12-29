using System.Diagnostics;

namespace DustyEngine.ProjectCompiler;

public static class ProjectCompiler
{
    public static bool Compile(string projectPath, string outPath, ProjectSettings settings)
    {
        var outDir = Path.Combine(outPath, settings.ProjectName);

        if (Directory.Exists(outDir))
        {
            Debug.Log("Directory already exists!", Debug.LogLevel.Info, true);
            try
            {
                Debug.Log("Trying  to delete build dir", Debug.LogLevel.Info, true);
                var dirInfo = new DirectoryInfo(outDir);
                ClearReadOnlyRecursive(dirInfo);

                Directory.Delete(outDir, recursive: true);

                Debug.Log("Build dir deleted successfully!", Debug.LogLevel.Info, true);
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to delete build dir: {outDir}\n{e}", Debug.LogLevel.Info, true);
                return false;
            }
        }

        Debug.Log($"Build dir: {outDir}", Debug.LogLevel.Info, true);

        Directory.CreateDirectory(outDir);
        Debug.Log($"Build dir was create successfully at: {outDir}", Debug.LogLevel.Info, true);

        Debug.Log($"Copying project files to build", Debug.LogLevel.Info, true);

        string projectPathInBuild = Path.Combine(outDir, "Data");
        CopyDirectory(projectPath, projectPathInBuild);

        if (!Directory.Exists(Path.Combine(outDir, Path.GetDirectoryName(projectPath))))
        {
            Debug.Log($"Directory does not exist: {outDir}", Debug.LogLevel.Info, true);
            return false;
        }

        Debug.Log($"Project files copied successfully", Debug.LogLevel.Info, true);

        var templateDir = Path.Combine( Directory.GetCurrentDirectory(), "PlayerTemplates", "linux-x64");

        CopyDirectory(templateDir, outDir);

        var oldExe = Path.Combine(outDir, "PlayerRunner");
        var newExe = Path.Combine(outDir, settings.ProjectName);

        if (File.Exists(newExe))
            File.Delete(newExe);

        File.Move(oldExe, newExe);


        RunGame(outDir, settings.ProjectName);

        return true;
    }

    private static void RunGame(string playerDir, string exeName)
    {
        var playerExe = Path.Combine(playerDir, exeName);

        var psi = new ProcessStartInfo
        {
            FileName = playerExe,
            WorkingDirectory = playerDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var p = Process.Start(psi);

        string output = p.StandardOutput.ReadToEnd();
        string error = p.StandardError.ReadToEnd();

        Console.WriteLine(output);
        Console.WriteLine(error);
    }


    private static void CopyDirectory(string sourceDir, string destDir, string? excludeRoot = null)
    {
        excludeRoot ??= Path.GetFullPath(destDir);

        var sourceFull = Path.GetFullPath(sourceDir);
        var destFull = Path.GetFullPath(destDir);

        if (IsSubPathOf(sourceFull, excludeRoot))
            return;

        Directory.CreateDirectory(destFull);

        foreach (var file in Directory.GetFiles(sourceFull))
        {
            var name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destFull, name), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceFull))
        {
            var dirName = Path.GetFileName(dir);
            if (dirName is "build" or "bin" or "obj" or ".git" or ".vs" or ".idea")
                continue;

            CopyDirectory(dir, Path.Combine(destFull, dirName), excludeRoot);
        }
    }

    private static bool IsSubPathOf(string path, string basePath)
    {
        path = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(basePath, StringComparison.Ordinal);
    }


    private static void ClearReadOnlyRecursive(DirectoryInfo dir)
    {
        dir.Attributes &= ~FileAttributes.ReadOnly;

        foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
            file.Attributes &= ~FileAttributes.ReadOnly;

        foreach (var subDir in dir.GetDirectories("*", SearchOption.AllDirectories))
            subDir.Attributes &= ~FileAttributes.ReadOnly;
    }
}
