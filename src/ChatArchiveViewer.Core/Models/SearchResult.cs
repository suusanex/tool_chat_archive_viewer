namespace ChatArchiveViewer.Core.Models;

public sealed class SearchResult
{
    public required string ConversationId { get; init; }

    public required string ConversationDisplayName { get; init; }

    public required DateOnly Date { get; init; }

    public required ChatMessage Message { get; init; }
}
