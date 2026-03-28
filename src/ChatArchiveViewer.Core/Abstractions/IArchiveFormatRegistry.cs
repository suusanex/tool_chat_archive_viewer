using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.Core.Abstractions;

public interface IArchiveFormatRegistry
{
    IReadOnlyList<IArchiveFormatProvider> GetAllProviders();

    IArchiveFormatProvider? GetProvider(string formatId);

    Task<IReadOnlyList<FormatDetectionResult>> DetectAllAsync(IArchiveSource source, CancellationToken ct);
}
