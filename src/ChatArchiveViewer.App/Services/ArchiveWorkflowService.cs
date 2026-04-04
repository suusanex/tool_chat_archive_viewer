using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.App.Services;

public sealed class ArchiveWorkflowService : IArchiveWorkflowService
{
    private readonly IArchiveOpenService archiveOpenService;
    private readonly IBundledSampleLocator bundledSampleLocator;
    private readonly IArchiveFormatRegistry formatRegistry;
    private readonly IArchiveLoadService archiveLoadService;
    private readonly IArchiveSessionService archiveSessionService;
    private readonly ArchiveOverviewViewModel overviewViewModel;
    private readonly ArchiveBrowseViewModel browseViewModel;
    private readonly SearchViewModel searchViewModel;
    private readonly ILogger<ArchiveWorkflowService> logger;

    public ArchiveWorkflowService(
        IArchiveOpenService archiveOpenService,
        IBundledSampleLocator bundledSampleLocator,
        IArchiveFormatRegistry formatRegistry,
        IArchiveLoadService archiveLoadService,
        IArchiveSessionService archiveSessionService,
        ArchiveOverviewViewModel overviewViewModel,
        ArchiveBrowseViewModel browseViewModel,
        SearchViewModel searchViewModel,
        ILogger<ArchiveWorkflowService> logger)
    {
        this.archiveOpenService = archiveOpenService ?? throw new ArgumentNullException(nameof(archiveOpenService));
        this.bundledSampleLocator = bundledSampleLocator ?? throw new ArgumentNullException(nameof(bundledSampleLocator));
        this.formatRegistry = formatRegistry ?? throw new ArgumentNullException(nameof(formatRegistry));
        this.archiveLoadService = archiveLoadService ?? throw new ArgumentNullException(nameof(archiveLoadService));
        this.archiveSessionService = archiveSessionService ?? throw new ArgumentNullException(nameof(archiveSessionService));
        this.overviewViewModel = overviewViewModel ?? throw new ArgumentNullException(nameof(overviewViewModel));
        this.browseViewModel = browseViewModel ?? throw new ArgumentNullException(nameof(browseViewModel));
        this.searchViewModel = searchViewModel ?? throw new ArgumentNullException(nameof(searchViewModel));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OpenArchiveAsync(bool isZip, CancellationToken ct)
    {
        IArchiveSource? source = null;
        try
        {
            source = isZip
                ? await archiveOpenService.OpenZipAsync(bundledSampleLocator.SampleZipPath, ct)
                : await archiveOpenService.OpenFolderAsync(bundledSampleLocator.SampleFolderPath, ct);
            if (source is null)
            {
                return;
            }

            var loadResult = await LoadArchiveAsync(source, ct);
            await archiveSessionService.SetCurrentAsync(source, loadResult.Provider, loadResult.Archive);
            source = null;

            overviewViewModel.SetArchive(loadResult.Archive);
            await browseViewModel.RefreshFromSessionAsync();
            searchViewModel.SearchCommand.NotifyCanExecuteChanged();

            logger.LogInformation("Archive loaded. Format={FormatId} Conversations={ConversationCount}", loadResult.FormatId, loadResult.Archive.Conversations.Count);
        }
        finally
        {
            if (source is not null)
            {
                await source.DisposeAsync();
            }
        }
    }

    public async Task OpenBundledSampleAsync(BundledSampleKind kind, CancellationToken ct)
    {
        IArchiveSource? source = null;
        try
        {
            source = await archiveOpenService.OpenBundledSampleAsync(kind, ct);
            if (source is null)
            {
                return;
            }

            var loadResult = await LoadArchiveAsync(source, ct);
            await archiveSessionService.SetCurrentAsync(source, loadResult.Provider, loadResult.Archive);
            source = null;

            overviewViewModel.SetArchive(loadResult.Archive);
            await browseViewModel.RefreshFromSessionAsync();
            searchViewModel.SearchCommand.NotifyCanExecuteChanged();

            logger.LogInformation("Archive loaded. Format={FormatId} Conversations={ConversationCount}", loadResult.FormatId, loadResult.Archive.Conversations.Count);
        }
        finally
        {
            if (source is not null)
            {
                await source.DisposeAsync();
            }
        }
    }

    private async Task<(IArchiveFormatProvider Provider, ChatArchive Archive, string FormatId)> LoadArchiveAsync(IArchiveSource source, CancellationToken ct)
    {
        var detections = await formatRegistry.DetectAllAsync(source, ct);
        var best = detections
            .Where(x => x.IsDetected)
            .OrderByDescending(x => x.Confidence)
            .FirstOrDefault();

        if (best is null)
        {
            throw new UnsupportedArchiveFormatException();
        }

        var provider = formatRegistry.GetProvider(best.FormatId)
            ?? throw new InvalidOperationException($"Provider not found: {best.FormatId}");
        var archive = await archiveLoadService.LoadAsync(source, provider, progress: null, ct);
        return (provider, archive, best.FormatId);
    }
}
