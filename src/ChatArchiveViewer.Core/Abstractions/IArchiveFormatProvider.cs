using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.Core.Abstractions;

public interface IArchiveFormatProvider
{
    string FormatId { get; }

    string DisplayName { get; }

    string Description { get; }

    IArchiveFormatDetector CreateDetector();

    IArchiveParser CreateParser();
}
