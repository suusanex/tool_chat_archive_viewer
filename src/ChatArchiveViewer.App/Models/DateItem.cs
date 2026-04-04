using CommunityToolkit.Mvvm.ComponentModel;

namespace ChatArchiveViewer.App.Models;

public sealed partial class DateItem : ObservableObject
{
    public required DateOnly Date { get; init; }

    [ObservableProperty]
    private int messageCount;

    [ObservableProperty]
    private bool isSelected;

    public string DisplayLabel => Date.ToString("yyyy-MM-dd");

    public string DayLabel => Date.ToString("dd");

    public string DayOfWeekLabel => Date.ToString("ddd");
}
