using System.Text.Json;
using DustyEngine.Engine.Math.Vectors;
using DustyEngine.Scene;
using Editor.Panels.ProjectSetiingPanel;

namespace DustyEngine;

internal class ProjectSettings
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
        settings.PathToScenes = ProjectSetiingPanel.ScenePaths;
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

            PathToScenes = projectSettings.PathToScenes
                .Select(p => MakeRelative(projectRoot, p))
                .ToList(),

            PathToFragShader = MakeRelative(projectRoot, projectSettings.PathToFragShader),
            PathToVertShader = MakeRelative(projectRoot, projectSettings.PathToVertShader)
        };

        string json = JsonSerializer.Serialize(
            copy,
            new JsonSerializerOptions { WriteIndented = true }
        );

        File.WriteAllText(
            Path.Combine(projectRoot, "Settings/project_settings.json"),
            json
        );
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

        settings.PathToScenes = settings.PathToScenes
            .Select(p => ResolvePath(projectRoot, p))
            .ToList();

        settings.PathToFragShader = ResolvePath(projectRoot, settings.PathToFragShader);
        settings.PathToVertShader = ResolvePath(projectRoot, settings.PathToVertShader);

        return settings;
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


            var absolutePath = ResolvePath(projectRoot, scenePath);


            if (File.Exists(absolutePath))
            {
                validPaths.Add(scenePath);
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

                movedScenes[absolutePath] = foundPath;
                validPaths.Add(foundPath);
                pathsChanged = true;
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
            SaveProjectSettings(settings);

            global::DustyEngine.Debug.Log(
                $"Project settings updated: validated {validPaths.Count} scene paths",
                global::DustyEngine.Debug.LogLevel.Info,
                true);
        }

        if (SceneManager.CurrentScene != null && !string.IsNullOrWhiteSpace(SceneManager.CurrentScene.Path))
        {
            var currentScenePath = Path.GetFullPath(SceneManager.CurrentScene.Path);

            if (movedScenes.TryGetValue(currentScenePath, out var newPath))
            {
                SceneManager.CurrentScene.Path = newPath;
                global::DustyEngine.Debug.Log(
                    $"Updated CurrentScene path to: {newPath}",
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
        var _settings = DeserializeProjectSettings(DustyEngine.ProjectFolderPath);

        if (_settings == null)
            return null;

        ProjectSetiingPanel.ScenePaths = _settings.PathToScenes;

        ValidateAndFixScenePaths(_settings);

        return _settings;
    }


    public static void UpdateScenePath(ProjectSettings settings, string oldPath, string newPath)
    {
        if (settings?.PathToScenes == null)
            return;

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

            var sceneFull = Path.GetFullPath(scenePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(sceneFull, oldFull, cmp))
            {
                settings.PathToScenes[i] = newFull;
                global::DustyEngine.Debug.Log(
                    $"Updated scene path in project settings: {oldPath} -> {newPath}",
                    global::DustyEngine.Debug.LogLevel.Info,
                    true);
            }
        }

        ProjectSetiingPanel.ScenePaths = settings.PathToScenes;

        SaveProjectSettings(settings);
    }

    private static string MakeRelative(string projectRoot, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        try
        {
            string abs = ResolvePath(projectRoot, path);
            return Path.GetRelativePath(projectRoot, abs);
        }
        catch
        {
            return path;
        }
    }

    private static string ResolvePath(string projectRoot, string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return rawPath;

        if (rawPath.StartsWith("~"))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            rawPath = Path.Combine(home, rawPath.TrimStart('~', '/', '\\'));
        }

        rawPath = Environment.ExpandEnvironmentVariables(rawPath);

        if (Path.IsPathRooted(rawPath))
            return Path.GetFullPath(rawPath);

        return Path.GetFullPath(Path.Combine(projectRoot, rawPath));
    }
}
