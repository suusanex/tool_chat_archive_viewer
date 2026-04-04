namespace ChatArchiveViewer.Core.Models;

public sealed class FormatDetectionResult
{
    public required string FormatId { get; init; }

    public required string FormatDisplayName { get; init; }

    public required bool IsDetected { get; init; }

    public double Confidence { get; init; }
}
