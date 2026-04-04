namespace ChatArchiveViewer.CloudFetch.Models;

public sealed class CloudFetchProgress
{
    public required CloudFetchProgressStage Stage { get; init; }

    public string? Detail { get; init; }
}

public enum CloudFetchProgressStage
{
    Bootstrap,
    Authentication,
    Manifest,
    VersionCheck,
    Download,
    Verify,
    Commit,
    Completed
}
