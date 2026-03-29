using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ChatArchiveViewer.App.Views;

public sealed partial class MainPage : Page
{
    private enum NarrowBrowseStep
    {
        Channels,
        Dates,
        Messages
    }

    private readonly ArchiveOverviewViewModel overviewViewModel;
    private readonly ArchiveBrowseViewModel browseViewModel;
    private readonly SearchViewModel searchViewModel;
    private readonly AboutViewModel aboutViewModel;
    private readonly SettingsViewModel settingsViewModel;
    private readonly IArchiveOpenService archiveOpenService;
    private readonly IBundledSampleLocator bundledSampleLocator;
    private readonly IArchiveFormatRegistry formatRegistry;
    private readonly IArchiveLoadService archiveLoadService;
    private readonly IArchiveSessionService archiveSessionService;
    private readonly IAppSettingsService appSettingsService;
    private readonly AppLaunchOptions launchOptions;
    private readonly ILogger<MainPage> logger;
    private NarrowBrowseStep narrowStep = NarrowBrowseStep.Channels;
    private bool launchOptionsHandled;

    public MainPage()
    {
        InitializeComponent();
        var services = ((App)Application.Current).Host.Services;
        overviewViewModel = services.GetRequiredService<ArchiveOverviewViewModel>();
        browseViewModel = services.GetRequiredService<ArchiveBrowseViewModel>();
        searchViewModel = services.GetRequiredService<SearchViewModel>();
        aboutViewModel = services.GetRequiredService<AboutViewModel>();
        settingsViewModel = services.GetRequiredService<SettingsViewModel>();
        archiveOpenService = services.GetRequiredService<IArchiveOpenService>();
        bundledSampleLocator = services.GetRequiredService<IBundledSampleLocator>();
        formatRegistry = services.GetRequiredService<IArchiveFormatRegistry>();
        archiveLoadService = services.GetRequiredService<IArchiveLoadService>();
        archiveSessionService = services.GetRequiredService<IArchiveSessionService>();
        appSettingsService = services.GetRequiredService<IAppSettingsService>();
        launchOptions = services.GetRequiredService<AppLaunchOptions>();
        logger = services.GetRequiredService<ILogger<MainPage>>();
        this.ApplyLocalization();

        UpdateThemeCombo();
        BindAbout();
        RefreshBrowseBindings();
        RootNavigation.SelectedItem = BrowseNavItem;
        ShowPane("browse");
        SizeChanged += OnMainPageSizeChanged;
        Loaded += OnMainPageLoaded;
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
        await OpenBundledSampleAsync(launchOptions.AutoLoadSample.Value);
    }

    private async void OnOpenSampleClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await OpenBundledSampleAsync(BundledSampleKind.Folder);
    }

    private async void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        await OpenArchiveAsync(isZip: false);
    }

    private async void OnOpenZipClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        await OpenArchiveAsync(isZip: true);
    }

    private async Task OpenArchiveAsync(bool isZip)
    {
        IArchiveSource? source = null;
        try
        {
            using var cts = new CancellationTokenSource();
            source = isZip
                ? await archiveOpenService.OpenZipAsync(bundledSampleLocator.SampleZipPath, cts.Token)
                : await archiveOpenService.OpenFolderAsync(bundledSampleLocator.SampleFolderPath, cts.Token);
            if (source is null)
            {
                return;
            }

            source = await LoadArchiveAsync(source, cts.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Open archive failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.Failed"));
        }
        finally
        {
            if (source is not null)
            {
                await source.DisposeAsync();
            }
        }
    }

    private async Task OpenBundledSampleAsync(BundledSampleKind kind)
    {
        IArchiveSource? source = null;
        try
        {
            using var cts = new CancellationTokenSource();
            source = await archiveOpenService.OpenBundledSampleAsync(kind, cts.Token);
            source = await LoadArchiveAsync(source, cts.Token);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Open bundled sample failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.Failed"));
        }
        finally
        {
            if (source is not null)
            {
                await source.DisposeAsync();
            }
        }
    }

    private async Task<IArchiveSource?> LoadArchiveAsync(IArchiveSource source, CancellationToken ct)
    {
        var detections = await formatRegistry.DetectAllAsync(source, ct);
        var best = detections
            .Where(x => x.IsDetected)
            .OrderByDescending(x => x.Confidence)
            .FirstOrDefault();

        if (best is null)
        {
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.NoSupportedFormat"));
            return source;
        }

        var provider = formatRegistry.GetProvider(best.FormatId)
            ?? throw new InvalidOperationException($"Provider not found: {best.FormatId}");
        var archive = await archiveLoadService.LoadAsync(source, provider, progress: null, ct);
        await archiveSessionService.SetCurrentAsync(source, provider, archive);

        overviewViewModel.SetArchive(archive);
        await browseViewModel.RefreshFromSessionAsync();
        searchViewModel.SearchCommand.NotifyCanExecuteChanged();
        RootNavigation.SelectedItem = BrowseNavItem;
        ShowPane("browse");
        RefreshBrowseBindings();
        return null;
    }

    private async void OnConversationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _ = e;
        if (sender is ListView list && list.SelectedItem is Conversation selected)
        {
            await browseViewModel.SelectConversationAsync(selected);
            if (browseViewModel.IsNarrowLayout)
            {
                narrowStep = NarrowBrowseStep.Dates;
            }

            RefreshBrowseBindings();
        }
    }

    private async void OnDateItemClick(object sender, RoutedEventArgs e)
    {
        _ = e;
        if (sender is Button button && button.Tag is DateItem selected)
        {
            await browseViewModel.SelectDateAsync(selected);
            if (browseViewModel.IsNarrowLayout)
            {
                narrowStep = NarrowBrowseStep.Messages;
            }

            RefreshBrowseBindings();
        }
    }

    private async void OnSearchButtonClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        try
        {
            if (!searchViewModel.SearchCommand.CanExecute(null))
            {
                return;
            }

            await searchViewModel.SearchCommand.ExecuteAsync(null);
            SearchResultsList.ItemsSource = searchViewModel.Results;
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

    private async void OnOpenPrivacyPolicyClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        if (!aboutViewModel.OpenPrivacyPolicyCommand.CanExecute(null))
        {
            return;
        }

        try
        {
            await aboutViewModel.OpenPrivacyPolicyCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Open privacy policy failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.PrivacyPolicy.OpenFailed"));
        }
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _ = e;
        if (sender is not ComboBox combo)
        {
            return;
        }

        settingsViewModel.SelectedTheme = combo.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    private void OnSaveSettingsClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        settingsViewModel.SaveThemeCommand.Execute(null);
        if (((App)Application.Current).MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = settingsViewModel.SelectedTheme;
        }

        BindAbout();
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        _ = sender;
        if (args.SelectedItemContainer == BrowseNavItem)
        {
            ShowPane("browse");
        }
        else if (args.SelectedItemContainer == SearchNavItem)
        {
            ShowPane("search");
        }
        else if (args.SelectedItemContainer == SettingsNavItem)
        {
            ShowPane("settings");
        }
        else if (args.SelectedItemContainer == AboutNavItem)
        {
            ShowPane("about");
        }
    }

    private void ShowPane(string pane)
    {
        BrowseRoot.Visibility = pane == "browse" ? Visibility.Visible : Visibility.Collapsed;
        SearchRoot.Visibility = pane == "search" ? Visibility.Visible : Visibility.Collapsed;
        SettingsRoot.Visibility = pane == "settings" ? Visibility.Visible : Visibility.Collapsed;
        AboutRoot.Visibility = pane == "about" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshBrowseBindings()
    {
        var hasArchive = archiveSessionService.HasArchive;
        ArchiveNameText.Text = overviewViewModel.ArchiveName;
        ArchiveSummaryText.Text = $"{overviewViewModel.FormatDisplay} / {overviewViewModel.Summary}";
        BreadcrumbText.Text = browseViewModel.Breadcrumb;
        ConversationList.ItemsSource = browseViewModel.Conversations;
        DateYearItemsControl.ItemsSource = browseViewModel.DateYears;
        MessageContextText.Text = browseViewModel.MessageList.ContextTitle;
        MessageListView.ItemsSource = browseViewModel.MessageList.Messages;
        NarrowBackButton.IsEnabled = narrowStep != NarrowBrowseStep.Channels;
        NarrowStepTitleText.Text = narrowStep switch
        {
            NarrowBrowseStep.Channels => LocalizedStrings.Get("Browse.NarrowStep.Channels"),
            NarrowBrowseStep.Dates => LocalizedStrings.Get("Browse.NarrowStep.Dates"),
            _ => LocalizedStrings.Get("Browse.NarrowStep.Messages")
        };
        SearchNavItem.Visibility = hasArchive ? Visibility.Visible : Visibility.Collapsed;
        ArchiveSummaryPanel.Visibility = hasArchive ? Visibility.Visible : Visibility.Collapsed;
        BrowseContentPanel.Visibility = hasArchive ? Visibility.Visible : Visibility.Collapsed;
        ApplyBrowseLayoutMode();
    }

    private void BindAbout()
    {
        AboutAppNameText.Text = aboutViewModel.AppName;
        AboutVersionText.Text = $"{LocalizedStrings.Get("About.VersionPrefix")}{aboutViewModel.Version}";
        AboutDisclaimerText.Text = aboutViewModel.Disclaimer;
        AboutPrivacySummaryText.Text = aboutViewModel.PrivacySummary;
        AboutFormatsText.Text = aboutViewModel.SupportedFormats;
        OpenPrivacyPolicyButton.IsEnabled = aboutViewModel.OpenPrivacyPolicyCommand.CanExecute(null);
    }

    private void UpdateThemeCombo()
    {
        ThemeComboBox.SelectedIndex = settingsViewModel.SelectedTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };
    }

    private void OnMainPageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _ = sender;
        var width = e.NewSize.Width;
        var narrow = width < 600;
        browseViewModel.IsNarrowLayout = narrow;
        if (!narrow)
        {
            narrowStep = NarrowBrowseStep.Messages;
        }

        if (width >= 900)
        {
            ChannelColumn.Width = new GridLength(260);
            DateColumn.Width = new GridLength(220);
        }
        else if (width >= 600)
        {
            ChannelColumn.Width = new GridLength(200);
            DateColumn.Width = new GridLength(180);
        }

        ApplyBrowseLayoutMode();
    }

    private void OnNarrowBackClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        if (!browseViewModel.IsNarrowLayout)
        {
            return;
        }

        narrowStep = narrowStep switch
        {
            NarrowBrowseStep.Messages => NarrowBrowseStep.Dates,
            NarrowBrowseStep.Dates => NarrowBrowseStep.Channels,
            _ => NarrowBrowseStep.Channels
        };

        if (narrowStep == NarrowBrowseStep.Dates)
        {
            browseViewModel.NavigateToChannelOnly();
        }

        RefreshBrowseBindings();
    }

    private void ApplyBrowseLayoutMode()
    {
        if (!browseViewModel.IsNarrowLayout)
        {
            NarrowStepBar.Visibility = Visibility.Collapsed;
            ChannelPane.Visibility = Visibility.Visible;
            DatePane.Visibility = Visibility.Visible;
            MessagePane.Visibility = Visibility.Visible;
            return;
        }

        NarrowStepBar.Visibility = Visibility.Visible;
        ChannelPane.Visibility = narrowStep == NarrowBrowseStep.Channels ? Visibility.Visible : Visibility.Collapsed;
        DatePane.Visibility = narrowStep == NarrowBrowseStep.Dates ? Visibility.Visible : Visibility.Collapsed;
        MessagePane.Visibility = narrowStep == NarrowBrowseStep.Messages ? Visibility.Visible : Visibility.Collapsed;
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
        BrowseNavItem.Content = LocalizedStrings.Get("Nav.Browse");
        SearchNavItem.Content = LocalizedStrings.Get("Nav.Search");
        SettingsNavItem.Content = LocalizedStrings.Get("Nav.Settings");
        AboutNavItem.Content = LocalizedStrings.Get("Nav.About");

        EntryActionTitleText.Text = LocalizedStrings.Get("Entry.OpenGroup.Title");
        EntryActionDescriptionText.Text = LocalizedStrings.Get("Entry.OpenGroup.Description");
        EntryActionHintText.Text = LocalizedStrings.Get("Entry.OpenGroup.Hint");
        OpenSampleButton.Content = LocalizedStrings.Get("Entry.OpenSample");
        OpenFolderButton.Content = LocalizedStrings.Get("Browse.OpenFolder");
        OpenZipButton.Content = LocalizedStrings.Get("Browse.OpenZip");
        NarrowBackButton.Content = LocalizedStrings.Get("Browse.Back");
        ChannelsHeaderText.Text = LocalizedStrings.Get("Browse.Channels");
        DatesHeaderText.Text = LocalizedStrings.Get("Browse.Dates");
        if (string.IsNullOrWhiteSpace(MessageContextText.Text))
        {
            MessageContextText.Text = LocalizedStrings.Get("Browse.Messages");
        }

        SearchKeywordBox.PlaceholderText = LocalizedStrings.Get("Search.Placeholder");
        SearchButton.Content = LocalizedStrings.Get("Search.Button");

        ThemeLabelText.Text = LocalizedStrings.Get("SettingsThemeLabel");
        ThemeComboBox.Items.Clear();
        ThemeComboBox.Items.Add(LocalizedStrings.Get("SettingsThemeSystem"));
        ThemeComboBox.Items.Add(LocalizedStrings.Get("SettingsThemeLight"));
        ThemeComboBox.Items.Add(LocalizedStrings.Get("SettingsThemeDark"));
        SaveSettingsButton.Content = LocalizedStrings.Get("Settings.Save");

        AboutLicensePlaceholderText.Text = LocalizedStrings.Get("About.LicensePlaceholder");
        OpenPrivacyPolicyButton.Content = LocalizedStrings.Get("About.OpenPrivacyPolicy");
        browseViewModel.RebuildLocalizedText();
    }
}
