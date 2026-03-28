using CommunityToolkit.Mvvm.ComponentModel;

namespace ChatArchiveViewer.App.ViewModels;

public sealed partial class ArchiveOverviewViewModel : ViewModelBase
{
    [ObservableProperty]
    private string archiveName = LocalizedStrings.Get("Overview.NoArchive");

    [ObservableProperty]
    private string formatDisplay = LocalizedStrings.Get("Overview.DefaultFormat", "-");

    [ObservableProperty]
    private string summary = LocalizedStrings.Get("Overview.DefaultSummary");

    public void SetArchive(ChatArchive archive)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArchiveName = archive.Metadata.DisplayName ?? LocalizedStrings.Get("Overview.DefaultArchiveName");
        FormatDisplay = archive.FormatDisplayName;
        Summary = LocalizedStrings.Format(
            "Overview.SummaryFormat",
            archive.Conversations.Count,
            archive.Participants.Count,
            archive.Metadata.TotalMessageCount);
    }
}
