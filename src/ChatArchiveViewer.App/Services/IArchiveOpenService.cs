using ChatArchiveViewer.Core.Abstractions;

namespace ChatArchiveViewer.App.Services;

public interface IArchiveOpenService
{
    Task<IArchiveSource?> OpenFolderAsync(CancellationToken ct);

    Task<IArchiveSource?> OpenFolderAsync(string? initialFolderPath, CancellationToken ct);

    Task<IArchiveSource?> OpenZipAsync(CancellationToken ct);

    Task<IArchiveSource?> OpenZipAsync(string? initialZipPath, CancellationToken ct);

    Task<IArchiveSource> OpenBundledSampleAsync(BundledSampleKind kind, CancellationToken ct);
}
