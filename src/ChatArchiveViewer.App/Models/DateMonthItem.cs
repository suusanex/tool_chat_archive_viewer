using CommunityToolkit.Mvvm.ComponentModel;

namespace ChatArchiveViewer.App.Models;

public sealed partial class DateMonthItem : ObservableObject
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public ObservableCollection<DateItem> Days { get; } = [];

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool hasLoadedMessageCounts;

    [ObservableProperty]
    private bool isLoadingMessageCounts;

    public string Label => $"{Month:00}";

    public string DisplayLabel => $"{Year:0000}-{Month:00}";
}
