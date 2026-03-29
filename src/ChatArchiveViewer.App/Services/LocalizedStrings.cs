using System.Globalization;
using System.Xml.Linq;

namespace ChatArchiveViewer.App.Services;

public static class LocalizedStrings
{
    private const string DefaultCultureName = "ja-JP";
    private static readonly Lock ResourceCacheLock = new();
    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> ResourceCache = new(StringComparer.OrdinalIgnoreCase);

    public static string Get(string key, string? fallback = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = ResolveString(key, CultureInfo.CurrentUICulture);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return fallback ?? key;
    }

    public static string Get(string key, CultureInfo culture, string? fallback = null)
        => GetCore(key, culture, fallback);

    private static string GetCore(string key, CultureInfo culture, string? fallback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(culture);

        var value = ResolveString(key, culture);
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

    private static string? ResolveString(string key, CultureInfo culture)
    {
        if (TryResolveFromResw(culture, key, out var value))
        {
            return value;
        }

        if (!string.Equals(culture.Name, DefaultCultureName, StringComparison.OrdinalIgnoreCase)
            && TryResolveFromResw(CultureInfo.GetCultureInfo(DefaultCultureName), key, out value))
        {
            return value;
        }

        return null;
    }

    private static bool TryResolveFromResw(CultureInfo culture, string key, out string? value)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        for (var current = culture; current != CultureInfo.InvariantCulture; current = current.Parent)
        {
            if (!TryGetResourceMap(current.Name, out var resources))
            {
                continue;
            }

            if (resources.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetResourceMap(string cultureName, out IReadOnlyDictionary<string, string> resources)
    {
        lock (ResourceCacheLock)
        {
            if (ResourceCache.TryGetValue(cultureName, out resources))
            {
                return true;
            }
        }

        var path = FindReswPath(cultureName);
        if (path is null)
        {
            resources = default!;
            return false;
        }

        var loaded = LoadResw(path);
        lock (ResourceCacheLock)
        {
            ResourceCache[cultureName] = loaded;
        }

        resources = loaded;
        return true;
    }

    private static string? FindReswPath(string cultureName)
    {
        foreach (var root in EnumerateSearchRoots())
        {
            var directPath = Path.Combine(root, "Strings", cultureName, "Resources.resw");
            if (File.Exists(directPath))
            {
                return directPath;
            }

            var sourcePath = Path.Combine(root, "src", "ChatArchiveViewer.App", "Strings", cultureName, "Resources.resw");
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            if (string.IsNullOrWhiteSpace(seed))
            {
                continue;
            }

            for (var current = new DirectoryInfo(seed); current is not null; current = current.Parent)
            {
                if (seen.Add(current.FullName))
                {
                    yield return current.FullName;
                }
            }
        }
    }

    private static IReadOnlyDictionary<string, string> LoadResw(string path)
    {
        var document = XDocument.Load(path);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var data in document.Root?.Elements("data") ?? [])
        {
            var name = data.Attribute("name")?.Value;
            var text = data.Element("value")?.Value;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            values[name] = text;
        }

        return values;
    }
}
