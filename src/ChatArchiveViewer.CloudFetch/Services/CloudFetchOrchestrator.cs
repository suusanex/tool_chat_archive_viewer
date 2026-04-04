using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.CloudFetch.Services;

public sealed class CloudFetchOrchestrator : ICloudFetchOrchestrator
{
    private readonly IBootstrapConfigProvider bootstrapConfigProvider;
    private readonly ICloudAuthService cloudAuthService;
    private readonly ICloudManifestProvider cloudManifestProvider;
    private readonly ICloudArchiveDownloader cloudArchiveDownloader;
    private readonly ICacheManager cacheManager;
    private readonly IHashVerifier hashVerifier;
    private readonly ILogger<CloudFetchOrchestrator> logger;

    public CloudFetchOrchestrator(
        IBootstrapConfigProvider bootstrapConfigProvider,
        ICloudAuthService cloudAuthService,
        ICloudManifestProvider cloudManifestProvider,
        ICloudArchiveDownloader cloudArchiveDownloader,
        ICacheManager cacheManager,
        IHashVerifier hashVerifier,
        ILogger<CloudFetchOrchestrator> logger)
    {
        this.bootstrapConfigProvider = bootstrapConfigProvider ?? throw new ArgumentNullException(nameof(bootstrapConfigProvider));
        this.cloudAuthService = cloudAuthService ?? throw new ArgumentNullException(nameof(cloudAuthService));
        this.cloudManifestProvider = cloudManifestProvider ?? throw new ArgumentNullException(nameof(cloudManifestProvider));
        this.cloudArchiveDownloader = cloudArchiveDownloader ?? throw new ArgumentNullException(nameof(cloudArchiveDownloader));
        this.cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        this.hashVerifier = hashVerifier ?? throw new ArgumentNullException(nameof(hashVerifier));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CloudFetchResult> FetchLatestAsync(IProgress<CloudFetchProgress>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            ReportProgress(progress, CloudFetchProgressStage.Bootstrap, "Loading bootstrap config.");
            var config = await bootstrapConfigProvider.GetConfigAsync(ct);

            ReportProgress(progress, CloudFetchProgressStage.Authentication, "Authenticating.");
            var credential = await cloudAuthService.AuthenticateAsync(config, ct);

            ReportProgress(progress, CloudFetchProgressStage.Manifest, "Loading manifest.");
            var manifest = await cloudManifestProvider.GetManifestAsync(config, credential, ct);

            ReportProgress(progress, CloudFetchProgressStage.VersionCheck, "Checking cache state.");
            var currentState = await cacheManager.GetCurrentStateAsync(ct);
            var currentZipPath = cacheManager.GetCurrentZipPath();
            if (!string.IsNullOrWhiteSpace(currentZipPath) &&
                currentState is not null &&
                string.Equals(currentState.Version, manifest.Version, StringComparison.Ordinal))
            {
                ReportProgress(progress, CloudFetchProgressStage.Completed, "Already up to date.");
                return new CloudFetchResult
                {
                    Status = CloudFetchStatus.AlreadyUpToDate,
                    CachedZipPath = currentZipPath,
                    Version = currentState.Version
                };
            }

            var tempPath = await cacheManager.GetTempDownloadPathAsync(ct);
            try
            {
                ReportProgress(progress, CloudFetchProgressStage.Download, "Downloading archive.");
                await cloudArchiveDownloader.DownloadAsync(manifest, credential, tempPath, ct);

                ReportProgress(progress, CloudFetchProgressStage.Verify, "Verifying archive hash.");
                var hashResult = await hashVerifier.VerifyAsync(tempPath, manifest.Sha256, ct);
                if (!hashResult.Matched)
                {
                    logger.LogError(
                        "Cloud archive hash mismatch. ExpectedSha256={ExpectedSha256} ActualSha256={ActualSha256}",
                        manifest.Sha256,
                        hashResult.ActualSha256);
                    DeleteTempFileIfExists(tempPath);
                    return await CreateFallbackResultAsync("Cloud archive verification failed.", null, ct);
                }

                ReportProgress(progress, CloudFetchProgressStage.Commit, "Committing archive cache.");
                await cacheManager.CommitDownloadAsync(tempPath, manifest.Version, manifest.Sha256, ct);
            }
            catch (OperationCanceledException)
            {
                DeleteTempFileIfExists(tempPath);
                throw;
            }
            catch (Exception ex)
            {
                DeleteTempFileIfExists(tempPath);
                return await CreateFallbackResultAsync("Cloud archive download failed.", ex, ct);
            }

            var latestZipPath = cacheManager.GetCurrentZipPath();
            if (string.IsNullOrWhiteSpace(latestZipPath))
            {
                throw new InvalidOperationException("Cache commit completed but current.zip is unavailable.");
            }

            ReportProgress(progress, CloudFetchProgressStage.Completed, "Download completed.");
            return new CloudFetchResult
            {
                Status = CloudFetchStatus.FreshDownload,
                CachedZipPath = latestZipPath,
                Version = manifest.Version
            };
        }
        catch (OperationCanceledException ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }

            return await CreateFallbackResultAsync("Cloud archive fetch was canceled.", ex, ct);
        }
        catch (Exception ex)
        {
            return await CreateFallbackResultAsync("Cloud archive fetch failed.", ex, ct);
        }
    }

    private async Task<CloudFetchResult> CreateFallbackResultAsync(string message, Exception? exception, CancellationToken ct)
    {
        if (exception is not null)
        {
            logger.LogError(exception, "Cloud fetch fallback activated. Exception={Exception}", exception.ToString());
        }

        CacheState? currentState = null;
        try
        {
            currentState = await cacheManager.GetCurrentStateAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load current cache state during fallback. Exception={Exception}", ex.ToString());
        }

        var currentZipPath = cacheManager.GetCurrentZipPath();
        if (!string.IsNullOrWhiteSpace(currentZipPath))
        {
            return new CloudFetchResult
            {
                Status = CloudFetchStatus.StaleCache,
                CachedZipPath = currentZipPath,
                Version = currentState?.Version,
                ErrorMessage = message
            };
        }

        return new CloudFetchResult
        {
            Status = CloudFetchStatus.NoCacheError,
            ErrorMessage = message
        };
    }

    private void DeleteTempFileIfExists(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete temp file. Exception={Exception}", ex.ToString());
        }
    }

    private static void ReportProgress(IProgress<CloudFetchProgress>? progress, CloudFetchProgressStage stage, string detail)
    {
        progress?.Report(new CloudFetchProgress { Stage = stage, Detail = detail });
    }
}
