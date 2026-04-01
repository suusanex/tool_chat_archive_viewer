using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.App.Services;

public interface IArchiveSessionService : IAsyncDisposable
{
    event EventHandler? ArchiveChanged;

    ChatArchive? Archive { get; }

    bool HasArchive { get; }

    Task SetCurrentAsync(IArchiveSource source, IArchiveFormatProvider provider, ChatArchive archive);

    Task<IReadOnlyList<ChatMessage>> LoadMessagesAsync(string conversationId, DateOnly? date, CancellationToken ct);

    Task<IReadOnlyList<ChatMessage>> LoadAllMessagesAsync(CancellationToken ct);
}
