using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Models;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.Core.Services;

public sealed class ArchiveLoadService : IArchiveLoadService
{
    private readonly ILogger<ArchiveLoadService> logger;

    public ArchiveLoadService(ILogger<ArchiveLoadService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatArchive> LoadAsync(
        IArchiveSource source,
        IArchiveFormatProvider provider,
        IProgress<ArchiveLoadProgress>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(provider);

        ct.ThrowIfCancellationRequested();
        logger.LogInformation("Archive load started. Format={FormatId}", provider.FormatId);

        try
        {
            var parser = provider.CreateParser();
            var archive = await parser.ParseAsync(source, progress, ct);
            logger.LogInformation(
                "Archive load completed. Conversations={ConversationCount} Participants={ParticipantCount} Messages={MessageCount}",
                archive.Conversations.Count,
                archive.Participants.Count,
                archive.Metadata.TotalMessageCount);
            return archive;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Archive load failed. Exception={Exception}", ex.ToString());
            throw;
        }
    }
}
