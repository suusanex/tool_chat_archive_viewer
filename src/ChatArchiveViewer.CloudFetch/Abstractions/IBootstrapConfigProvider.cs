using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Abstractions;

public interface IBootstrapConfigProvider
{
    Task<BootstrapConfig> GetConfigAsync(CancellationToken ct);
}
