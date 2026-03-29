namespace ChatArchiveViewer.App.Services;

public sealed class AppLaunchOptions
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private const string DebugEnglishArgument = "--debug-language-en-us";
    private const string DebugLanguageOffArgument = "--debug-language-off";

    public BundledSampleKind? AutoLoadSample { get; init; }

    public string? DebugPrimaryLanguageOverride { get; init; }

    public static AppLaunchOptions Parse(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var normalizedArgs = args.ToHashSet(Comparer);
        BundledSampleKind? autoLoadSample = normalizedArgs.Contains("--ui-test-load-sample-zip")
            ? BundledSampleKind.Zip
            : normalizedArgs.Contains("--ui-test-load-sample-folder") || normalizedArgs.Contains("--ui-test-load-sample")
                ? BundledSampleKind.Folder
                : null;
        var enableDebugEnglish = normalizedArgs.Contains(DebugEnglishArgument);
        var disableDebugLanguageOverride = normalizedArgs.Contains(DebugLanguageOffArgument);

        if (enableDebugEnglish && disableDebugLanguageOverride)
        {
            throw new ArgumentException(
                $"Cannot specify both {DebugEnglishArgument} and {DebugLanguageOffArgument}.",
                nameof(args));
        }

        var debugPrimaryLanguageOverride = enableDebugEnglish
            ? "en-US"
            : disableDebugLanguageOverride
                ? DefaultCultureName
                : null;

        return new AppLaunchOptions
        {
            AutoLoadSample = autoLoadSample,
            DebugPrimaryLanguageOverride = debugPrimaryLanguageOverride
        };
    }

    private const string DefaultCultureName = "ja-JP";
}
