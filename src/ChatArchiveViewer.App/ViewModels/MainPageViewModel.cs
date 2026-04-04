using CommunityToolkit.Mvvm.ComponentModel;
using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App.ViewModels;

public enum MainPageSection
{
    Browse,
    Search,
    Settings,
    About
}

public sealed partial class MainPageViewModel : ViewModelBase
{
    private readonly IArchiveSessionService archiveSessionService;

    [ObservableProperty]
    private MainPageSection selectedSection = MainPageSection.Browse;

    [ObservableProperty]
    private bool hasArchive;

    public MainPageViewModel(IArchiveSessionService archiveSessionService)
    {
        this.archiveSessionService = archiveSessionService ?? throw new ArgumentNullException(nameof(archiveSessionService));
        HasArchive = this.archiveSessionService.HasArchive;
    }

    public Type ResolveCurrentPageType()
        => SelectedSection switch
        {
            MainPageSection.Browse => typeof(Views.BrowsePage),
            MainPageSection.Search => typeof(Views.SearchPage),
            MainPageSection.Settings => typeof(Views.SettingsPage),
            MainPageSection.About => typeof(Views.AboutPage),
            _ => throw new InvalidOperationException($"Unsupported section: {SelectedSection}")
        };

    public void RefreshArchiveState()
    {
        HasArchive = archiveSessionService.HasArchive;
    }
}
