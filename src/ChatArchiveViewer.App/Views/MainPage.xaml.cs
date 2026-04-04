using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.App.Views;

public sealed partial class MainPage : Page
{
    private readonly MainPageViewModel viewModel;
    private readonly IArchiveSessionService archiveSessionService;
    private readonly IArchiveWorkflowService archiveWorkflowService;
    private readonly AppLaunchOptions launchOptions;
    private readonly ILogger<MainPage> logger;
    private bool launchOptionsHandled;

    public MainPage()
    {
        InitializeComponent();
        var services = ((App)Application.Current).Host.Services;
        viewModel = services.GetRequiredService<MainPageViewModel>();
        archiveSessionService = services.GetRequiredService<IArchiveSessionService>();
        archiveWorkflowService = services.GetRequiredService<IArchiveWorkflowService>();
        launchOptions = services.GetRequiredService<AppLaunchOptions>();
        logger = services.GetRequiredService<ILogger<MainPage>>();
        archiveSessionService.ArchiveChanged += OnArchiveChanged;
        Loaded += OnMainPageLoaded;
        Unloaded += OnMainPageUnloaded;

        ApplyLocalization();
        RootNavigation.SelectedItem = BrowseNavItem;
        NavigateToSelectedSection();
        UpdateSearchMenuVisibility();
    }

    public void ToggleNavigationPane()
    {
        RootNavigation.IsPaneOpen = !RootNavigation.IsPaneOpen;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        _ = sender;
        if (args.SelectedItemContainer == BrowseNavItem)
        {
            viewModel.SelectedSection = MainPageSection.Browse;
        }
        else if (args.SelectedItemContainer == SearchNavItem)
        {
            viewModel.SelectedSection = MainPageSection.Search;
        }
        else if (args.SelectedItemContainer == SettingsNavItem)
        {
            viewModel.SelectedSection = MainPageSection.Settings;
        }
        else if (args.SelectedItemContainer == AboutNavItem)
        {
            viewModel.SelectedSection = MainPageSection.About;
        }

        NavigateToSelectedSection();
        UpdateSearchMenuVisibility();
    }

    private void NavigateToSelectedSection()
    {
        var pageType = viewModel.ResolveCurrentPageType();
        if (ContentFrame.CurrentSourcePageType == pageType)
        {
            return;
        }

        ContentFrame.Navigate(pageType);
    }

    private void ApplyLocalization()
    {
        BrowseNavItem.Content = LocalizedStrings.Get("Nav.Browse");
        SearchNavItem.Content = LocalizedStrings.Get("Nav.Search");
        SettingsNavItem.Content = LocalizedStrings.Get("Nav.Settings");
        AboutNavItem.Content = LocalizedStrings.Get("Nav.About");
    }

    private void UpdateSearchMenuVisibility()
    {
        viewModel.RefreshArchiveState();
        SearchNavItem.Visibility = viewModel.HasArchive ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnMainPageLoaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (launchOptionsHandled || launchOptions.AutoLoadSample is null)
        {
            return;
        }

        launchOptionsHandled = true;
        try
        {
            using var cts = new CancellationTokenSource();
            await archiveWorkflowService.OpenBundledSampleAsync(launchOptions.AutoLoadSample.Value, cts.Token);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "Bundled sample file is missing. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenSample.Missing"));
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError(ex, "Bundled sample directory is missing. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenSample.Missing"));
        }
        catch (UnsupportedArchiveFormatException ex)
        {
            logger.LogError(ex, "Open bundled sample failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.NoSupportedFormat"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Open bundled sample failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.Failed"));
        }
    }

    private void OnArchiveChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateSearchMenuVisibility();
            return;
        }

        _ = DispatcherQueue.TryEnqueue(UpdateSearchMenuVisibility);
    }

    private void OnMainPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        archiveSessionService.ArchiveChanged -= OnArchiveChanged;
        Loaded -= OnMainPageLoaded;
        Unloaded -= OnMainPageUnloaded;
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
}
