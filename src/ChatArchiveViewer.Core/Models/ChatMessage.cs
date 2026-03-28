namespace ChatArchiveViewer.Core.Models;

public sealed class ChatMessage
{
    public required string Id { get; init; }

    public required string ConversationId { get; init; }

    public string? ParticipantId { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required string Text { get; init; }

    public string? RawSubtype { get; init; }

    public string? ThreadId { get; init; }

    public bool IsThreadParent { get; init; }

    public int ReplyCount { get; init; }

    public bool IsEdited { get; init; }

    public DateTimeOffset? EditedAt { get; init; }

    public MessageType Type { get; init; } = MessageType.Normal;

    public IReadOnlyList<MessageAttachment> Attachments { get; init; } = Array.Empty<MessageAttachment>();

    public IReadOnlyList<MessageReaction> Reactions { get; init; } = Array.Empty<MessageReaction>();

    public IReadOnlyDictionary<string, string> ExtendedProperties { get; init; } =
        new Dictionary<string, string>();
}

public enum MessageType
{
    Normal,
    System,
    Unknown
}
