using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace DustyProjectHub;

public sealed class HubSettings
{
    public List<string> EnginePaths { get; set; } = [];
    public List<string> ProjectsPath { get; set; } = [];
}

public static class HubSettingsLoader
{
    public static HubSettings HubSettings { get; set; } = new();
    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "hub_settings.json");


    public static void Load()
    {
        if (!File.Exists(SettingsPath))
            HubSettings = new HubSettings();

        HubSettings = JsonSerializer.Deserialize<HubSettings>(File.ReadAllText(SettingsPath)) ?? new HubSettings();
    }

    public static void Save()
    {
        File.WriteAllText(
            SettingsPath,
            JsonSerializer.Serialize(HubSettings, new JsonSerializerOptions
            {
                WriteIndented = true
            })
        );
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

    public static string DetectTargetFramework(string? enginePath)
    {
        var dir = Path.GetDirectoryName(enginePath)!;
        var name = Path.GetFileNameWithoutExtension(enginePath);

        var runtimeConfig = Path.Combine(dir, $"{name}.runtimeconfig.json");

        if (!File.Exists(runtimeConfig))
            return "net6.0";

        using var doc = JsonDocument.Parse(
            File.ReadAllText(runtimeConfig)
        );

        return doc.RootElement
            .GetProperty("runtimeOptions")
            .GetProperty("tfm")
            .GetString()!;
    }

    public static string? FindEnginePathByVersion(double version)
    {
        var target = version.ToString("0.################", CultureInfo.InvariantCulture);

        return (from p in HubSettings.EnginePaths
            where !string.IsNullOrWhiteSpace(p) && File.Exists(p)
            let v = LoadEngineVersionFromFile(p)
            let vText = v.ToString("0.################", CultureInfo.InvariantCulture)
            where vText == target
            select p).FirstOrDefault();
    }
}