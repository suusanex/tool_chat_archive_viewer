namespace ChatArchiveViewer.App.Services;

public sealed class AppLaunchOptions
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public BundledSampleKind? AutoLoadSample { get; init; }

    public static AppLaunchOptions Parse(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var normalizedArgs = args.ToHashSet(Comparer);
        BundledSampleKind? autoLoadSample = normalizedArgs.Contains("--ui-test-load-sample-zip")
            ? BundledSampleKind.Zip
            : normalizedArgs.Contains("--ui-test-load-sample-folder") || normalizedArgs.Contains("--ui-test-load-sample")
                ? BundledSampleKind.Folder
                : null;

        return new AppLaunchOptions
        {
            AutoLoadSample = autoLoadSample
        };
    }
}
