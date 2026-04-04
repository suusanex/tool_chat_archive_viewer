using Azure.Core;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;
using ChatArchiveViewer.CloudFetch.Services;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.CloudFetch.Tests.Services;

[TestFixture]
public sealed class CloudFetchOrchestratorTests
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

    // TP-C010: 初回起動で正常フローが FreshDownload になる
    [Test]
    public async Task UT_IT_TP_C010__FetchLatestAsync_NoCacheAndSuccess_ReturnsFreshDownload()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: false);
        var logger = new ListLogger<CloudFetchOrchestrator>();
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, destination, _) => WriteTempArchiveAsync(destination),
            verify: (_, _, _) => Task.FromResult(true),
            logger: logger);

        var result = await sut.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.FreshDownload));
        Assert.That(result.CachedZipPath, Is.Not.Null);
        Assert.That(result.Version, Is.EqualTo("2026-04-01-v1"));
        Assert.That(
            sequence,
            Is.EqualTo(
            [
                "bootstrap",
                "auth",
                "manifest",
                "cache:get-state",
                "cache:get-zip",
                "cache:get-temp",
                "download",
                "verify",
                "cache:commit",
                "cache:get-zip"
            ]));
    }

    // TP-C011: version 一致 + キャッシュありならダウンロード不要
    [Test]
    public async Task UT_IT_TP_C011__FetchLatestAsync_CacheHit_ReturnsAlreadyUpToDate()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: true, "2026-04-01-v1");
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, _, _) => throw new AssertionException("Download must not be called."),
            verify: (_, _, _) => throw new AssertionException("Verify must not be called."));

        var result = await sut.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.AlreadyUpToDate));
        Assert.That(sequence, Does.Not.Contain("download"));
        Assert.That(sequence, Does.Not.Contain("verify"));
    }

    // TP-C012: 既存キャッシュより新しい version を検出した場合は再取得してコミットする
    [Test]
    public async Task UT_IT_TP_C012__FetchLatestAsync_VersionChanged_ReturnsFreshDownloadAndCommits()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: true, cacheVersion: "2026-03-31-v1");
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest("2026-04-01-v1")),
            download: (_, _, destination, _) => WriteTempArchiveAsync(destination),
            verify: (_, _, _) => Task.FromResult(true));

        var result = await sut.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.FreshDownload));
        Assert.That(result.Version, Is.EqualTo("2026-04-01-v1"));
        Assert.That(sequence, Does.Contain("download"));
        Assert.That(sequence, Does.Contain("cache:commit"));
    }

    // TP-C013/027a: bootstrap 失敗時にキャッシュへフォールバックし例外詳細をログ出力
    [Test]
    public async Task UT_IT_TP_C013_TP_C027a__FetchLatestAsync_BootstrapFailsWithCache_ReturnsStaleAndLogsException()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: true, "cached-version");
        var logger = new ListLogger<CloudFetchOrchestrator>();
        var exception = new HttpRequestException("network-down");
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => throw exception,
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true),
            logger: logger);

        var result = await sut.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.StaleCache));
        Assert.That(logger.Messages.Any(x => x.Contains(exception.ToString(), StringComparison.Ordinal)), Is.True);
    }

    // TP-C014: bootstrap 失敗 + キャッシュなしは NoCacheError
    [Test]
    public async Task UT_IT_TP_C014__FetchLatestAsync_BootstrapFailsWithoutCache_ReturnsNoCacheError()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: false);
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => throw new HttpRequestException("network-down"),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true));

        var result = await sut.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.NoCacheError));
        Assert.That(result.CachedZipPath, Is.Null);
        Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
    }

    // TP-C015/016: ハッシュ不一致時は temp 削除しキャッシュ有無で分岐
    [Test]
    public async Task UT_IT_TP_C015_TP_C016__FetchLatestAsync_HashMismatch_DeletesTempAndFallsBackByCacheState()
    {
        var tempRootWithCache = CreateTempDirectory();
        var seqWithCache = new List<string>();
        var cacheWithCache = new FakeCacheManager(tempRootWithCache, seqWithCache, hasCache: true, "cached-version");
        var sutWithCache = CreateSut(
            cacheWithCache,
            seqWithCache,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest("new-version")),
            download: (_, _, destination, _) => WriteTempArchiveAsync(destination),
            verify: (_, _, _) => Task.FromResult(false));

        var resultWithCache = await sutWithCache.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(resultWithCache.Status, Is.EqualTo(CloudFetchStatus.StaleCache));
        Assert.That(File.Exists(cacheWithCache.LastTempPath), Is.False);

        var tempRootNoCache = CreateTempDirectory();
        var seqNoCache = new List<string>();
        var cacheNoCache = new FakeCacheManager(tempRootNoCache, seqNoCache, hasCache: false);
        var sutNoCache = CreateSut(
            cacheNoCache,
            seqNoCache,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest("new-version")),
            download: (_, _, destination, _) => WriteTempArchiveAsync(destination),
            verify: (_, _, _) => Task.FromResult(false));

        var resultNoCache = await sutNoCache.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(resultNoCache.Status, Is.EqualTo(CloudFetchStatus.NoCacheError));
        Assert.That(File.Exists(cacheNoCache.LastTempPath), Is.False);
    }

    // TP-C017/027b: 認証キャンセル時はキャッシュにフォールバックし例外詳細をログ出力する
    [Test]
    public async Task UT_IT_TP_C017_TP_C027b__FetchLatestAsync_AuthenticationCanceledWithCache_ReturnsStaleCache()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: true, cacheVersion: "cached-version");
        var logger = new ListLogger<CloudFetchOrchestrator>();
        var exception = new OperationCanceledException("Authentication was canceled by user.");
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => throw exception,
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true),
            logger: logger);

        var result = await sut.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.StaleCache));
        Assert.That(result.CachedZipPath, Is.Not.Null);
        Assert.That(logger.Messages.Any(x => x.Contains(exception.ToString(), StringComparison.Ordinal)), Is.True);
    }

    // TP-C018: 認証エラー時はキャッシュなしなら NoCacheError を返す
    [Test]
    public async Task UT_IT_TP_C018__FetchLatestAsync_AuthenticationFailureWithoutCache_ReturnsNoCacheError()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: false);
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => throw new InvalidOperationException("Authentication failed."),
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true));

        var result = await sut.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(CloudFetchStatus.NoCacheError));
        Assert.That(result.CachedZipPath, Is.Null);
        Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
    }

    // TP-C017/018: 認証例外でもキャッシュ有無で同じフォールバック規則が適用される
    [Test]
    public async Task UT_IT_TP_C017_TP_C018__FetchLatestAsync_AuthenticationFailure_FallsBackByCacheState()
    {
        var withCacheRoot = CreateTempDirectory();
        var withCacheSequence = new List<string>();
        var withCache = new FakeCacheManager(withCacheRoot, withCacheSequence, hasCache: true, cacheVersion: "cached-version");
        var sutWithCache = CreateSut(
            withCache,
            withCacheSequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => throw new InvalidOperationException("Authentication failed."),
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true));

        var staleResult = await sutWithCache.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(staleResult.Status, Is.EqualTo(CloudFetchStatus.StaleCache));
        Assert.That(staleResult.CachedZipPath, Is.Not.Null);

        var withoutCacheRoot = CreateTempDirectory();
        var withoutCacheSequence = new List<string>();
        var withoutCache = new FakeCacheManager(withoutCacheRoot, withoutCacheSequence, hasCache: false);
        var sutWithoutCache = CreateSut(
            withoutCache,
            withoutCacheSequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => throw new OperationCanceledException("Authentication was canceled by user."),
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true));

        var noCacheResult = await sutWithoutCache.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(noCacheResult.Status, Is.EqualTo(CloudFetchStatus.NoCacheError));
        Assert.That(noCacheResult.CachedZipPath, Is.Null);
    }

    // TP-C019/027c: manifest 取得失敗はキャッシュ有無でフォールバック結果が分岐する
    [Test]
    public async Task UT_IT_TP_C019_TP_C027c__FetchLatestAsync_ManifestFailure_FallsBackByCacheState()
    {
        var withCacheRoot = CreateTempDirectory();
        var withCacheSequence = new List<string>();
        var withCache = new FakeCacheManager(withCacheRoot, withCacheSequence, hasCache: true, cacheVersion: "cached-version");
        var logger = new ListLogger<CloudFetchOrchestrator>();
        var manifestException = new InvalidDataException("manifest failure");
        var sutWithCache = CreateSut(
            withCache,
            withCacheSequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => throw manifestException,
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true),
            logger: logger);

        var staleResult = await sutWithCache.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(staleResult.Status, Is.EqualTo(CloudFetchStatus.StaleCache));
        Assert.That(logger.Messages.Any(x => x.Contains(manifestException.ToString(), StringComparison.Ordinal)), Is.True);

        var withoutCacheRoot = CreateTempDirectory();
        var withoutCacheSequence = new List<string>();
        var withoutCache = new FakeCacheManager(withoutCacheRoot, withoutCacheSequence, hasCache: false);
        var sutWithoutCache = CreateSut(
            withoutCache,
            withoutCacheSequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => throw new InvalidDataException("manifest failure"),
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true));

        var noCacheResult = await sutWithoutCache.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(noCacheResult.Status, Is.EqualTo(CloudFetchStatus.NoCacheError));
    }

    // TP-C020/027d: ダウンロード失敗時は temp を削除しキャッシュ有無でフォールバックする
    [Test]
    public async Task UT_IT_TP_C020_TP_C027d__FetchLatestAsync_DownloadFailure_DeletesTempAndFallsBackByCacheState()
    {
        var withCacheRoot = CreateTempDirectory();
        var withCacheSequence = new List<string>();
        var withCache = new FakeCacheManager(withCacheRoot, withCacheSequence, hasCache: true, cacheVersion: "cached-version");
        var logger = new ListLogger<CloudFetchOrchestrator>();
        var downloadException = new HttpRequestException("download failed");
        var sutWithCache = CreateSut(
            withCache,
            withCacheSequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest("new-version")),
            download: (_, _, destination, _) =>
            {
                File.WriteAllText(destination, "partial");
                throw downloadException;
            },
            verify: (_, _, _) => Task.FromResult(true),
            logger: logger);

        var staleResult = await sutWithCache.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(staleResult.Status, Is.EqualTo(CloudFetchStatus.StaleCache));
        Assert.That(File.Exists(withCache.LastTempPath), Is.False);
        Assert.That(logger.Messages.Any(x => x.Contains(downloadException.ToString(), StringComparison.Ordinal)), Is.True);

        var withoutCacheRoot = CreateTempDirectory();
        var withoutCacheSequence = new List<string>();
        var withoutCache = new FakeCacheManager(withoutCacheRoot, withoutCacheSequence, hasCache: false);
        var sutWithoutCache = CreateSut(
            withoutCache,
            withoutCacheSequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest("new-version")),
            download: (_, _, destination, _) =>
            {
                File.WriteAllText(destination, "partial");
                throw new UnauthorizedAccessException("403");
            },
            verify: (_, _, _) => Task.FromResult(true));

        var noCacheResult = await sutWithoutCache.FetchLatestAsync(progress: null, CancellationToken.None);

        Assert.That(noCacheResult.Status, Is.EqualTo(CloudFetchStatus.NoCacheError));
        Assert.That(File.Exists(withoutCache.LastTempPath), Is.False);
    }

    // TP-C021: キャンセル時は OperationCanceledException を返す
    [Test]
    public void UT_IT_TP_C021__FetchLatestAsync_Canceled_ThrowsOperationCanceledException()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: false);
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult<TokenCredential>(new StaticTokenCredential());
            },
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, _, _) => Task.CompletedTask,
            verify: (_, _, _) => Task.FromResult(true));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => sut.FetchLatestAsync(progress: null, cts.Token));
    }

    // TP-C021b/c: ダウンロード中キャンセル時は temp を削除して再送出する
    [Test]
    public void UT_IT_TP_C021__FetchLatestAsync_DownloadCanceled_DeletesTempAndRethrows()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: false);
        using var cts = new CancellationTokenSource();
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest("new-version")),
            download: (_, _, destination, ct) =>
            {
                File.WriteAllText(destination, "partial");
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            },
            verify: (_, _, _) => Task.FromResult(true));

        Assert.ThrowsAsync<OperationCanceledException>(() => sut.FetchLatestAsync(progress: null, cts.Token));
        Assert.That(File.Exists(cache.LastTempPath), Is.False);
    }

    // TP-C022: progress が各フェーズで通知される
    [Test]
    public async Task UT_IT_TP_C022__FetchLatestAsync_ReportsProgressStages()
    {
        var tempRoot = CreateTempDirectory();
        var sequence = new List<string>();
        var cache = new FakeCacheManager(tempRoot, sequence, hasCache: false);
        var progressEvents = new List<CloudFetchProgressStage>();
        var sut = CreateSut(
            cache,
            sequence,
            bootstrap: () => Task.FromResult(CreateBootstrapConfig()),
            auth: (_, _) => Task.FromResult<TokenCredential>(new StaticTokenCredential()),
            manifest: (_, _, _) => Task.FromResult(CreateManifest()),
            download: (_, _, destination, _) => WriteTempArchiveAsync(destination),
            verify: (_, _, _) => Task.FromResult(true));

        await sut.FetchLatestAsync(new SynchronousProgress(progressEvents), CancellationToken.None);

        Assert.That(progressEvents, Does.Contain(CloudFetchProgressStage.Bootstrap));
        Assert.That(progressEvents, Does.Contain(CloudFetchProgressStage.Authentication));
        Assert.That(progressEvents, Does.Contain(CloudFetchProgressStage.Manifest));
        Assert.That(progressEvents, Does.Contain(CloudFetchProgressStage.Download));
        Assert.That(progressEvents, Does.Contain(CloudFetchProgressStage.Verify));
        Assert.That(progressEvents, Does.Contain(CloudFetchProgressStage.Commit));
        Assert.That(progressEvents.Last(), Is.EqualTo(CloudFetchProgressStage.Completed));
    }

    private static CloudFetchOrchestrator CreateSut(
        FakeCacheManager cacheManager,
        List<string> sequence,
        Func<Task<BootstrapConfig>> bootstrap,
        Func<BootstrapConfig, CancellationToken, Task<TokenCredential>> auth,
        Func<BootstrapConfig, TokenCredential, CancellationToken, Task<CloudManifest>> manifest,
        Func<CloudManifest, TokenCredential, string, CancellationToken, Task> download,
        Func<string, string, CancellationToken, Task<bool>> verify,
        ListLogger<CloudFetchOrchestrator>? logger = null)
    {
        return new CloudFetchOrchestrator(
            new DelegateBootstrapProvider(sequence, bootstrap),
            new DelegateAuthService(sequence, auth),
            new DelegateManifestProvider(sequence, manifest),
            new DelegateDownloader(sequence, download),
            cacheManager,
            new DelegateHashVerifier(sequence, verify),
            logger ?? new ListLogger<CloudFetchOrchestrator>());
    }

    private static BootstrapConfig CreateBootstrapConfig()
        => new()
        {
            TenantId = "tenant",
            ClientId = "client",
            ManifestUrl = "https://example.com/manifest.json",
            Authority = "https://login.microsoftonline.com/tenant",
            Scopes = ["scope"]
        };

    private static CloudManifest CreateManifest(string version = "2026-04-01-v1")
        => new()
        {
            Version = version,
            DownloadUrl = "archive.zip",
            Sha256 = "abc123",
            DownloadUri = new Uri("https://example.com/archive.zip")
        };

    private static Task WriteTempArchiveAsync(string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        return File.WriteAllTextAsync(destinationPath, "zip");
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cloud-orchestrator-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        tempDirectories.Add(path);
        return path;
    }

    private sealed class DelegateBootstrapProvider(List<string> sequence, Func<Task<BootstrapConfig>> handler) : IBootstrapConfigProvider
    {
        public async Task<BootstrapConfig> GetConfigAsync(CancellationToken ct)
        {
            _ = ct;
            sequence.Add("bootstrap");
            return await handler();
        }
    }

    private sealed class DelegateAuthService(List<string> sequence, Func<BootstrapConfig, CancellationToken, Task<TokenCredential>> handler) : ICloudAuthService
    {
        public async Task<TokenCredential> AuthenticateAsync(BootstrapConfig config, CancellationToken ct)
        {
            sequence.Add("auth");
            return await handler(config, ct);
        }
    }

    private sealed class DelegateManifestProvider(
        List<string> sequence,
        Func<BootstrapConfig, TokenCredential, CancellationToken, Task<CloudManifest>> handler) : ICloudManifestProvider
    {
        public async Task<CloudManifest> GetManifestAsync(BootstrapConfig config, TokenCredential credential, CancellationToken ct)
        {
            sequence.Add("manifest");
            return await handler(config, credential, ct);
        }
    }

    private sealed class DelegateDownloader(List<string> sequence, Func<CloudManifest, TokenCredential, string, CancellationToken, Task> handler)
        : ICloudArchiveDownloader
    {
        public Task DownloadAsync(CloudManifest manifest, TokenCredential credential, string destinationPath, CancellationToken ct)
        {
            sequence.Add("download");
            return handler(manifest, credential, destinationPath, ct);
        }
    }

    private sealed class DelegateHashVerifier(List<string> sequence, Func<string, string, CancellationToken, Task<bool>> handler)
        : IHashVerifier
    {
        public async Task<HashVerifyResult> VerifyAsync(string filePath, string expectedSha256, CancellationToken ct)
        {
            sequence.Add("verify");
            var matched = await handler(filePath, expectedSha256, ct);
            return new HashVerifyResult(matched, matched ? expectedSha256 : "mismatch-actual-hash");
        }
    }

    private sealed class StaticTokenCredential : TokenCredential
    {
        private static readonly AccessToken AccessToken = new("token", DateTimeOffset.UtcNow.AddHours(1));

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            _ = requestContext;
            _ = cancellationToken;
            return AccessToken;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            _ = requestContext;
            _ = cancellationToken;
            return ValueTask.FromResult(AccessToken);
        }
    }

    private sealed class FakeCacheManager : ICacheManager
    {
        private readonly List<string> sequence;
        private readonly string zipPath;
        private CacheState? currentState;

        public FakeCacheManager(string root, List<string> sequence, bool hasCache, string cacheVersion = "cached-version")
        {
            this.sequence = sequence;
            CacheDirectory = root;
            zipPath = Path.Combine(root, "current.zip");
            LastTempPath = Path.Combine(root, "downloading.zip.tmp");
            if (hasCache)
            {
                File.WriteAllText(zipPath, "cached");
                currentState = new CacheState
                {
                    Version = cacheVersion,
                    Sha256 = "cache-sha",
                    DownloadedAt = DateTimeOffset.UtcNow
                };
            }
        }

        public string CacheDirectory { get; }

        public string LastTempPath { get; private set; }

        public Task<CacheState?> GetCurrentStateAsync(CancellationToken ct)
        {
            _ = ct;
            sequence.Add("cache:get-state");
            return Task.FromResult(currentState);
        }

        public Task<string> GetTempDownloadPathAsync(CancellationToken ct)
        {
            _ = ct;
            sequence.Add("cache:get-temp");
            if (File.Exists(LastTempPath))
            {
                File.Delete(LastTempPath);
            }

            return Task.FromResult(LastTempPath);
        }

        public Task CommitDownloadAsync(string tempPath, string version, string sha256, CancellationToken ct)
        {
            _ = ct;
            sequence.Add("cache:commit");
            File.Copy(tempPath, zipPath, overwrite: true);
            File.Delete(tempPath);
            currentState = new CacheState
            {
                Version = version,
                Sha256 = sha256,
                DownloadedAt = DateTimeOffset.UtcNow
            };
            return Task.CompletedTask;
        }

        public string? GetCurrentZipPath()
        {
            sequence.Add("cache:get-zip");
            return File.Exists(zipPath) ? zipPath : null;
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _ = logLevel;
            _ = eventId;
            Messages.Add(formatter(state, exception));
            if (exception is not null)
            {
                Messages.Add(exception.ToString());
            }
        }
    }

    private sealed class SynchronousProgress(List<CloudFetchProgressStage> sink) : IProgress<CloudFetchProgress>
    {
        public void Report(CloudFetchProgress value)
        {
            sink.Add(value.Stage);
        }
    }
}
