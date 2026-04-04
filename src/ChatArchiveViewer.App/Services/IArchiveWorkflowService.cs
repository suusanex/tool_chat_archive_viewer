using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.App.Services;

public interface IArchiveWorkflowService
{
    Task OpenArchiveAsync(bool isZip, CancellationToken ct);

    Task OpenBundledSampleAsync(BundledSampleKind kind, CancellationToken ct);

    Task<CloudFetchResult> OpenCloudArchiveAsync(CancellationToken ct);
}
