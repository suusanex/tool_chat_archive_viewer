namespace ChatArchiveViewer.Core.Models;

public sealed class Participant
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string? RealName { get; init; }
}
