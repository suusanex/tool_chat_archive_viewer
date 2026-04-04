using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Abstractions;

public interface ICacheManager
{
    string CacheDirectory { get; }

    Task<CacheState?> GetCurrentStateAsync(CancellationToken ct);

    Task<string> GetTempDownloadPathAsync(CancellationToken ct);

    Task CommitDownloadAsync(string tempPath, string version, string sha256, CancellationToken ct);

    string? GetCurrentZipPath();
}
