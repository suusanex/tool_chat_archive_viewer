using Azure.Core;
using Azure.Storage.Blobs;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Services;

public sealed class CloudArchiveDownloader : ICloudArchiveDownloader
{
    public async Task DownloadAsync(CloudManifest manifest, TokenCredential credential, string destinationPath, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var blobClient = new BlobClient(manifest.DownloadUri, credential);
        await blobClient.DownloadToAsync(destinationPath, ct);
    }
}
