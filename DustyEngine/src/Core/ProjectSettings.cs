using System.Text.Json;
using DustyEngine.Core;
using DustyEngine.Engine.Math.Vectors;
using DustyEngine.Scene;
using Editor.Panels.ProjectSetiingPanel;

namespace DustyEngine;

public class ProjectSettings
{
    public string ProjectName { get; set; }
    public float Version { get; set; }

    public List<string>? PathToScenes { get; set; } = [];
    public string PathToFragShader { get; set; }
    public string PathToVertShader { get; set; }

    public bool Debug { get; set; }
    public Debug.LogLevel LogLevel { get; set; }
    public bool LogToConsole { get; set; }
    public bool LogToFile { get; set; }

    public Vector2i ScreenSize { get; set; }
    public bool Vsync { get; set; }

    public static void SaveProject(ProjectSettings settings)
    {
        SceneSerializer.SaveScene(SceneManager.CurrentScene, SceneManager.GetCurrentScenePath());
        SaveProjectSettings(settings);
    }

    public static void SaveProjectSettings(ProjectSettings settings)
    {
        settings.PathToScenes = new List<string>(ProjectSetiingPanel.ScenePaths ?? []);

        global::DustyEngine.Debug.Log(
            $"[SaveProjectSettings] Saving {settings.PathToScenes.Count} scene paths",
            global::DustyEngine.Debug.LogLevel.Info,
            true);

        foreach (var p in settings.PathToScenes)
        {
            global::DustyEngine.Debug.Log(
                $"  - Scene path before save: {p}",
                global::DustyEngine.Debug.LogLevel.Info,
                true);
        }

        SerializeProjectSettings(settings, DustyEngine.ProjectFolderPath);
    }

    public static void SerializeProjectSettings(ProjectSettings projectSettings, string projectFolderPath)
    {
        string projectRoot = Path.GetFullPath(projectFolderPath);

        var copy = new ProjectSettings
        {
            ProjectName = projectSettings.ProjectName,
            Version = projectSettings.Version,
            Debug = projectSettings.Debug,
            LogLevel = projectSettings.LogLevel,
            LogToConsole = projectSettings.LogToConsole,
            LogToFile = projectSettings.LogToFile,
            ScreenSize = projectSettings.ScreenSize,
            Vsync = projectSettings.Vsync,

            PathToScenes = projectSettings.PathToScenes?
                .Select(p => EnsureRelativePath(p, projectRoot))
                .ToList() ?? [],

            PathToFragShader = EnsureRelativePath(projectSettings.PathToFragShader, projectRoot),
            PathToVertShader = EnsureRelativePath(projectSettings.PathToVertShader, projectRoot)
        };

        global::DustyEngine.Debug.Log(
            $"[SerializeProjectSettings] After conversion:",
            global::DustyEngine.Debug.LogLevel.Info,
            true);

        foreach (var p in copy.PathToScenes)
        {
            global::DustyEngine.Debug.Log(
                $"  - Will save: {p}",
                global::DustyEngine.Debug.LogLevel.Info,
                true);
        }

        string json = JsonSerializer.Serialize(
            copy,
            new JsonSerializerOptions { WriteIndented = true }
        );

        string settingsPath = Path.Combine(projectRoot, "Settings/project_settings.json");
        File.WriteAllText(settingsPath, json);

        global::DustyEngine.Debug.Log(
            $"[SerializeProjectSettings] Saved to: {settingsPath}",
            global::DustyEngine.Debug.LogLevel.Info,
            true);
    }

    public static ProjectSettings DeserializeProjectSettings(string projectFolderPath)
    {
        string projectRoot = Path.GetFullPath(projectFolderPath);
        string filePath = Path.Combine(projectRoot, "Settings/project_settings.json");

        if (!File.Exists(filePath))
        {
            global::DustyEngine.Debug.Log(
                "Project settings file not found",
                global::DustyEngine.Debug.LogLevel.FatalError,
                true
            );
            return null!;
        }

        string fileContent = File.ReadAllText(filePath);
        var settings = JsonSerializer.Deserialize<ProjectSettings>(fileContent);

        if (settings == null)
        {
            global::DustyEngine.Debug.Log(
                "Project settings could not be loaded",
                global::DustyEngine.Debug.LogLevel.FatalError,
                true
            );
            return null!;
        }

        global::DustyEngine.Debug.Log(
            $"[DeserializeProjectSettings] Loaded {settings.PathToScenes?.Count ?? 0} scene paths from JSON",
            global::DustyEngine.Debug.LogLevel.Info,
            true);

        settings.PathToScenes = settings.PathToScenes?
            .Select(p =>
            {
                var relative = EnsureRelativePath(p, projectRoot);
                global::DustyEngine.Debug.Log(
                    $"  - Loaded from JSON: {p} -> Converted to: {relative}",
                    global::DustyEngine.Debug.LogLevel.Info,
                    true);
                return relative;
            })
            .ToList() ?? [];

        settings.PathToFragShader = PathUtility.GetAbsolutePath(settings.PathToFragShader);
        settings.PathToVertShader = PathUtility.GetAbsolutePath(settings.PathToVertShader);

        return settings;
    }

    private static string EnsureRelativePath(string path, string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        path = path.Replace('\\', '/');
        projectRoot = projectRoot.Replace('\\', '/').TrimEnd('/');

        if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (Path.IsPathRooted(path))
        {
            string normalizedPath = Path.GetFullPath(path).Replace('\\', '/');

            int assetsIndex = normalizedPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return normalizedPath.Substring(assetsIndex + 1);
            }

            if (normalizedPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = normalizedPath.Substring(projectRoot.Length + 1);

                if (!relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = "Assets/" + relativePath;
                }

                return relativePath;
            }

            string fileName = Path.GetFileName(normalizedPath);
            global::DustyEngine.Debug.Log(
                $"[EnsureRelativePath] WARNING: Could not convert absolute path to relative: {path}. Using filename only: Assets/{fileName}",
                global::DustyEngine.Debug.LogLevel.Warning,
                true);
            return "Assets/" + fileName;
        }

        if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return "Assets/" + path;
        }

        return path;
    }

    public static void ValidateAndFixScenePaths(ProjectSettings settings)
    {
        if (settings?.PathToScenes == null || settings.PathToScenes.Count == 0)
            return;

        string projectRoot = Path.GetFullPath(DustyEngine.ProjectFolderPath);
        string assetsFolder = Path.Combine(projectRoot, "Assets");

        bool pathsChanged = false;
        var validPaths = new List<string>();
        var movedScenes = new Dictionary<string, string>();

        global::DustyEngine.Debug.Log(
            $"[ValidateAndFixScenePaths] Validating {settings.PathToScenes.Count} scene paths",
            global::DustyEngine.Debug.LogLevel.Info,
            true);

        for (int i = 0; i < settings.PathToScenes.Count; i++)
        {
            var scenePath = settings.PathToScenes[i];

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                global::DustyEngine.Debug.Log(
                    $"Removed empty scene path at index {i}",
                    global::DustyEngine.Debug.LogLevel.Warning,
                    true);
                pathsChanged = true;
                continue;
            }

            var absolutePath = PathUtility.GetAbsolutePath(scenePath);

            if (File.Exists(absolutePath))
            {
                var ensuredRelative = EnsureRelativePath(scenePath, projectRoot);
                validPaths.Add(ensuredRelative);

                global::DustyEngine.Debug.Log(
                    $"  ✓ Scene exists: {scenePath} (as relative: {ensuredRelative})",
                    global::DustyEngine.Debug.LogLevel.Info,
                    true);
                continue;
            }

            global::DustyEngine.Debug.Log(
                $"Scene file not found: {absolutePath}",
                global::DustyEngine.Debug.LogLevel.Warning,
                true);

            var fileName = Path.GetFileName(absolutePath);
            var foundPath = FindSceneFileRecursive(assetsFolder, fileName);

            if (foundPath != null)
            {
                global::DustyEngine.Debug.Log(
                    $"Found moved scene: {fileName} at new location: {foundPath}",
                    global::DustyEngine.Debug.LogLevel.Info,
                    true);

                var relativeFoundPath = EnsureRelativePath(foundPath, projectRoot);
                movedScenes[absolutePath] = foundPath;
                validPaths.Add(relativeFoundPath);
                pathsChanged = true;

                global::DustyEngine.Debug.Log(
                    $"  → Added as relative: {relativeFoundPath}",
                    global::DustyEngine.Debug.LogLevel.Info,
                    true);
            }
            else
            {
                global::DustyEngine.Debug.Log(
                    $"Scene file '{fileName}' not found anywhere in Assets folder - removed from project settings",
                    global::DustyEngine.Debug.LogLevel.Error,
                    true);
                pathsChanged = true;
            }
        }

        if (pathsChanged)
        {
            settings.PathToScenes = validPaths;
            ProjectSetiingPanel.ScenePaths = new List<string>(validPaths);

            global::DustyEngine.Debug.Log(
                $"[ValidateAndFixScenePaths] Paths changed, saving...",
                global::DustyEngine.Debug.LogLevel.Info,
                true);

            SaveProjectSettings(settings);

            global::DustyEngine.Debug.Log(
                $"Project settings updated: validated {validPaths.Count} scene paths",
                global::DustyEngine.Debug.LogLevel.Info,
                true);
        }
        else
        {
            global::DustyEngine.Debug.Log(
                $"[ValidateAndFixScenePaths] No changes needed",
                global::DustyEngine.Debug.LogLevel.Info,
                true);
        }

        if (SceneManager.CurrentScene != null && !string.IsNullOrWhiteSpace(SceneManager.CurrentScene.Path))
        {
            var currentScenePath = PathUtility.GetAbsolutePath(SceneManager.CurrentScene.Path);

            if (movedScenes.TryGetValue(currentScenePath, out var newPath))
            {
                SceneManager.CurrentScene.Path = EnsureRelativePath(newPath, projectRoot);
                global::DustyEngine.Debug.Log(
                    $"Updated CurrentScene path to: {SceneManager.CurrentScene.Path}",
                    global::DustyEngine.Debug.LogLevel.Info,
                    true);
            }
        }
    }

    private static string? FindSceneFileRecursive(string directory, string fileName)
    {
        try
        {
            var files = Directory.GetFiles(directory, fileName, SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
                return files[0];

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var found = FindSceneFileRecursive(subDir, fileName);
                if (found != null)
                    return found;
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (Exception ex)
        {
            global::DustyEngine.Debug.Log(
                $"Error searching directory {directory}: {ex.Message}",
                global::DustyEngine.Debug.LogLevel.Warning,
                false);
        }

        return null;
    }

    public static ProjectSettings? LoadProject(string path)
    {
        DustyEngine.ProjectFolderPath = path;

        global::DustyEngine.Debug.Log(
            $"[LoadProject] Loading project from: {path}",
            global::DustyEngine.Debug.LogLevel.Info,
            true);

        var _settings = DeserializeProjectSettings(DustyEngine.ProjectFolderPath);

        if (_settings == null)
            return null;

        ProjectSetiingPanel.ScenePaths = new List<string>(_settings.PathToScenes ?? []);

        global::DustyEngine.Debug.Log(
            $"[LoadProject] Loaded {ProjectSetiingPanel.ScenePaths.Count} scene paths into UI panel",
            global::DustyEngine.Debug.LogLevel.Info,
            true);

        ValidateAndFixScenePaths(_settings);

        return _settings;
    }

    public static void UpdateScenePath(ProjectSettings settings, string oldPath, string newPath)
    {
        if (settings?.PathToScenes == null)
            return;

        string projectRoot = Path.GetFullPath(DustyEngine.ProjectFolderPath);

        var cmp = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var oldFull = Path.GetFullPath(oldPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var newFull = Path.GetFullPath(newPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        for (int i = 0; i < settings.PathToScenes.Count; i++)
        {
            var scenePath = settings.PathToScenes[i];

            if (string.IsNullOrWhiteSpace(scenePath))
                continue;

            var sceneFull = Path.GetFullPath(PathUtility.GetAbsolutePath(scenePath))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(sceneFull, oldFull, cmp))
            {
                settings.PathToScenes[i] = EnsureRelativePath(newFull, projectRoot);
                global::DustyEngine.Debug.Log(
                    $"Updated scene path in project settings: {oldPath} -> {settings.PathToScenes[i]}",
                    global::DustyEngine.Debug.LogLevel.Info,
                    true);
            }
        }

        ProjectSetiingPanel.ScenePaths = settings.PathToScenes;

        SaveProjectSettings(settings);
    }
}
