namespace ChatArchiveViewer.Core.Models;

public sealed class ArchiveLoadProgress
{
    public required string Phase { get; init; }

    public int? Current { get; init; }

    public int? Total { get; init; }
}
