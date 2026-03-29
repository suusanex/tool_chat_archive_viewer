using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace ChatArchiveViewer.App.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private const string ThemeKey = "app.theme";
    private const string PrivacyPolicyUrlValue = "https://example.com/privacy";
    private readonly ILogger<AppSettingsService> logger;
    private readonly string settingsFilePath;
    private readonly Dictionary<string, string> settings;

    public AppSettingsService(ILogger<AppSettingsService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    private Dictionary<string, string> LoadSettings(string path)
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return loaded is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Settings file is invalid. Path={Path} Exception={Exception}", path, ex.ToString());
            throw new InvalidOperationException($"Settings file is invalid: {path}", ex);
        }
    }

    private void SaveSettings()
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsFilePath, json);
    }
}
