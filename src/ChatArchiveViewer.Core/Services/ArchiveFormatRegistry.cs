using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.Core.Services;

public sealed class ArchiveFormatRegistry : IArchiveFormatRegistry
{
    private readonly IReadOnlyList<IArchiveFormatProvider> providers;

    public ArchiveFormatRegistry(IEnumerable<IArchiveFormatProvider> providers)
    {
        this.providers = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));
    }

    public IReadOnlyList<IArchiveFormatProvider> GetAllProviders() => providers;

    public IArchiveFormatProvider? GetProvider(string formatId)
    {
        if (string.IsNullOrWhiteSpace(formatId))
        {
            return null;
        }

        return providers.FirstOrDefault(
            x => string.Equals(x.FormatId, formatId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<FormatDetectionResult>> DetectAllAsync(IArchiveSource source, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);

        var results = new List<FormatDetectionResult>(providers.Count);
        foreach (var provider in providers)
        {
            ct.ThrowIfCancellationRequested();
            var detector = provider.CreateDetector();
            var result = await detector.DetectAsync(source, ct);
            results.Add(result);
        }

        return results;
    }
}
