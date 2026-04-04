using CommunityToolkit.Mvvm.ComponentModel;
using ChatArchiveViewer.App.Models;
using ChatArchiveViewer.App.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace ChatArchiveViewer.App.ViewModels;

public sealed partial class ArchiveBrowseViewModel : ViewModelBase
{
    private readonly IArchiveSessionService sessionService;
    private readonly IConversationDateCountService dateCountService;
    private readonly MessageListViewModel messageListViewModel;
    private readonly ILogger<ArchiveBrowseViewModel> logger;
    private readonly Dictionary<string, string> participantNameMap = new(StringComparer.OrdinalIgnoreCase);
    private DateItem? currentSelectedDateItem;

    public ObservableCollection<Conversation> Conversations { get; } = new();

    public ObservableCollection<DateYearItem> DateYears { get; } = new();

    public ObservableCollection<string> BreadcrumbItems { get; } = new();

    [ObservableProperty]
    private Conversation? selectedConversation;

    [ObservableProperty]
    private DateItem? selectedDate;

    [ObservableProperty]
    private string breadcrumb = LocalizedStrings.Get("Browse.Breadcrumb.Root");

    [ObservableProperty]
    private bool isNarrowLayout;

    public ArchiveBrowseViewModel(
        IArchiveSessionService sessionService,
        MessageListViewModel messageListViewModel,
        IConversationDateCountService dateCountService,
        ILogger<ArchiveBrowseViewModel> logger)
    {
        this.sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        this.messageListViewModel = messageListViewModel ?? throw new ArgumentNullException(nameof(messageListViewModel));
        this.dateCountService = dateCountService ?? throw new ArgumentNullException(nameof(dateCountService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MessageListViewModel MessageList => messageListViewModel;

    public bool HasArchive => sessionService.HasArchive && sessionService.Archive is not null;

    public async Task RefreshFromSessionAsync()
    {
        dateCountService.ClearCache();
        Conversations.Clear();
        DateYears.Clear();
        participantNameMap.Clear();
        selectedConversation = null;
        selectedDate = null;
        currentSelectedDateItem = null;

        if (sessionService.Archive is null)
        {
            SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"));
            messageListViewModel.SetMessages(LocalizedStrings.Get("Browse.Context.NoArchive"), Array.Empty<MessageViewItem>());
            return;
        }

        foreach (var participant in sessionService.Archive.Participants)
        {
            if (!string.IsNullOrWhiteSpace(participant.Id) && !string.IsNullOrWhiteSpace(participant.DisplayName))
            {
                participantNameMap[participant.Id] = participant.DisplayName;
            }
        }

        foreach (var conversation in sessionService.Archive.Conversations.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            Conversations.Add(conversation);
        }

        if (Conversations.Count > 0)
        {
            await SelectConversationAsync(Conversations[0]);
        }
        else
        {
            SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"));
            messageListViewModel.SetMessages(LocalizedStrings.Get("Browse.Context.NoChannel"), Array.Empty<MessageViewItem>());
        }
    }

    public async Task SelectConversationAsync(Conversation? value)
    {
        SelectedConversation = value;
        DateYears.Clear();
        currentSelectedDateItem = null;
        SelectedDate = null;

        if (value is null)
        {
            SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"));
            messageListViewModel.SetMessages(LocalizedStrings.Get("Browse.Context.SelectChannel"), Array.Empty<MessageViewItem>());
            return;
        }

        var dateItems = value.AvailableDates
            .OrderByDescending(x => x)
            .Select(
                date => new DateItem
                {
                    Date = date,
                    MessageCount = 0
                })
            .ToArray();

        foreach (var yearGroup in dateItems
                     .GroupBy(x => x.Date.Year)
                     .OrderByDescending(x => x.Key))
        {
            var yearItem = new DateYearItem
            {
                Year = yearGroup.Key,
                IsExpanded = false
            };

            foreach (var monthGroup in yearGroup
                         .GroupBy(x => x.Date.Month)
                         .OrderByDescending(x => x.Key))
            {
                var monthItem = new DateMonthItem
                {
                    Year = yearGroup.Key,
                    Month = monthGroup.Key,
                    IsExpanded = false
                };
                monthItem.PropertyChanged += OnMonthItemPropertyChanged;

                foreach (var day in monthGroup.OrderByDescending(x => x.Date))
                {
                    monthItem.Days.Add(day);
                }

                yearItem.Months.Add(monthItem);
            }

            DateYears.Add(yearItem);
        }

        SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"), value.DisplayName);
        if (dateItems.Length == 0)
        {
            messageListViewModel.SetMessages(
                LocalizedStrings.Format("Browse.Context.NoDate", value.DisplayName),
                Array.Empty<MessageViewItem>());
            return;
        }

        messageListViewModel.SetMessages(
            LocalizedStrings.Format("Browse.Context.SelectDate", value.DisplayName),
            Array.Empty<MessageViewItem>());
    }

    private async void OnMonthItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not DateMonthItem month || e.PropertyName != nameof(DateMonthItem.IsExpanded) || !month.IsExpanded)
        {
            return;
        }

        try
        {
            await LoadMonthDateCountsAsync(month);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Load date counts failed. Exception={Exception}", ex.ToString());
        }
    }

    public async Task LoadMonthDateCountsAsync(DateMonthItem month, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(month);

        var conversation = selectedConversation;
        if (conversation is null || month.HasLoadedMessageCounts || month.IsLoadingMessageCounts)
        {
            return;
        }

        month.IsLoadingMessageCounts = true;
        try
        {
            var dates = month.Days.Select(x => x.Date).ToArray();
            var counts = await dateCountService.LoadMonthCountsAsync(conversation.Id, dates, ct);
            foreach (var day in month.Days)
            {
                if (counts.TryGetValue(day.Date, out var count))
                {
                    day.MessageCount = count;
                }
            }

            month.HasLoadedMessageCounts = true;
        }
        finally
        {
            month.IsLoadingMessageCounts = false;
        }
    }

    public async Task SelectDateAsync(DateItem? value)
    {
        if (currentSelectedDateItem is not null)
        {
            currentSelectedDateItem.IsSelected = false;
        }

        SelectedDate = value;
        currentSelectedDateItem = value;
        if (value is not null)
        {
            value.IsSelected = true;
        }

        await LoadSelectedMessagesAsync(value);
    }

    private async Task LoadSelectedMessagesAsync(DateItem? value)
    {
        if (selectedConversation is null || value is null)
        {
            return;
        }

        var messages = await sessionService.LoadMessagesAsync(selectedConversation.Id, value.Date, CancellationToken.None);
        SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"), selectedConversation.DisplayName, value.Date.ToString("yyyy-MM-dd"));
        var mapped = messages
            .Select(
                message => new MessageViewItem
                {
                    Message = message,
                    ParticipantDisplayName = ResolveParticipantDisplayName(message),
                    DisplayTimestamp = message.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    Text = message.Text
                })
            .ToArray();
        messageListViewModel.SetMessages(
            LocalizedStrings.Format("Browse.Context.SelectedDate", selectedConversation.DisplayName, value.Date),
            mapped);
    }

    public void NavigateToChannelOnly()
    {
        if (selectedConversation is null)
        {
            SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"));
            return;
        }

        SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"), selectedConversation.DisplayName);
    }

    public void RebuildLocalizedText()
    {
        if (selectedConversation is null)
        {
            SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"));
            return;
        }

        if (selectedDate is null)
        {
            SetBreadcrumb(LocalizedStrings.Get("Browse.Breadcrumb.Root"), selectedConversation.DisplayName);
            return;
        }

        SetBreadcrumb(
            LocalizedStrings.Get("Browse.Breadcrumb.Root"),
            selectedConversation.DisplayName,
            selectedDate.Date.ToString("yyyy-MM-dd"));
        messageListViewModel.ContextTitle = LocalizedStrings.Format(
            "Browse.Context.SelectedDate",
            selectedConversation.DisplayName,
            selectedDate.Date);
    }

    private void SetBreadcrumb(params string[] segments)
    {
        BreadcrumbItems.Clear();
        foreach (var segment in segments.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            BreadcrumbItems.Add(segment);
        }

        Breadcrumb = string.Join(" > ", BreadcrumbItems);
    }

    private string ResolveParticipantDisplayName(ChatMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.ParticipantId) &&
            participantNameMap.TryGetValue(message.ParticipantId, out var resolved))
        {
            return resolved;
        }

        if (message.ExtendedProperties.TryGetValue("user_profile_display_name", out var profileDisplayName) &&
            !string.IsNullOrWhiteSpace(profileDisplayName))
        {
            return profileDisplayName;
        }

        return string.IsNullOrWhiteSpace(message.ParticipantId)
            ? LocalizedStrings.Get("Browse.Day.UnknownParticipant")
            : message.ParticipantId!;
    }
}
