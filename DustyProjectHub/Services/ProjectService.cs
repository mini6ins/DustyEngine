using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using DustyProjectHub.UI.Windows;

namespace DustyProjectHub;

public sealed record ProjectInfo(
    string Name,
    double EngineVersion,
    string Path,
    DateTime LastOpened
);

public class ProjectService
{
    public readonly ObservableCollection<ProjectInfo> Projects = [];

    public ProjectService()
    {
        var paths = HubSettingsLoader.HubSettings.ProjectsPath ?? [];

        foreach (var projectPath in paths.Distinct())
        {
            if (string.IsNullOrWhiteSpace(projectPath)) continue;
            if (!Directory.Exists(projectPath)) continue; 
            Projects.Add(GetProjectInfo(projectPath));
        }
    }

    public async Task CreateProject()
    {
        var projectPath = await CreateNewProject.Show();
        await TryAddProjectAsync(projectPath, MainWindow.ShowErrorDialog, MainWindow.ShowInfoDialog);
    }

    public async Task AddProject()
    {
        var projectPath = await AddProjectDialog.Show();
        await TryAddProjectAsync(projectPath, MainWindow.ShowErrorDialog, MainWindow.ShowInfoDialog);
    }

    public static async void OnProjectClicked(ProjectInfo projectInfo)
    {
        var enginePath = HubSettingsLoader.HubSettings.EnginePath;

        if (!ProjectLauncher.ValidateEnginePath(enginePath, out var errorMessage))
        {
            await MessageDialog.Show("Error", errorMessage!);
            return;
        }

        var engineVersion = HubSettingsLoader.LoadEngineVersionFromFile(enginePath);

        if (!ProjectLauncher.IsVersionCompatible(engineVersion, projectInfo.EngineVersion))
        {
            var ok = await ConfirmDialog.Show("Confirm",
                $"Project '{projectInfo.Name}' was created with another engine version.\nContinue anyway?"
            );

            if (!ok) return;
        }

        ProjectLauncher.LaunchProject(projectInfo, enginePath);
    }


    private async Task TryAddProjectAsync(string projectPath, Func<string, string, Task> showError,
        Func<string, string, Task> showInfo)
    {
        if (string.IsNullOrWhiteSpace(projectPath)) return;

        if (!ValidateProjectPath(projectPath, out var errorMessage))
        {
            await showError("Error", errorMessage!);
            return;
        }

        try
        {
            var projectInfo = GetProjectInfo(projectPath);

            if (Projects.Any(p => p.Path == projectPath))
            {
                await showInfo("Info", "This project is already in the list.");
                return;
            }

            Projects.Add(projectInfo);
            HubSettingsLoader.HubSettings.ProjectsPath.Add(projectPath);
            HubSettingsLoader.Save();
        }
        catch (Exception ex)
        {
            await showError("Error", $"Failed to load project:\n{ex.Message}");
        }
    }

    private static ProjectInfo GetProjectInfo(string projectPath)
    {
        var json = File.ReadAllText(Path.Combine(projectPath, "Settings/project_settings.json"));

        using var doc = JsonDocument.Parse(json);

        var engineVersion = doc.RootElement.GetProperty("DustyEngineVersion").GetDouble();
        var name = doc.RootElement.GetProperty("ProjectName").GetString();

        Console.WriteLine(name + engineVersion);
        return new ProjectInfo(name ?? "Unknown", engineVersion, projectPath, DateTime.Now);
    }

    private static bool ValidateProjectPath(string projectPath, out string? errorMessage)
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

internal static class ProjectLauncher
{
    public static void LaunchProject(ProjectInfo projectInfo, string enginePath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = enginePath,
            Arguments = $"\"{projectInfo.Path}\"",
            UseShellExecute = false
        });
    }

    public static bool ValidateEnginePath(string enginePath, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(enginePath))
        {
            errorMessage = "Engine path is not configured. Please set it in Settings.";
            return false;
        }

        if (!File.Exists(enginePath))
        {
            errorMessage = $"Engine not found at:\n{enginePath}\n\nPlease update the path in Settings.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static bool IsVersionCompatible(double engineVersion, double projectVersion) => System.Math.Abs(engineVersion - projectVersion) <= 0.0001;
}