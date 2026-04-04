using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Abstractions;

public interface ICloudFetchOrchestrator
{
    Task<CloudFetchResult> FetchLatestAsync(IProgress<CloudFetchProgress>? progress, CancellationToken ct);
}
