using ChatArchiveViewer.Core.Abstractions;

namespace ChatArchiveViewer.App.Services;

public interface IArchiveOpenService
{
    Task<IArchiveSource?> OpenFolderAsync(CancellationToken ct);

    Task<IArchiveSource?> OpenZipAsync(CancellationToken ct);
}
