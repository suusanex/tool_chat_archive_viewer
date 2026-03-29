using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App.ViewModels;

public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly IAppSettingsService settingsService;
    private readonly IExternalLauncher externalLauncher;

    [ObservableProperty]
    private string appName = AppIdentity.AppName;

    [ObservableProperty]
    private string version = AppIdentity.Version;

    [ObservableProperty]
    private string disclaimer = LocalizedStrings.Get("About.Disclaimer");

    [ObservableProperty]
    private string privacySummary = LocalizedStrings.Get("About.PrivacySummary");

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenPrivacyPolicyCommand))]
    private string? privacyPolicyUrl;

    [ObservableProperty]
    private string supportedFormats = LocalizedStrings.Get("About.SupportedFormats");

    public AboutViewModel(IAppSettingsService settingsService, IExternalLauncher externalLauncher)
    {
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));
        PrivacyPolicyUrl = this.settingsService.PrivacyPolicyUrl;
    }

    private bool CanOpenPrivacyPolicy()
        => Uri.TryCreate(PrivacyPolicyUrl, UriKind.Absolute, out _);

    [RelayCommand(CanExecute = nameof(CanOpenPrivacyPolicy))]
    private async Task OpenPrivacyPolicyAsync()
    {
        if (!Uri.TryCreate(PrivacyPolicyUrl, UriKind.Absolute, out var uri))
        {
            return;
        }

        await externalLauncher.LaunchUriAsync(uri);
    }
}
