using System.Text.Json;
using Microsoft.UI.Xaml;

namespace ChatArchiveViewer.App.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private const string ThemeKey = "app.theme";
    private const string PrivacyPolicyUrlValue = "https://example.com/privacy";
    private readonly string settingsFilePath;
    private readonly Dictionary<string, string> settings;

    public AppSettingsService()
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatArchiveViewer");
        Directory.CreateDirectory(settingsDirectory);
        settingsFilePath = Path.Combine(settingsDirectory, "settings.json");
        settings = LoadSettings(settingsFilePath);
    }

    public ElementTheme CurrentTheme
    {
        get
        {
            if (settings.TryGetValue(ThemeKey, out var raw))
            {
                return raw switch
                {
                    nameof(ElementTheme.Light) => ElementTheme.Light,
                    nameof(ElementTheme.Dark) => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }

            return ElementTheme.Default;
        }
    }

    public string PrivacyPolicyUrl => PrivacyPolicyUrlValue;

    public void SaveTheme(ElementTheme theme)
    {
        settings[ThemeKey] = theme.ToString();
        SaveSettings();
    }

    private static Dictionary<string, string> LoadSettings(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return loaded is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
    }

    private void SaveSettings()
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsFilePath, json);
    }
}
