using System.Globalization;

namespace ChatArchiveViewer.CloudArchiveUpdater.Services;

public interface IManifestVersionGenerator
{
    string CreateNextVersion(string currentVersion, DateTimeOffset publishedAtUtc);
}

public sealed class ManifestVersionGenerator : IManifestVersionGenerator
{
    public string CreateNextVersion(string currentVersion, DateTimeOffset publishedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentVersion);

        var prefix = publishedAtUtc.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var expectedPrefix = $"{prefix}-v";
        if (currentVersion.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(currentVersion[expectedPrefix.Length..], CultureInfo.InvariantCulture, out var currentNumber) &&
            currentNumber >= 1)
        {
            return $"{prefix}-v{currentNumber + 1}";
        }

        return $"{prefix}-v1";
    }
}
