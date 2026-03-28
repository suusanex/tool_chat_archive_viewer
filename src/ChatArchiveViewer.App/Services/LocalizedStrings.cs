using System.Globalization;
using System.Resources;

namespace ChatArchiveViewer.App.Services;

public static class LocalizedStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "ChatArchiveViewer.App.Resources.UiStrings",
        typeof(LocalizedStrings).Assembly);

    public static string Get(string key, string? fallback = null)
    {
        var value = ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return fallback ?? key;
    }

    public static string Format(string key, params object[] args)
        => string.Format(CultureInfo.CurrentUICulture, Get(key), args);
}
