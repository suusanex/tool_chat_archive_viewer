using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.Core.Abstractions;

public interface IArchiveFormatDetector
{
    Task<FormatDetectionResult> DetectAsync(IArchiveSource source, CancellationToken ct);
}
