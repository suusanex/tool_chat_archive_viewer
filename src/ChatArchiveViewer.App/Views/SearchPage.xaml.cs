using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.App.Views;

public sealed partial class SearchPage : Page
{
    private readonly SearchViewModel searchViewModel;
    private readonly ILogger<SearchPage> logger;

    public SearchPage()
    {
        InitializeComponent();
        var services = ((App)Application.Current).Host.Services;
        searchViewModel = services.GetRequiredService<SearchViewModel>();
        logger = services.GetRequiredService<ILogger<SearchPage>>();
        ApplyLocalization();
        SearchResultsList.ItemsSource = searchViewModel.Results;
    }

    private async void OnSearchButtonClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        try
        {
            if (!searchViewModel.SearchCommand.CanExecute(null))
            {
                return;
            }

            await searchViewModel.SearchCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.Search.Failed"));
        }
    }

    private void OnSearchKeywordTextChanged(object sender, TextChangedEventArgs e)
    {
        _ = e;
        if (sender is TextBox textBox)
        {
            searchViewModel.Keyword = textBox.Text;
            SearchButton.IsEnabled = searchViewModel.SearchCommand.CanExecute(null);
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = LocalizedStrings.Get("Dialog.Error.Title"),
            Content = message,
            CloseButtonText = LocalizedStrings.Get("Dialog.Close"),
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void ApplyLocalization()
    {
        SearchKeywordBox.PlaceholderText = LocalizedStrings.Get("Search.Placeholder");
        SearchButton.Content = LocalizedStrings.Get("Search.Button");
    }
}
