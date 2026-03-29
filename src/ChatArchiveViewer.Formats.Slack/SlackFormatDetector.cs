using System.Globalization;
using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Models;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.Formats.Slack;

public sealed class SlackFormatDetector : IArchiveFormatDetector
{
    private readonly ILogger<SlackFormatDetector> logger;

    public SlackFormatDetector(ILogger<SlackFormatDetector> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FormatDetectionResult> DetectAsync(IArchiveSource source, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ct.ThrowIfCancellationRequested();

        var hasChannels = await source.FileExistsAsync("channels.json", ct);
        var hasUsers = await source.FileExistsAsync("users.json", ct);
        var directories = await source.GetDirectoriesAsync(string.Empty, ct);

        var hasDailyJson = false;
        foreach (var directory in directories)
        {
            ct.ThrowIfCancellationRequested();
            if (directory.Contains("content_flags", StringComparison.OrdinalIgnoreCase) ||
                directory.StartsWith("FC:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var files = await source.GetFilesAsync(directory, "*.json", ct);
            if (files.Any(file => IsDailyJsonFile(Path.GetFileName(file))))
            {
                hasDailyJson = true;
                break;
            }
        }

        var detected = (hasChannels || hasUsers) && hasDailyJson;
        var confidence = detected
            ? (hasChannels && hasUsers ? 0.95 : 0.75)
            : (hasChannels || hasUsers ? 0.25 : 0.0);

        logger.LogInformation(
            "Slack detection completed. Detected={Detected} Confidence={Confidence} HasChannels={HasChannels} HasUsers={HasUsers} HasDailyJson={HasDailyJson}",
            detected,
            confidence,
            hasChannels,
            hasUsers,
            hasDailyJson);

        return new FormatDetectionResult
        {
            FormatId = SlackFormatConstants.FormatId,
            FormatDisplayName = SlackFormatConstants.DisplayName,
            IsDetected = detected,
            Confidence = confidence
        };
    }

    private static bool IsDailyJsonFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return DateOnly.TryParseExact(
            Path.GetFileNameWithoutExtension(fileName),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }
}
