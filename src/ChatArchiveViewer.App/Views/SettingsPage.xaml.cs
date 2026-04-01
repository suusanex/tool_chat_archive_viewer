using Microsoft.Extensions.DependencyInjection;

namespace ChatArchiveViewer.App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly SettingsViewModel settingsViewModel;

    public SettingsPage()
    {
        InitializeComponent();
        var services = ((App)Application.Current).Host.Services;
        settingsViewModel = services.GetRequiredService<SettingsViewModel>();
        ApplyLocalization();
        UpdateThemeCombo();
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
        _ = e;
        settingsViewModel.SaveThemeCommand.Execute(null);
        if (((App)Application.Current).MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = settingsViewModel.SelectedTheme;
        }
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

    private void ApplyLocalization()
    {
        ThemeLabelText.Text = LocalizedStrings.Get("SettingsThemeLabel");
        ThemeComboBox.Items.Clear();
        ThemeComboBox.Items.Add(LocalizedStrings.Get("SettingsThemeSystem"));
        ThemeComboBox.Items.Add(LocalizedStrings.Get("SettingsThemeLight"));
        ThemeComboBox.Items.Add(LocalizedStrings.Get("SettingsThemeDark"));
        SaveSettingsButton.Content = LocalizedStrings.Get("Settings.Save");
    }
}
