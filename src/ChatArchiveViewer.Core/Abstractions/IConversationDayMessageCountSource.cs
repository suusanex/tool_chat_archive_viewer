namespace ChatArchiveViewer.Core.Abstractions;

public interface IConversationDayMessageCountSource
{
    Task<int> GetMessageCountAsync(string conversationId, DateOnly date, CancellationToken ct);
}
