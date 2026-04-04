using System.Text.Json;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.CloudFetch.Services;

public sealed class LocalCacheManager : ICacheManager
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ILogger<LocalCacheManager> logger;
    private readonly string statePath;
    private readonly string zipPath;
    private readonly string tempPath;
    private readonly string bootstrapConfigUrl;

    public LocalCacheManager(ILogger<LocalCacheManager> logger, string? cacheDirectory = null, string? bootstrapConfigUrl = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.bootstrapConfigUrl = bootstrapConfigUrl ?? string.Empty;
        CacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatArchiveViewer",
            "CloudCache");
        statePath = Path.Combine(CacheDirectory, "cache-state.json");
        zipPath = Path.Combine(CacheDirectory, "current.zip");
        tempPath = Path.Combine(CacheDirectory, "downloading.zip.tmp");
    }

    public string CacheDirectory { get; }

    public async Task<CacheState?> GetCurrentStateAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(statePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(statePath, ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var state = JsonSerializer.Deserialize<CacheState>(json, JsonOptions);
            if (state is null || string.IsNullOrWhiteSpace(state.Version) || string.IsNullOrWhiteSpace(state.Sha256))
            {
                return null;
            }

            return state;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read cache state. Exception={Exception}", ex.ToString());
            return null;
        }
    }

    public Task<string> GetTempDownloadPathAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Directory.CreateDirectory(CacheDirectory);
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        return Task.FromResult(tempPath);
    }

    public async Task CommitDownloadAsync(string tempPathValue, string version, string sha256, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tempPathValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        ct.ThrowIfCancellationRequested();

        Directory.CreateDirectory(CacheDirectory);
        if (!File.Exists(tempPathValue))
        {
            throw new FileNotFoundException("Temporary download file not found.", tempPathValue);
        }

        File.Move(tempPathValue, zipPath, overwrite: true);

        var state = new CacheState
        {
            Version = version,
            Sha256 = sha256,
            DownloadedAt = DateTimeOffset.UtcNow,
            BootstrapUrl = bootstrapConfigUrl
        };

        var stateTempPath = $"{statePath}.tmp";
        var json = JsonSerializer.Serialize(state, JsonOptions);
        await File.WriteAllTextAsync(stateTempPath, json, ct);

        File.Move(stateTempPath, statePath, overwrite: true);
    }

    public string? GetCurrentZipPath()
        => File.Exists(zipPath) ? zipPath : null;
}
