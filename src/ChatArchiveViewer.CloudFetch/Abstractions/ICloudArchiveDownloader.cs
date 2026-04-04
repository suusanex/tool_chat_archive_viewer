using Azure.Core;
using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Abstractions;

public interface ICloudArchiveDownloader
{
    Task DownloadAsync(CloudManifest manifest, TokenCredential credential, string destinationPath, CancellationToken ct);
}
