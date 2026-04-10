using ChatArchiveViewer.CloudArchiveUpdater.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatArchiveViewer.CloudArchiveUpdater;

public static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args, CancellationToken ct)
    {
        try
        {
            var options = ArchiveUpdaterOptions.Parse(args);
            var bootstrapConfigUrl = BootstrapConfigUrlResolver.Resolve();
            if (string.IsNullOrWhiteSpace(bootstrapConfigUrl))
            {
                throw new InvalidOperationException(
                    $"Missing '{CloudArchiveUpdaterConstants.BootstrapConfigUrlConfigurationKey}' in appsettings.json or environment variables.");
            }

            using var httpClient = new HttpClient();
            var updater = new ArchiveUpdater(
                new BootstrapConfigProvider(
                    httpClient,
                    NullLogger<BootstrapConfigProvider>.Instance,
                    bootstrapConfigUrl),
                new MsalAuthService(NullLogger<MsalAuthService>.Instance),
                new CloudManifestProvider(NullLogger<CloudManifestProvider>.Instance),
                new CloudArchiveDownloader(),
                new ZipArchiveMerger(),
                new BlobFileUploader(),
                new Sha256FileHasher(),
                new ManifestVersionGenerator(),
                new SystemClock());

            var result = await updater.UpdateAsync(options, ct);

            Console.WriteLine($"Updated archive: {result.ArchiveUri}");
            Console.WriteLine($"Updated manifest: {result.ManifestUri}");
            Console.WriteLine($"Version: {result.Version}");
            Console.WriteLine($"SHA-256: {result.Sha256}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("The operation was canceled.");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
