using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using DustyEngine;
using DustyEngine.Engine.Math.Vectors;
using DustyEngine.Scene;
using DustyProjectHub.UI.Windows;
using OpenTK.Graphics.Vulkan;
using SceneSystem.Scene;

namespace DustyProjectHub.Services;

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
        var projectInfo = await CreateNewProject.Show();

        if (projectInfo.Item1 is null)
            return;

        var projectPath = "";
        
        switch (projectInfo.Item2)
        {
            case ProjectTemplates.Empty3D: 
                projectPath = CreateEmpty3DProject(projectInfo);
                break;

            case ProjectTemplates.Empty2D:
                break;
        }

        await TryAddProjectAsync(projectPath, MainWindow.ShowErrorDialog, MainWindow.ShowInfoDialog);
    }

    private string CreateEmpty3DProject((ProjectInfo?, ProjectTemplates) projectInfo)
    {
        var projectPath = Path.Combine(projectInfo.Item1.Path, projectInfo.Item1.Name);
        
        Directory.CreateDirectory(projectPath);

        File.WriteAllText(
            Path.Combine(projectPath, $"{projectInfo.Item1.Name}.csproj"),
            $"""
             <Project Sdk="Microsoft.NET.Sdk">
               <PropertyGroup>
                 <OutputType>Library</OutputType>
                 <TargetFramework>{HubSettingsLoader.DetectTargetFramework(HubSettingsLoader.FindEnginePathByVersion(projectInfo.Item1.EngineVersion))}</TargetFramework>
                 <ImplicitUsings>enable</ImplicitUsings>
                 <Nullable>enable</Nullable>
               </PropertyGroup>
             </Project>
             """
        );

        var assetsDir = Path.Combine(projectPath, "Assets");
        Directory.CreateDirectory(assetsDir);
        
        var sceneDir = Path.Combine(assetsDir, "Scene");
        Directory.CreateDirectory(sceneDir);
        
        var scenePath = Path.Combine(sceneDir, "ExampleScene.json");
        var scene = new Scene
        {
            Name = "ExampleScene"
        };

        SceneLoader.SaveToFile(scene, scenePath);
            
        var shadersDir = Path.Combine(assetsDir, "shaders");
        Directory.CreateDirectory(shadersDir);
        
        var vertPath = Path.Combine(shadersDir, "shader.vert");
        var fragPath = Path.Combine(shadersDir, "shader.frag");
        
        File.WriteAllText(vertPath,
     """
     #version 330
     layout(location = 0) in vec3 aPosition;
     layout(location = 1) in vec4 aColor;
     
     uniform mat4 uModel;
     uniform mat4 uView;
     uniform mat4 uProjection;
     
     out vec4 vColor;
     
     void main()
     {
         gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
         vColor = aColor;
     }
     """
        );
        
        File.WriteAllText(fragPath,
            """
            #version 330
            
            in vec4 vColor;
            out vec4 outColor;
            
            void main()
            {
                outColor = vColor;
            }
            """
        );
        
        var settingsDir = Path.Combine(projectPath, "Settings");
        Directory.CreateDirectory(settingsDir);

        
        
        
        var projectSettings = new ProjectSettings
        {
            ProjectName = projectInfo.Item1.Name,
            Version = 1,
            DustyEngineVersion = projectInfo.Item1.EngineVersion,
            PathToScenes = [scenePath],
            PathToFragShader = fragPath,
            PathToVertShader = vertPath,
            Debug = false,
            LogLevel = 0,
            LogToConsole = false,
            LogToFile = true,
            ScreenSize = new Vector2i(800,600),
            Vsync = true
        };
        ProjectSettings.SerializeProjectSettings(projectSettings,projectPath);
        
   

        return projectPath;
    }


    public async Task AddProject()
    {
        var projectPath = await AddProjectDialog.Show();
        await TryAddProjectAsync(projectPath, MainWindow.ShowErrorDialog, MainWindow.ShowInfoDialog);
    }

    public async Task RemoveProject(ProjectInfo projectInfo)
    {
        var ok = await ConfirmDialog.Show("Confirm",
            $"Remove '{projectInfo.Name}' from list?\n\n(It will NOT delete files from disk.)");
        if (!ok) return;

        Projects.Remove(projectInfo);

        HubSettingsLoader.HubSettings.ProjectsPath.RemoveAll(p =>
            string.Equals(p, projectInfo.Path, StringComparison.OrdinalIgnoreCase));
        HubSettingsLoader.Save();
    }


    public static async void OnProjectClicked(ProjectInfo projectInfo)
    {
        var enginePaths = HubSettingsLoader.HubSettings.EnginePaths;

        if (enginePaths.Count == 0)
        {
            await MessageDialog.Show("Error", "No engines installed.");
            return;
        }

        var engines = enginePaths
            .Select(p => new
            {
                Path = p,
                Version = HubSettingsLoader.LoadEngineVersionFromFile(p)
            })
            .Where(e => e.Version > 0)
            .ToList();

        if (engines.Count == 0)
        {
            await MessageDialog.Show("Error", "No valid engines found.");
            return;
        }

        var exact = engines.FirstOrDefault(e => e.Version == projectInfo.EngineVersion);

        if (exact != null)
        {
            ProjectLauncher.LaunchProject(projectInfo, exact.Path);
            return;
        }

        var closest = engines.OrderBy(e => System.Math.Abs(e.Version - projectInfo.EngineVersion))
            .First();

        var ok = await ConfirmDialog.Show(
            "Engine not found",
            $"Exact engine version {projectInfo.EngineVersion} not found.\n" +
            $"Closest available: {closest.Version}\n\n" +
            $"Use it?"
        );

        if (!ok) return;

        ProjectLauncher.LaunchProject(projectInfo, closest.Path);
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

    public static bool IsVersionCompatible(double engineVersion, double projectVersion) =>
        System.Math.Abs(engineVersion - projectVersion) <= 0.0001;
}