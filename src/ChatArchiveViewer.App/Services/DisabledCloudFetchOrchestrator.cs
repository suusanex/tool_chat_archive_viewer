using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.App.Services;

public sealed class DisabledCloudFetchOrchestrator : ICloudFetchOrchestrator
{
    public Task<CloudFetchResult> FetchLatestAsync(IProgress<CloudFetchProgress>? progress, CancellationToken ct)
    {
        _ = progress;
        _ = ct;
        return Task.FromResult(new CloudFetchResult
        {
            Status = CloudFetchStatus.None
        });
    }
}