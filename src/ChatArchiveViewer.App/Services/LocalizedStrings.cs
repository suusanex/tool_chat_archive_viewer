using System.Globalization;
using System.Resources;

namespace ChatArchiveViewer.App.Services;

public static class LocalizedStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "ChatArchiveViewer.App.Resources.UiStrings",
        typeof(LocalizedStrings).Assembly);

    public static string Get(string key, string? fallback = null)
        => GetCore(key, CultureInfo.CurrentUICulture, fallback);

    public static string Get(string key, CultureInfo culture, string? fallback = null)
        => GetCore(key, culture, fallback);

    private static string GetCore(string key, CultureInfo culture, string? fallback)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var value = ResourceManager.GetString(key, culture);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return fallback ?? key;
    }

    public static string Format(string key, params object[] args)
        => string.Format(CultureInfo.CurrentUICulture, Get(key), args);

    public static string Format(string key, CultureInfo culture, params object[] args)
        => string.Format(culture, Get(key, culture), args);
}
