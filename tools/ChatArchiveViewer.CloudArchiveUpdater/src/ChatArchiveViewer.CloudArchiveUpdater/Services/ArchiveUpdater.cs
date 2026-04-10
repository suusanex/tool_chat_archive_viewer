using System.Text.Json;
using Azure.Core;
using ChatArchiveViewer.CloudArchiveUpdater.Models;

namespace ChatArchiveViewer.CloudArchiveUpdater.Services;

public sealed class ArchiveUpdater
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IBootstrapConfigProvider bootstrapConfigProvider;
    private readonly ICloudAuthService cloudAuthService;
    private readonly ICloudManifestProvider cloudManifestProvider;
    private readonly ICloudArchiveDownloader cloudArchiveDownloader;
    private readonly IZipArchiveMerger zipArchiveMerger;
    private readonly IBlobFileUploader blobFileUploader;
    private readonly IFileHasher fileHasher;
    private readonly IManifestVersionGenerator manifestVersionGenerator;
    private readonly IClock clock;

    public ArchiveUpdater(
        IBootstrapConfigProvider bootstrapConfigProvider,
        ICloudAuthService cloudAuthService,
        ICloudManifestProvider cloudManifestProvider,
        ICloudArchiveDownloader cloudArchiveDownloader,
        IZipArchiveMerger zipArchiveMerger,
        IBlobFileUploader blobFileUploader,
        IFileHasher fileHasher,
        IManifestVersionGenerator manifestVersionGenerator,
        IClock clock)
    {
        this.bootstrapConfigProvider = bootstrapConfigProvider ?? throw new ArgumentNullException(nameof(bootstrapConfigProvider));
        this.cloudAuthService = cloudAuthService ?? throw new ArgumentNullException(nameof(cloudAuthService));
        this.cloudManifestProvider = cloudManifestProvider ?? throw new ArgumentNullException(nameof(cloudManifestProvider));
        this.cloudArchiveDownloader = cloudArchiveDownloader ?? throw new ArgumentNullException(nameof(cloudArchiveDownloader));
        this.zipArchiveMerger = zipArchiveMerger ?? throw new ArgumentNullException(nameof(zipArchiveMerger));
        this.blobFileUploader = blobFileUploader ?? throw new ArgumentNullException(nameof(blobFileUploader));
        this.fileHasher = fileHasher ?? throw new ArgumentNullException(nameof(fileHasher));
        this.manifestVersionGenerator = manifestVersionGenerator ?? throw new ArgumentNullException(nameof(manifestVersionGenerator));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ArchiveUpdateResult> UpdateAsync(ArchiveUpdaterOptions options, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);
        ct.ThrowIfCancellationRequested();

        var bootstrapConfig = await bootstrapConfigProvider.GetConfigAsync(ct);
        TokenCredential credential = await cloudAuthService.AuthenticateAsync(bootstrapConfig, ct);
        CloudManifest manifest = await cloudManifestProvider.GetManifestAsync(bootstrapConfig, credential, ct);

        var manifestUri = new Uri(bootstrapConfig.ManifestUrl, UriKind.Absolute);
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"chat-archive-updater-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var currentArchivePath = Path.Combine(tempDirectory, "current.zip");
            var mergedArchivePath = Path.Combine(tempDirectory, "merged.zip");
            var manifestPath = Path.Combine(tempDirectory, "manifest.json");

            await cloudArchiveDownloader.DownloadAsync(manifest, credential, currentArchivePath, ct);
            await zipArchiveMerger.MergeAsync(currentArchivePath, options.AdditionalZipPath, mergedArchivePath, ct);

            var sha256 = await fileHasher.ComputeSha256Async(mergedArchivePath, ct);
            var publishedAt = clock.GetUtcNow();
            var version = manifestVersionGenerator.CreateNextVersion(manifest.Version, publishedAt);
            var manifestDocument = new ManifestDocument
            {
                Version = version,
                DownloadUrl = manifest.DownloadUrl,
                Sha256 = sha256,
                PublishedAt = publishedAt
            };

            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifestDocument, JsonOptions),
                ct);

            await blobFileUploader.UploadAsync(manifest.DownloadUri, credential, mergedArchivePath, "application/zip", ct);
            await blobFileUploader.UploadAsync(manifestUri, credential, manifestPath, "application/json", ct);

            return new ArchiveUpdateResult
            {
                ArchiveUri = manifest.DownloadUri,
                ManifestUri = manifestUri,
                Version = version,
                Sha256 = sha256
            };
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
            catch
            {
                // Best effort cleanup only.
            }
        }
    }
}
