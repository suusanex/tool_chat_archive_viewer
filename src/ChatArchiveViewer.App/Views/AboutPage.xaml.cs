using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.App.Views;

public sealed partial class AboutPage : Page
{
    private readonly AboutViewModel aboutViewModel;
    private readonly ILogger<AboutPage> logger;

    public AboutPage()
    {
        InitializeComponent();
        var services = ((App)Application.Current).Host.Services;
        aboutViewModel = services.GetRequiredService<AboutViewModel>();
        logger = services.GetRequiredService<ILogger<AboutPage>>();
        ApplyLocalization();
        BindAbout();
    }

    private async void OnOpenPrivacyPolicyClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
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

    private void BindAbout()
    {
        AboutAppNameText.Text = aboutViewModel.AppName;
        AboutVersionText.Text = $"{LocalizedStrings.Get("About.VersionPrefix")}{aboutViewModel.Version}";
        AboutDisclaimerText.Text = aboutViewModel.Disclaimer;
        AboutPrivacySummaryText.Text = aboutViewModel.PrivacySummary;
        AboutFormatsText.Text = aboutViewModel.SupportedFormats;
        OpenPrivacyPolicyButton.IsEnabled = aboutViewModel.OpenPrivacyPolicyCommand.CanExecute(null);
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
        AboutLicensePlaceholderText.Text = LocalizedStrings.Get("About.LicensePlaceholder");
        OpenPrivacyPolicyButton.Content = LocalizedStrings.Get("About.OpenPrivacyPolicy");
    }
}
