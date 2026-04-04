namespace ChatArchiveViewer.Core.Models;

public sealed class MessageReaction
{
    public required string Name { get; init; }

    public int Count { get; init; }
}
