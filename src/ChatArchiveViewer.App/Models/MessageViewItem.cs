using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.App.Models;

public sealed class MessageViewItem
{
    public required ChatMessage Message { get; init; }

    public required string ParticipantDisplayName { get; init; }

    public required string DisplayTimestamp { get; init; }

    public required string Text { get; init; }
}
