using System.Text.Json;

namespace ChatArchiveViewer.CloudArchiveUpdater.Services;

public static class BootstrapConfigUrlResolver
{
    private const string AppSettingsFileName = "appsettings.json";
    private const string EnvironmentVariableName = "CloudFetch__BootstrapConfigUrl";

    public static string? Resolve()
    {
        var configuredValue = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return CloudArchiveUpdaterConstants.GetBootstrapConfigUrl(configuredValue);
        }

        foreach (var path in GetConfigurationFilePaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("CloudFetch", out var cloudFetch))
            {
                continue;
            }

            if (!TryGetBootstrapConfigUrl(cloudFetch, out var bootstrapConfigUrl))
            {
                continue;
            }

            return CloudArchiveUpdaterConstants.GetBootstrapConfigUrl(bootstrapConfigUrl);
        }

        return CloudArchiveUpdaterConstants.GetBootstrapConfigUrl(null);
    }

    private static bool TryGetBootstrapConfigUrl(JsonElement cloudFetch, out string? bootstrapConfigUrl)
    {
        if (cloudFetch.TryGetProperty("BootstrapConfigUrl", out var value)
            || cloudFetch.TryGetProperty("bootstrapConfigUrl", out value))
        {
            bootstrapConfigUrl = value.GetString();
            return true;
        }

        bootstrapConfigUrl = null;
        return false;
    }

    private static IEnumerable<string> GetConfigurationFilePaths()
    {
        var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, AppSettingsFileName);
        yield return baseDirectoryPath;

        var currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), AppSettingsFileName);
        if (!string.Equals(currentDirectoryPath, baseDirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            yield return currentDirectoryPath;
        }
    }
}
