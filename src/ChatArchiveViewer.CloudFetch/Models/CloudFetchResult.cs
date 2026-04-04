namespace ChatArchiveViewer.CloudFetch.Models;

public sealed class CloudFetchResult
{
    public required CloudFetchStatus Status { get; init; }

    public string? CachedZipPath { get; init; }

    public string? Version { get; init; }

    public string? ErrorMessage { get; init; }
}

public enum CloudFetchStatus
{
    None,
    FreshDownload,
    AlreadyUpToDate,
    StaleCache,
    NoCacheError
}
