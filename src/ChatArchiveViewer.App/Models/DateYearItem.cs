using CommunityToolkit.Mvvm.ComponentModel;

namespace ChatArchiveViewer.App.Models;

public sealed partial class DateYearItem : ObservableObject
{
    public required int Year { get; init; }

    public ObservableCollection<DateMonthItem> Months { get; } = [];

    [ObservableProperty]
    private bool isExpanded;

    public string Label => Year.ToString();
}
