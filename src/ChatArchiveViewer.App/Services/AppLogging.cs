using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace ChatArchiveViewer.App.Services;

internal static class AppLogging
{
    private const string NLogConfigFileName = "NLog.config";
    private static readonly object SyncRoot = new();
    private static bool isConfigured;

    public static ILoggerFactory CreateBootstrapLoggerFactory()
        => LoggerFactory.Create(ConfigureLogging);

    public static void ConfigureLogging(ILoggingBuilder logging)
    {
        ArgumentNullException.ThrowIfNull(logging);

        EnsureNLogConfigured();
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Trace);
        logging.AddNLog(
            new NLogProviderOptions
            {
                CaptureMessageTemplates = true,
                CaptureMessageProperties = true,
                RemoveLoggerFactoryFilter = true,
            });
    }

    public static void EnsureNLogConfigured()
    {
        lock (SyncRoot)
        {
            if (isConfigured)
            {
                return;
            }

            var configPath = ResolveNLogConfigPath();
            var configuration = new NLog.Config.XmlLoggingConfiguration(configPath);
            NLog.LogManager.Configuration = configuration;
            isConfigured = true;
        }
    }

    private static string ResolveNLogConfigPath()
    {
        foreach (var root in EnumerateSearchRoots())
        {
            var directPath = Path.Combine(root, NLogConfigFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }

            var sourcePath = Path.Combine(root, "src", "ChatArchiveViewer.App", NLogConfigFileName);
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }

        throw new FileNotFoundException($"Could not find {NLogConfigFileName} from startup search roots.");
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
}
