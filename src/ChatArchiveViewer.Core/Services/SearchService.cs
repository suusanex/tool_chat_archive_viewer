using ChatArchiveViewer.Core.Models;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.Core.Services;

public sealed class SearchService
{
    private readonly ILogger<SearchService> logger;

    public SearchService(ILogger<SearchService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        ChatArchive archive,
        IReadOnlyList<ChatMessage> messages,
        string keyword,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(keyword);
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            logger.LogInformation("Search skipped because keyword is empty.");
            return Task.FromResult((IReadOnlyList<SearchResult>)Array.Empty<SearchResult>());
        }

        var conversationMap = archive.Conversations.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var normalized = keyword.Trim();
        var results = messages
            .Where(message => message.Text.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .Select(
                message =>
                {
                    var conversationName = conversationMap.TryGetValue(message.ConversationId, out var conversation)
                        ? conversation.DisplayName
                        : message.ConversationId;

                    return new SearchResult
                    {
                        ConversationId = message.ConversationId,
                        ConversationDisplayName = conversationName,
                        Date = DateOnly.FromDateTime(message.Timestamp.UtcDateTime),
                        Message = message
                    };
                })
            .OrderBy(x => x.ConversationDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Message.Timestamp)
            .ToArray();

        logger.LogInformation("Search completed. ResultCount={ResultCount}", results.Length);

        return Task.FromResult((IReadOnlyList<SearchResult>)results);
    }
}
