using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.Core.Abstractions;

public interface IArchiveParser
{
    Task<ChatArchive> ParseAsync(IArchiveSource source, IProgress<ArchiveLoadProgress>? progress, CancellationToken ct);

    Task<IReadOnlyList<ChatMessage>> LoadMessagesAsync(
        IArchiveSource source,
        string conversationId,
        DateOnly? date,
        CancellationToken ct);
}
