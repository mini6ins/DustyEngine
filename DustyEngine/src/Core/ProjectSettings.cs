using System.Text.Json;

namespace DustyEngine;

public class ProjectSettings
{
    public string ProjectName { get; set; }
    public float Version { get; set; }
    public List<string> PathToScenes { get; set; }
    public string PathToFragShader { get; set; }
    public string PathToVertShader { get; set; }
    public bool Debug { get; set; }
    public Debug.LogLevel LogLevel { get; set; }
    public bool LogToConsole { get; set; }
    public bool LogToFile { get; set; }
    public Vector2 ScreenSize { get; set; }
    public bool Vsync { get; set; }


    public static void SerializeProjectSettings(ProjectSettings projectSettings, string projectFolderPath)
    {
        string json = JsonSerializer.Serialize(projectSettings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(projectFolderPath, "Settings/project_settings.json"), json);
    }
    
    public static ProjectSettings DeserializeProjectSettings(string projectFolderPath)
    {
        string filePath = Path.Combine(projectFolderPath, "Settings/project_settings.json");

        if (!File.Exists(filePath))
        {
            global::DustyEngine.Debug.Log("Project settings file not found", global::DustyEngine.Debug.LogLevel.FatalError, true);
            
            return null!;
        }

        string fileContent = File.ReadAllText(filePath);
        var settings = JsonSerializer.Deserialize<ProjectSettings>(fileContent);

        if (settings == null)
        {
            global::DustyEngine.Debug.Log("Project settings could not be loaded", global::DustyEngine.Debug.LogLevel.FatalError, true);
            return null!;
        }

        return settings;
    }
}