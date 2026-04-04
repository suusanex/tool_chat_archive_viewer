namespace ChatArchiveViewer.Core.Models;

public sealed class ArchiveMetadata
{
    public string? DisplayName { get; init; }

    public DateTimeOffset? ExportedAt { get; init; }

    public DateOnly? EarliestDate { get; init; }

    public DateOnly? LatestDate { get; init; }

    public int TotalMessageCount { get; init; }

    public IReadOnlyDictionary<string, string> ExtendedProperties { get; init; } =
        new Dictionary<string, string>();
}
