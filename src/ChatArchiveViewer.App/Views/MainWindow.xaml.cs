using ChatArchiveViewer.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ChatArchiveViewer.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainPage mainPage;

    public MainWindow(MainPage mainPage, IAppSettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(mainPage);
        ArgumentNullException.ThrowIfNull(settingsService);

        InitializeComponent();

        this.mainPage = mainPage;
        Title = AppIdentity.AppName;
        AppWindow.Title = AppIdentity.AppName;
        AppTitleBar.Title = AppIdentity.AppName;
        MainContentPresenter.Content = mainPage;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (MainContentPresenter.Content is FrameworkElement element)
        {
            element.RequestedTheme = settingsService.CurrentTheme;
        }
    }

    private void OnPaneToggleRequested(TitleBar sender, object args)
    {
        _ = sender;
        _ = args;
        mainPage.ToggleNavigationPane();
    }
}
