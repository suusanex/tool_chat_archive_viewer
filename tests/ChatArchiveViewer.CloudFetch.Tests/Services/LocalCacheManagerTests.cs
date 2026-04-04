using System.Text.Json;
using ChatArchiveViewer.CloudFetch.Models;
using ChatArchiveViewer.CloudFetch.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatArchiveViewer.CloudFetch.Tests.Services;

[TestFixture]
public sealed class LocalCacheManagerTests
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

    // TP-C008a/b: キャッシュがないときは state/path とも null
    [Test]
    public async Task UT_IT_TP_C008__GetCurrentStateAndZipPath_NoCache_ReturnsNull()
    {
        var cacheRoot = CreateTempDirectory();
        var sut = new LocalCacheManager(NullLogger<LocalCacheManager>.Instance, cacheRoot);

        var state = await sut.GetCurrentStateAsync(CancellationToken.None);
        var zipPath = sut.GetCurrentZipPath();

        Assert.That(state, Is.Null);
        Assert.That(zipPath, Is.Null);
    }

    // TP-C008c/d/e: 正常なキャッシュと zip 不整合を読み取れる
    [Test]
    public async Task UT_IT_TP_C008__GetCurrentStateAndZipPath_ValidCacheAndMissingZip_AreHandled()
    {
        var cacheRoot = CreateTempDirectory();
        Directory.CreateDirectory(cacheRoot);
        var state = new CacheState
        {
            Version = "2026-04-01-v1",
            Sha256 = "abc123",
            DownloadedAt = DateTimeOffset.Parse("2026-04-01T12:34:56Z"),
            BootstrapUrl = "https://example.invalid/bootstrap.json"
        };
        await File.WriteAllTextAsync(
            Path.Combine(cacheRoot, "cache-state.json"),
            JsonSerializer.Serialize(state));
        await File.WriteAllTextAsync(Path.Combine(cacheRoot, "current.zip"), "zip");

        var sut = new LocalCacheManager(NullLogger<LocalCacheManager>.Instance, cacheRoot);

        var loadedState = await sut.GetCurrentStateAsync(CancellationToken.None);
        var zipPath = sut.GetCurrentZipPath();

        Assert.That(loadedState, Is.Not.Null);
        Assert.That(loadedState!.Version, Is.EqualTo("2026-04-01-v1"));
        Assert.That(zipPath, Is.Not.Null.And.EndsWith("current.zip"));

        File.Delete(zipPath!);

        Assert.That(sut.GetCurrentZipPath(), Is.Null);
    }

    // TP-C009b/c/d: コミットで current.zip と cache-state.json が更新され temp が消える
    [Test]
    public async Task UT_IT_TP_C009__CommitDownloadAsync_CreatesZipAndStateAndRemovesTemp()
    {
        var cacheRoot = CreateTempDirectory();
        var sut = new LocalCacheManager(NullLogger<LocalCacheManager>.Instance, cacheRoot);
        var tempPath = await sut.GetTempDownloadPathAsync(CancellationToken.None);
        Assert.That(Path.GetFileName(tempPath), Is.EqualTo("downloading.zip.tmp"));
        await File.WriteAllTextAsync(tempPath, "dummy zip");

        await sut.CommitDownloadAsync(tempPath, "2026-04-01-v1", "abc123", CancellationToken.None);

        var zipPath = sut.GetCurrentZipPath();
        Assert.That(zipPath, Is.Not.Null);
        Assert.That(File.Exists(zipPath!), Is.True);
        Assert.That(File.Exists(tempPath), Is.False);

        var state = await sut.GetCurrentStateAsync(CancellationToken.None);
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Version, Is.EqualTo("2026-04-01-v1"));
        Assert.That(state.Sha256, Is.EqualTo("abc123"));
    }

    // TP-C009e/f/g: 既存キャッシュ上書きと temp 残骸除去を検証する
    [Test]
    public async Task UT_IT_TP_C009__CommitDownloadAsync_OverwritesExistingCacheAndCleansResidualTemp()
    {
        var cacheRoot = CreateTempDirectory();
        var sut = new LocalCacheManager(NullLogger<LocalCacheManager>.Instance, cacheRoot);
        Directory.CreateDirectory(cacheRoot);
        await File.WriteAllTextAsync(Path.Combine(cacheRoot, "current.zip"), "old zip");
        await File.WriteAllTextAsync(Path.Combine(cacheRoot, "cache-state.json"), """{"version":"old","sha256":"old","downloadedAt":"2026-04-01T00:00:00Z"}""");
        await File.WriteAllTextAsync(Path.Combine(cacheRoot, "downloading.zip.tmp"), "stale temp");

        var tempPath = await sut.GetTempDownloadPathAsync(CancellationToken.None);

        Assert.That(File.Exists(tempPath), Is.False);

        await File.WriteAllTextAsync(tempPath, "new zip");
        await sut.CommitDownloadAsync(tempPath, "new-version", "new-sha", CancellationToken.None);

        var state = await sut.GetCurrentStateAsync(CancellationToken.None);
        var zipPath = sut.GetCurrentZipPath();

        Assert.That(state, Is.Not.Null);
        Assert.That(state!.Version, Is.EqualTo("new-version"));
        Assert.That(state.Sha256, Is.EqualTo("new-sha"));
        Assert.That(zipPath, Is.Not.Null);
        Assert.That(await File.ReadAllTextAsync(zipPath!), Is.EqualTo("new zip"));
    }

    // TP-C008f/g: 壊れた cache-state.json は null 扱いで継続できる
    [Test]
    public async Task UT_IT_TP_C008__GetCurrentStateAsync_BrokenJson_ReturnsNull()
    {
        var cacheRoot = CreateTempDirectory();
        Directory.CreateDirectory(cacheRoot);
        await File.WriteAllTextAsync(Path.Combine(cacheRoot, "cache-state.json"), "{ broken");
        var sut = new LocalCacheManager(NullLogger<LocalCacheManager>.Instance, cacheRoot);

        var state = await sut.GetCurrentStateAsync(CancellationToken.None);

        Assert.That(state, Is.Null);
    }

    [Test]
    public async Task UT_IT_TP_C008__GetCurrentStateAsync_EmptyStateFile_ReturnsNull()
    {
        var cacheRoot = CreateTempDirectory();
        Directory.CreateDirectory(cacheRoot);
        await File.WriteAllTextAsync(Path.Combine(cacheRoot, "cache-state.json"), string.Empty);
        var sut = new LocalCacheManager(NullLogger<LocalCacheManager>.Instance, cacheRoot);

        var state = await sut.GetCurrentStateAsync(CancellationToken.None);

        Assert.That(state, Is.Null);
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cloud-cache-tests-{Guid.NewGuid():N}");
        tempDirectories.Add(path);
        Directory.CreateDirectory(path);
        return path;
    }
}
