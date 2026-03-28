using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatArchiveViewer.App.Services;
using Microsoft.UI.Xaml;

namespace ChatArchiveViewer.App.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService settingsService;

    [ObservableProperty]
    private ElementTheme selectedTheme;

    public IReadOnlyList<ElementTheme> Themes { get; } =
    [
        ElementTheme.Default,
        ElementTheme.Light,
        ElementTheme.Dark
    ];

    public SettingsViewModel(IAppSettingsService settingsService)
    {
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        SelectedTheme = settingsService.CurrentTheme;
    }

    [RelayCommand]
    private void SaveTheme()
    {
        settingsService.SaveTheme(SelectedTheme);
    }
}
