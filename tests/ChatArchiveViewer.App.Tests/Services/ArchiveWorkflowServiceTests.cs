using ChatArchiveViewer.App.Services;
using ChatArchiveViewer.App.ViewModels;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;
using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Models;
using ChatArchiveViewer.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatArchiveViewer.App.Tests.Services;

[TestFixture]
public sealed class ArchiveWorkflowServiceTests
{
    private readonly List<string> tempDirectories = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var directory in tempDirectories)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        tempDirectories.Clear();
    }

    // TP-C023: FreshDownload のキャッシュ ZIP を ZipArchiveSource として読み込み、ステータスを反映する
    [Test]
    public async Task UT_IT_TP_C023__OpenCloudArchiveAsync_FreshDownload_LoadsZipAndSetsSession()
    {
        var zipPath = CreateArchiveZip();
        var fixture = CreateFixture();
        fixture.Orchestrator.Result = new CloudFetchResult
        {
            Status = CloudFetchStatus.FreshDownload,
            CachedZipPath = zipPath,
            Version = "2026-04-01-v1"
        };

        var result = await fixture.Service.OpenCloudArchiveAsync(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.FreshDownload));
        Assert.That(fixture.MainPageViewModel.CloudFetchStatus, Is.EqualTo(CloudFetchStatus.FreshDownload));
        Assert.That(fixture.Registry.LastDetectSource, Is.TypeOf<ZipArchiveSource>());
        Assert.That(fixture.SessionService.SetCurrentCallCount, Is.EqualTo(1));
        Assert.That(fixture.SessionService.LastSource, Is.Not.Null);
        Assert.That(fixture.SessionService.LastSource!.DisplayPath, Is.EqualTo(zipPath));
    }

    // TP-C023d: StaleCache の場合も表示しつつ警告状態を維持する
    [Test]
    public async Task UT_IT_TP_C023__OpenCloudArchiveAsync_StaleCache_LoadsZipAndKeepsWarningStatus()
    {
        var zipPath = CreateArchiveZip();
        var fixture = CreateFixture();
        fixture.Orchestrator.Result = new CloudFetchResult
        {
            Status = CloudFetchStatus.StaleCache,
            CachedZipPath = zipPath,
            Version = "2026-04-01-v1",
            ErrorMessage = "stale-cache"
        };

        var result = await fixture.Service.OpenCloudArchiveAsync(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.StaleCache));
        Assert.That(fixture.MainPageViewModel.CloudFetchStatus, Is.EqualTo(CloudFetchStatus.StaleCache));
        Assert.That(fixture.MainPageViewModel.CloudFetchErrorMessage, Is.EqualTo("stale-cache"));
        Assert.That(fixture.SessionService.SetCurrentCallCount, Is.EqualTo(1));
    }

    // TP-C024: ローカルアーカイブを開いたときはクラウド警告状態をクリアする
    [Test]
    public async Task UT_IT_TP_C024__OpenArchiveAsync_LocalArchiveClearsCloudWarning()
    {
        var fixture = CreateFixture();
        fixture.MainPageViewModel.SetCloudFetchResult(CloudFetchStatus.StaleCache, "stale-cache");
        fixture.ArchiveOpenService.ZipSource = new FakeArchiveSource("local.zip");

        await fixture.Service.OpenArchiveAsync(isZip: true, CancellationToken.None);

        Assert.That(fixture.MainPageViewModel.CloudFetchStatus, Is.EqualTo(CloudFetchStatus.None));
        Assert.That(fixture.MainPageViewModel.CloudFetchErrorMessage, Is.Null);
        Assert.That(fixture.SessionService.SetCurrentCallCount, Is.EqualTo(1));
        Assert.That(fixture.Registry.LastDetectSource, Is.TypeOf<FakeArchiveSource>());
    }

    // TP-C025: NoCacheError のときはアーカイブを開かず、エラー状態だけを反映する
    [Test]
    public async Task UT_IT_TP_C025__OpenCloudArchiveAsync_NoCacheError_DoesNotLoadArchive()
    {
        var fixture = CreateFixture();
        fixture.Orchestrator.Result = new CloudFetchResult
        {
            Status = CloudFetchStatus.NoCacheError,
            ErrorMessage = "no-cache"
        };

        var result = await fixture.Service.OpenCloudArchiveAsync(CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.NoCacheError));
        Assert.That(fixture.MainPageViewModel.CloudFetchStatus, Is.EqualTo(CloudFetchStatus.NoCacheError));
        Assert.That(fixture.MainPageViewModel.CloudFetchErrorMessage, Is.EqualTo("no-cache"));
        Assert.That(fixture.Registry.DetectAllCallCount, Is.Zero);
        Assert.That(fixture.LoadService.LoadCallCount, Is.Zero);
        Assert.That(fixture.SessionService.SetCurrentCallCount, Is.Zero);
    }

    private Fixture CreateFixture()
    {
        var sessionService = new FakeArchiveSessionService();
        var mainPageViewModel = new MainPageViewModel(sessionService);
        var overviewViewModel = new ArchiveOverviewViewModel();
        var messageListViewModel = new MessageListViewModel();
        var browseViewModel = new ArchiveBrowseViewModel(
            sessionService,
            messageListViewModel,
            new FakeConversationDateCountService(),
            NullLogger<ArchiveBrowseViewModel>.Instance);
        var searchViewModel = new SearchViewModel(
            sessionService,
            new SearchService(NullLogger<SearchService>.Instance));

        var provider = new FakeArchiveFormatProvider();
        var registry = new FakeArchiveFormatRegistry(provider);
        var loadService = new FakeArchiveLoadService(CreateArchive());
        var archiveOpenService = new FakeArchiveOpenService();
        var orchestrator = new FakeCloudFetchOrchestrator();

        return new Fixture(
            new ArchiveWorkflowService(
                archiveOpenService,
                new FakeBundledSampleLocator(),
                registry,
                loadService,
                sessionService,
                overviewViewModel,
                browseViewModel,
                searchViewModel,
                mainPageViewModel,
                orchestrator,
                NullLogger<ArchiveWorkflowService>.Instance),
            mainPageViewModel,
            sessionService,
            registry,
            loadService,
            archiveOpenService,
            orchestrator);
    }

    private string CreateArchiveZip()
    {
        var root = CreateTempDirectory();
        var sourceDirectory = Path.Combine(root, "archive");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "users.json"), "[]");
        var zipPath = Path.Combine(root, "archive.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(sourceDirectory, zipPath);
        return zipPath;
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"archive-workflow-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        tempDirectories.Add(path);
        return path;
    }

    private static ChatArchive CreateArchive()
        => new()
        {
            FormatId = "fake",
            FormatDisplayName = "Fake",
            Metadata = new ArchiveMetadata
            {
                DisplayName = "Archive",
                TotalMessageCount = 0
            },
            Conversations = [],
            Participants = [],
            Diagnostics = []
        };

    private sealed record Fixture(
        ArchiveWorkflowService Service,
        MainPageViewModel MainPageViewModel,
        FakeArchiveSessionService SessionService,
        FakeArchiveFormatRegistry Registry,
        FakeArchiveLoadService LoadService,
        FakeArchiveOpenService ArchiveOpenService,
        FakeCloudFetchOrchestrator Orchestrator);

    private sealed class FakeCloudFetchOrchestrator : ICloudFetchOrchestrator
    {
        public CloudFetchResult Result { get; set; } = new() { Status = CloudFetchStatus.NoCacheError };

        public Task<CloudFetchResult> FetchLatestAsync(IProgress<CloudFetchProgress>? progress, CancellationToken ct)
        {
            _ = progress;
            _ = ct;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeArchiveOpenService : IArchiveOpenService
    {
        public IArchiveSource? ZipSource { get; set; }

        public IArchiveSource? FolderSource { get; set; }

        public IArchiveSource? BundledSampleSource { get; set; }

        public Task<IArchiveSource?> OpenFolderAsync(CancellationToken ct)
            => OpenFolderAsync(initialFolderPath: null, ct);

        public Task<IArchiveSource?> OpenFolderAsync(string? initialFolderPath, CancellationToken ct)
        {
            _ = initialFolderPath;
            _ = ct;
            return Task.FromResult(FolderSource);
        }

        public Task<IArchiveSource?> OpenZipAsync(CancellationToken ct)
            => OpenZipAsync(initialZipPath: null, ct);

        public Task<IArchiveSource?> OpenZipAsync(string? initialZipPath, CancellationToken ct)
        {
            _ = initialZipPath;
            _ = ct;
            return Task.FromResult(ZipSource);
        }

        public Task<IArchiveSource> OpenBundledSampleAsync(BundledSampleKind kind, CancellationToken ct)
        {
            _ = kind;
            _ = ct;
            return Task.FromResult(BundledSampleSource ?? new FakeArchiveSource("sample"));
        }
    }

    private sealed class FakeBundledSampleLocator : IBundledSampleLocator
    {
        public string SampleFolderPath => "sample-folder";

        public string SampleZipPath => "sample.zip";

        public bool HasSampleFolder => true;

        public bool HasSampleZip => true;
    }

    private sealed class FakeArchiveFormatRegistry(IArchiveFormatProvider provider) : IArchiveFormatRegistry
    {
        public int DetectAllCallCount { get; private set; }

        public IArchiveSource? LastDetectSource { get; private set; }

        public IReadOnlyList<IArchiveFormatProvider> GetAllProviders() => [provider];

        public IArchiveFormatProvider? GetProvider(string formatId)
            => string.Equals(formatId, provider.FormatId, StringComparison.Ordinal) ? provider : null;

        public Task<IReadOnlyList<FormatDetectionResult>> DetectAllAsync(IArchiveSource source, CancellationToken ct)
        {
            _ = ct;
            DetectAllCallCount++;
            LastDetectSource = source;
            return Task.FromResult<IReadOnlyList<FormatDetectionResult>>(
            [
                new FormatDetectionResult
                {
                    FormatId = provider.FormatId,
                    FormatDisplayName = provider.DisplayName,
                    IsDetected = true,
                    Confidence = 1.0
                }
            ]);
        }
    }

    private sealed class FakeArchiveFormatProvider : IArchiveFormatProvider
    {
        public string FormatId => "fake";

        public string DisplayName => "Fake";

        public string Description => "Fake provider";

        public IArchiveFormatDetector CreateDetector() => throw new NotSupportedException();

        public IArchiveParser CreateParser() => throw new NotSupportedException();
    }

    private sealed class FakeArchiveLoadService(ChatArchive archive) : IArchiveLoadService
    {
        public int LoadCallCount { get; private set; }

        public Task<ChatArchive> LoadAsync(IArchiveSource source, IArchiveFormatProvider provider, IProgress<ArchiveLoadProgress>? progress, CancellationToken ct)
        {
            _ = source;
            _ = provider;
            _ = progress;
            _ = ct;
            LoadCallCount++;
            return Task.FromResult(archive);
        }
    }

    private sealed class FakeArchiveSessionService : IArchiveSessionService
    {
        public event EventHandler? ArchiveChanged;

        public ChatArchive? Archive { get; private set; }

        public bool HasArchive => Archive is not null;

        public int SetCurrentCallCount { get; private set; }

        public IArchiveSource? LastSource { get; private set; }

        public IArchiveFormatProvider? LastProvider { get; private set; }

        public Task SetCurrentAsync(IArchiveSource source, IArchiveFormatProvider provider, ChatArchive archive)
        {
            LastSource = source;
            LastProvider = provider;
            Archive = archive;
            SetCurrentCallCount++;
            ArchiveChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ChatMessage>> LoadMessagesAsync(string conversationId, DateOnly? date, CancellationToken ct)
        {
            _ = conversationId;
            _ = date;
            _ = ct;
            return Task.FromResult<IReadOnlyList<ChatMessage>>([]);
        }

        public Task<IReadOnlyList<ChatMessage>> LoadAllMessagesAsync(CancellationToken ct)
        {
            _ = ct;
            return Task.FromResult<IReadOnlyList<ChatMessage>>([]);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeConversationDateCountService : IConversationDateCountService
    {
        public Task<IReadOnlyDictionary<DateOnly, int>> LoadMonthCountsAsync(string conversationId, IReadOnlyCollection<DateOnly> dates, CancellationToken ct)
        {
            _ = conversationId;
            _ = dates;
            _ = ct;
            return Task.FromResult<IReadOnlyDictionary<DateOnly, int>>(new Dictionary<DateOnly, int>());
        }

        public void ClearCache()
        {
        }
    }

    private sealed class FakeArchiveSource(string displayPath) : IArchiveSource
    {
        public string DisplayPath { get; } = displayPath;

        public Task<IReadOnlyList<string>> GetFilesAsync(string relativePath, string pattern, CancellationToken ct)
        {
            _ = relativePath;
            _ = pattern;
            _ = ct;
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<Stream> OpenFileAsync(string relativePath, CancellationToken ct)
        {
            _ = relativePath;
            _ = ct;
            return Task.FromResult<Stream>(new MemoryStream());
        }

        public Task<bool> FileExistsAsync(string relativePath, CancellationToken ct)
        {
            _ = relativePath;
            _ = ct;
            return Task.FromResult(false);
        }

        public Task<bool> DirectoryExistsAsync(string relativePath, CancellationToken ct)
        {
            _ = relativePath;
            _ = ct;
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<string>> GetDirectoriesAsync(string relativePath, CancellationToken ct)
        {
            _ = relativePath;
            _ = ct;
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
