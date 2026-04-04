using Azure.Core;
using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Abstractions;

public interface ICloudAuthService
{
    Task<TokenCredential> AuthenticateAsync(BootstrapConfig config, CancellationToken ct);
}
