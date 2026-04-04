using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.Core.Abstractions;

public interface IArchiveLoadService
{
    Task<ChatArchive> LoadAsync(
        IArchiveSource source,
        IArchiveFormatProvider provider,
        IProgress<ArchiveLoadProgress>? progress,
        CancellationToken ct);
}
