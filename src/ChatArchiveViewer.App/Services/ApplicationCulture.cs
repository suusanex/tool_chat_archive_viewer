using System.Globalization;

namespace ChatArchiveViewer.App.Services;

public static class ApplicationCulture
{
    private static readonly CultureInfo JapaneseCulture = CultureInfo.GetCultureInfo("ja-JP");
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

    public static CultureInfo ResolveSupportedCulture(CultureInfo? requestedCulture = null)
    {
        requestedCulture ??= CultureInfo.CurrentUICulture;

        return requestedCulture.TwoLetterISOLanguageName switch
        {
            "en" => EnglishCulture,
            "ja" => JapaneseCulture,
            _ => JapaneseCulture,
        };
    }

    public static CultureInfo ApplySupportedCulture(CultureInfo? requestedCulture = null)
    {
        var selectedCulture = ResolveSupportedCulture(requestedCulture);
        CultureInfo.DefaultThreadCurrentCulture = selectedCulture;
        CultureInfo.DefaultThreadCurrentUICulture = selectedCulture;
        CultureInfo.CurrentCulture = selectedCulture;
        CultureInfo.CurrentUICulture = selectedCulture;
        return selectedCulture;
    }
}
