using System.Text.Json;

namespace DustyProjectHub;

public sealed class HubSettings
{
    public string EnginePath { get; set; } = "";
}

public static class HubSettingsLoader
{
    private static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "hub_settings.json");

    public static HubSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new HubSettings();

        return JsonSerializer.Deserialize<HubSettings>(
            File.ReadAllText(SettingsPath)
        ) ?? new HubSettings();
    }

    public static void Save(HubSettings settings)
    {
        File.WriteAllText(
            SettingsPath,
            JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            })
        );
    }
}