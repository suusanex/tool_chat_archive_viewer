namespace ChatArchiveViewer.Core.Models;

public sealed class Conversation
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string? Topic { get; init; }

    public string? Purpose { get; init; }

    public ConversationType Type { get; init; } = ConversationType.Channel;

    public IReadOnlyList<DateOnly> AvailableDates { get; init; } = Array.Empty<DateOnly>();

    public int MessageCount { get; init; }
}

public enum ConversationType
{
    Channel,
    DirectMessage,
    Group,
    Other
}
