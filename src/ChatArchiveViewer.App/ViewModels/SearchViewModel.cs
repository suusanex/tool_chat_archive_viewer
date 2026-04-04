using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatArchiveViewer.App.Services;
using ChatArchiveViewer.Core.Services;

namespace ChatArchiveViewer.App.ViewModels;

public sealed partial class SearchViewModel : ViewModelBase
{
    private readonly IArchiveSessionService sessionService;
    private readonly SearchService searchService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string keyword = string.Empty;

    public ObservableCollection<SearchResult> Results { get; } = new();

    public SearchViewModel(IArchiveSessionService sessionService, SearchService searchService)
    {
        this.sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        this.searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    private bool CanSearch()
        => !string.IsNullOrWhiteSpace(Keyword) && sessionService.HasArchive;

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
        if (!sessionService.HasArchive || sessionService.Archive is null)
        {
            return;
        }

        var messages = await sessionService.LoadAllMessagesAsync(CancellationToken.None);
        var results = await searchService.SearchAsync(sessionService.Archive, messages, Keyword, CancellationToken.None);
        Results.Clear();
        foreach (var result in results)
        {
            Results.Add(result);
        }
    }
}
