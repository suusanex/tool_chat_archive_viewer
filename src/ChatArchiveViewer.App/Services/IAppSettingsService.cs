using Microsoft.UI.Xaml;

namespace ChatArchiveViewer.App.Services;

public interface IAppSettingsService
{
    ElementTheme CurrentTheme { get; }

    string PrivacyPolicyUrl { get; }

    void SaveTheme(ElementTheme theme);
}
