namespace ChatArchiveViewer.CloudArchiveUpdater.Models;

public sealed class ArchiveUpdateResult
{
    public required Uri ArchiveUri { get; init; }

    public required Uri ManifestUri { get; init; }

    public required string Version { get; init; }

    public required string Sha256 { get; init; }
}
