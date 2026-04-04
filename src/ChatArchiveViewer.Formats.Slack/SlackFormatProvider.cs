using ChatArchiveViewer.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.Formats.Slack;

public sealed class SlackFormatProvider : IArchiveFormatProvider
{
    private readonly ILoggerFactory loggerFactory;

    public SlackFormatProvider(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public string FormatId => SlackFormatConstants.FormatId;

    public string DisplayName => SlackFormatConstants.DisplayName;

    public string Description => "Unofficial local viewer format provider for Slack JSON export archives.";

    public IArchiveFormatDetector CreateDetector()
        => new SlackFormatDetector(loggerFactory.CreateLogger<SlackFormatDetector>());

    public IArchiveParser CreateParser()
        => new SlackArchiveParser(loggerFactory.CreateLogger<SlackArchiveParser>());
}
