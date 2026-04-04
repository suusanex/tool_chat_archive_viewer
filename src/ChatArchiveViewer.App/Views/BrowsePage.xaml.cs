using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Navigation;
using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.App.Views;

public sealed partial class BrowsePage : Page
{
    private enum NarrowBrowseStep
    {
        Channels,
        Dates,
        Messages
    }

    private readonly ArchiveOverviewViewModel overviewViewModel;
    private readonly ArchiveBrowseViewModel browseViewModel;
    private readonly IArchiveSessionService archiveSessionService;
    private readonly IArchiveWorkflowService archiveWorkflowService;
    private readonly CloudFetchFeatureOptions cloudFetchFeatureOptions;
    private readonly ILogger<BrowsePage> logger;
    private NarrowBrowseStep narrowStep = NarrowBrowseStep.Channels;
    private bool isSessionEventHooked;

    public BrowsePage()
    {
        InitializeComponent();
        var services = ((App)Application.Current).Host.Services;
        overviewViewModel = services.GetRequiredService<ArchiveOverviewViewModel>();
        browseViewModel = services.GetRequiredService<ArchiveBrowseViewModel>();
        archiveSessionService = services.GetRequiredService<IArchiveSessionService>();
        archiveWorkflowService = services.GetRequiredService<IArchiveWorkflowService>();
        cloudFetchFeatureOptions = services.GetRequiredService<CloudFetchFeatureOptions>();
        logger = services.GetRequiredService<ILogger<BrowsePage>>();

        ApplyLocalization();
        RefreshBrowseBindings();
        SizeChanged += OnBrowsePageSizeChanged;
        Unloaded += OnBrowsePageUnloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (!isSessionEventHooked)
        {
            archiveSessionService.ArchiveChanged += OnArchiveChanged;
            isSessionEventHooked = true;
        }

        RefreshBrowseBindings();
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

    private async void OnOpenCloudClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await OpenCloudArchiveAsync();
    }

    private async Task OpenArchiveAsync(bool isZip)
    {
        try
        {
            using var cts = new CancellationTokenSource();
            await archiveWorkflowService.OpenArchiveAsync(isZip, cts.Token);
            RefreshBrowseBindings();
        }
        catch (UnsupportedArchiveFormatException ex)
        {
            logger.LogError(ex, "Open archive failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.NoSupportedFormat"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Open archive failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.Failed"));
        }
    }

    private async Task OpenBundledSampleAsync(BundledSampleKind kind)
    {
        try
        {
            using var cts = new CancellationTokenSource();
            await archiveWorkflowService.OpenBundledSampleAsync(kind, cts.Token);
            RefreshBrowseBindings();
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

    private async Task OpenCloudArchiveAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource();
            var result = await archiveWorkflowService.OpenCloudArchiveAsync(cts.Token);
            if (result.Status == CloudFetchStatus.NoCacheError)
            {
                await ShowErrorAsync(result.ErrorMessage ?? LocalizedStrings.Get("Error.CloudFetch.NoCache"));
            }

            RefreshBrowseBindings();
        }
        catch (UnsupportedArchiveFormatException ex)
        {
            logger.LogError(ex, "Open cloud archive failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.NoSupportedFormat"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Open cloud archive failed. Exception={Exception}", ex.ToString());
            await ShowErrorAsync(LocalizedStrings.Get("Error.OpenArchive.Failed"));
        }
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

    private void RefreshBrowseBindings()
    {
        var hasArchive = browseViewModel.HasArchive;
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
        ArchiveSummaryPanel.Visibility = hasArchive ? Visibility.Visible : Visibility.Collapsed;
        BrowseContentPanel.Visibility = hasArchive ? Visibility.Visible : Visibility.Collapsed;
        ApplyBrowseLayoutMode();
    }

    private void OnBrowsePageSizeChanged(object sender, SizeChangedEventArgs e)
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
        EntryActionTitleText.Text = LocalizedStrings.Get("Entry.OpenGroup.Title");
        EntryActionDescriptionText.Text = LocalizedStrings.Get("Entry.OpenGroup.Description");
        EntryActionHintText.Text = LocalizedStrings.Get("Entry.OpenGroup.Hint");
        OpenSampleButton.Content = LocalizedStrings.Get("Entry.OpenSample");
        OpenFolderButton.Content = LocalizedStrings.Get("Browse.OpenFolder");
        OpenZipButton.Content = LocalizedStrings.Get("Browse.OpenZip");
        OpenCloudButton.Content = LocalizedStrings.Get("Browse.OpenCloud");
        OpenCloudButton.Visibility = cloudFetchFeatureOptions.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        NarrowBackButton.Content = LocalizedStrings.Get("Browse.Back");
        ChannelsHeaderText.Text = LocalizedStrings.Get("Browse.Channels");
        DatesHeaderText.Text = LocalizedStrings.Get("Browse.Dates");
        if (string.IsNullOrWhiteSpace(MessageContextText.Text))
        {
            MessageContextText.Text = LocalizedStrings.Get("Browse.Messages");
        }

        browseViewModel.RebuildLocalizedText();
    }

    private void OnArchiveChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (DispatcherQueue.HasThreadAccess)
        {
            RefreshBrowseBindings();
            return;
        }

        _ = DispatcherQueue.TryEnqueue(RefreshBrowseBindings);
    }

    private void OnBrowsePageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (isSessionEventHooked)
        {
            archiveSessionService.ArchiveChanged -= OnArchiveChanged;
            isSessionEventHooked = false;
        }

        Unloaded -= OnBrowsePageUnloaded;
    }
}
