using Azure.Core;
using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Abstractions;

public interface ICloudManifestProvider
{
    Task<CloudManifest> GetManifestAsync(BootstrapConfig config, TokenCredential credential, CancellationToken ct);
}
