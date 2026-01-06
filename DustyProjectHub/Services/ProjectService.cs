using System.Globalization;
using System.Text.Json;

namespace DustyProjectHub;

public sealed record ProjectInfo(
    string Name,
    double EngineVersion,
    string Path,
    DateTime LastOpened
);

public static class ProjectService
{
    public static ProjectInfo GetProjectInfo(string projectPath)
    {
        var json = File.ReadAllText(Path.Combine(projectPath, "Settings/project_settings.json"));

        using var doc = JsonDocument.Parse(json);

        var engineVersion = doc.RootElement.GetProperty("DustyEngineVersion").GetDouble();
        var name = doc.RootElement.GetProperty("ProjectName").GetString();

        Console.WriteLine(name + engineVersion);
        return new ProjectInfo(name ?? "Unknown", engineVersion, projectPath, DateTime.Now);
    }

    public static double LoadEngineVersionFromFile(string enginePath)
    {
        if (string.IsNullOrWhiteSpace(enginePath))
            return 0.0;

        var dir = Path.GetDirectoryName(enginePath);
        if (string.IsNullOrWhiteSpace(dir))
            return 0.0;

        var path = Path.Combine(dir, "engine_version.txt");

        if (!File.Exists(path)) return 0.0;

        var text = File.ReadAllText(path).Trim();
        return double.Parse(text, CultureInfo.InvariantCulture);
    }

    public static bool ValidateProjectPath(string projectPath, out string? errorMessage)
    {
        var settingsFile = Path.Combine(projectPath, "Settings/project_settings.json");

        if (!File.Exists(settingsFile))
        {
            errorMessage =
                $"Project settings file not found at:\n{settingsFile}\n\nPlease select a valid project folder.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}