using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.App.Services;

public sealed class ArchiveSessionService : IArchiveSessionService, IConversationDayMessageCountSource
{
    private IArchiveSource? source;
    private IArchiveFormatProvider? provider;

    public event EventHandler? ArchiveChanged;

    public ChatArchive? Archive { get; private set; }

    public bool HasArchive => Archive is not null && source is not null && provider is not null;

    public async Task SetCurrentAsync(IArchiveSource newSource, IArchiveFormatProvider newProvider, ChatArchive archive)
    {
        ArgumentNullException.ThrowIfNull(newSource);
        ArgumentNullException.ThrowIfNull(newProvider);
        ArgumentNullException.ThrowIfNull(archive);

        if (source is not null)
        {
            await source.DisposeAsync();
        }

        source = newSource;
        provider = newProvider;
        Archive = archive;
        ArchiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<IReadOnlyList<ChatMessage>> LoadMessagesAsync(string conversationId, DateOnly? date, CancellationToken ct)
    {
        if (source is null || provider is null)
        {
            throw new InvalidOperationException("Archive is not loaded.");
        }

        return await provider.CreateParser().LoadMessagesAsync(source, conversationId, date, ct);
    }

    public async Task<int> GetMessageCountAsync(string conversationId, DateOnly date, CancellationToken ct)
    {
        var messages = await LoadMessagesAsync(conversationId, date, ct);
        return messages.Count;
    }

    public async Task<IReadOnlyList<ChatMessage>> LoadAllMessagesAsync(CancellationToken ct)
    {
        if (Archive is null)
        {
            return Array.Empty<ChatMessage>();
        }

        var all = new List<ChatMessage>();
        foreach (var conversation in Archive.Conversations)
        {
            ct.ThrowIfCancellationRequested();
            var messages = await LoadMessagesAsync(conversation.Id, null, ct);
            all.AddRange(messages);
        }

        return all;
    }

    public async ValueTask DisposeAsync()
    {
        if (source is not null)
        {
            await source.DisposeAsync();
            source = null;
        }
    }
}
