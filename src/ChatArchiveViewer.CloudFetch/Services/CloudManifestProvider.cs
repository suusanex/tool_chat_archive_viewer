using System.Text.Json;
using Azure.Core;
using Azure.Storage.Blobs;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.CloudFetch.Services;

public sealed class CloudManifestProvider : ICloudManifestProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<CloudManifestProvider> logger;

    public CloudManifestProvider(ILogger<CloudManifestProvider> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CloudManifest> GetManifestAsync(BootstrapConfig config, TokenCredential credential, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(credential);

        var manifestUri = new Uri(config.ManifestUrl, UriKind.Absolute);
        var blobClient = new BlobClient(manifestUri, credential);
        var response = await blobClient.DownloadContentAsync(ct);
        var json = response.Value.Content.ToString();
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("manifest.json is empty.");
        }

        var rawManifest = JsonSerializer.Deserialize<RawManifest>(json, JsonOptions);
        if (rawManifest is null)
        {
            throw new InvalidDataException("manifest.json cannot be deserialized.");
        }

        if (string.IsNullOrWhiteSpace(rawManifest.Version))
        {
            throw new InvalidDataException("manifest.json version is required.");
        }

        if (string.IsNullOrWhiteSpace(rawManifest.DownloadUrl))
        {
            throw new InvalidDataException("manifest.json downloadUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(rawManifest.Sha256))
        {
            throw new InvalidDataException("manifest.json sha256 is required.");
        }

        ValidateSha256(rawManifest.Sha256);

        var downloadUri = Uri.TryCreate(rawManifest.DownloadUrl, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri(manifestUri, rawManifest.DownloadUrl);

        logger.LogInformation("Manifest loaded. Version={Version} DownloadUri={DownloadUri}", rawManifest.Version, downloadUri);
        return new CloudManifest
        {
            Version = rawManifest.Version,
            DownloadUrl = rawManifest.DownloadUrl,
            Sha256 = rawManifest.Sha256,
            PublishedAt = rawManifest.PublishedAt,
            DownloadUri = downloadUri
        };
    }

    private static void ValidateSha256(string sha256)
    {
        var normalized = sha256.Trim();
        if (normalized.Length != 64)
        {
            throw new InvalidDataException("manifest.json sha256 must be 64 hex chars.");
        }

        _ = Convert.FromHexString(normalized);
    }

    private sealed class RawManifest
    {
        public string? Version { get; init; }

        public string? DownloadUrl { get; init; }

        public string? Sha256 { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }
    }
}
