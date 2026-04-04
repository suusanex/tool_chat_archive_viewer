namespace ChatArchiveViewer.Core.Abstractions;

public interface IConversationDateCountService
{
    Task<IReadOnlyDictionary<DateOnly, int>> LoadMonthCountsAsync(
        string conversationId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken ct);

    void ClearCache();
}
