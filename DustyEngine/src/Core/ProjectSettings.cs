using System.Text.Json;
using DustyEngine.Engine.Math.Vectors;

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
