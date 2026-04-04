using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Models;
using ChatArchiveViewer.Core.Services;
using ChatArchiveViewer.Formats.Slack;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatArchiveViewer.Core.Tests.Services;

[TestFixture]
public sealed class ArchiveFormatRegistryTests
{
    private readonly List<string> tempDirectories = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var tempDirectory in tempDirectories)
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        tempDirectories.Clear();
    }

    // TP-040a: 全登録プロバイダの検出結果が返される
    [Test]
    public async Task UT_IT_040a__DetectAllAsync_ReturnsDetectionResultsForAllProviders()
    {
        var providerA = new StubProvider("a", true, 0.8);
        var providerB = new StubProvider("b", false, 0.1);
        var registry = new ArchiveFormatRegistry(new[] { providerA, providerB });
        var root = CreateEmptyArchiveFolder();
        await using var source = new FolderArchiveSource(root);

        var results = await registry.DetectAllAsync(source, CancellationToken.None);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Any(x => x.FormatId == "a" && x.IsDetected), Is.True);
        Assert.That(results.Any(x => x.FormatId == "b" && !x.IsDetected), Is.True);
    }

    // TP-040c: プロバイダ未登録（空レジストリ）なら結果リストが空
    [Test]
    public async Task UT_IT_040c__DetectAllAsync_EmptyRegistry_ReturnsEmpty()
    {
        var registry = new ArchiveFormatRegistry(Array.Empty<IArchiveFormatProvider>());
        var root = CreateEmptyArchiveFolder();
        await using var source = new FolderArchiveSource(root);

        var results = await registry.DetectAllAsync(source, CancellationToken.None);

        Assert.That(results, Is.Empty);
    }

    // TP-040d: GetAllProviders は登録済み全プロバイダを返す
    [Test]
    public void UT_IT_040d__GetAllProviders_ReturnsAllRegisteredProviders()
    {
        var providerA = new StubProvider("a", true, 0.9);
        var providerB = new StubProvider("b", false, 0.1);
        var registry = new ArchiveFormatRegistry(new[] { providerA, providerB });

        var providers = registry.GetAllProviders();

        Assert.That(providers, Has.Count.EqualTo(2));
        Assert.That(providers.Any(p => p.FormatId == "a"), Is.True);
        Assert.That(providers.Any(p => p.FormatId == "b"), Is.True);
    }

    // TP-040e: GetProvider は存在する ID に対して対応するプロバイダを返す
    [Test]
    public void UT_IT_040e__GetProvider_ExistingId_ReturnsProvider()
    {
        var providerA = new StubProvider("slack-json-export", true, 0.9);
        var registry = new ArchiveFormatRegistry(new[] { providerA });

        var provider = registry.GetProvider("slack-json-export");

        Assert.That(provider, Is.Not.Null);
        Assert.That(provider!.FormatId, Is.EqualTo("slack-json-export"));
    }

    // TP-040f: GetProvider は存在しない ID に null を返す
    [Test]
    public void UT_IT_040f__GetProvider_NonExistingId_ReturnsNull()
    {
        var registry = new ArchiveFormatRegistry(new[] { new StubProvider("a", true, 0.9) });

        var provider = registry.GetProvider("does-not-exist");

        Assert.That(provider, Is.Null);
    }

    // TP-040b: Slack プロバイダ登録済み + 非 Slack ソースで全エントリが IsDetected=false
    [Test]
    public async Task UT_IT_040b__DetectAllAsync_AllProvidersReturnNotDetected_WhenNonMatchingSource()
    {
        var root = CreateNonSlackFolder();
        try
        {
            var registry = new ArchiveFormatRegistry([new SlackFormatProvider(NullLoggerFactory.Instance)]);
            await using var source = new FolderArchiveSource(root);

            var results = await registry.DetectAllAsync(source, CancellationToken.None);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].FormatId, Is.EqualTo(SlackFormatConstants.FormatId));
            Assert.That(results.All(r => !r.IsDetected), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // TP-110d: レジストリ経由で非 Slack ソースを検出すると全件 IsDetected=false
    [Test]
    public async Task UT_IT_110d__DetectAllAsync_WithSlackProviderAndNonSlackSource_ReturnsAllFalse()
    {
        var root = CreateNonSlackFolder();
        try
        {
            var registry = new ArchiveFormatRegistry([new SlackFormatProvider(NullLoggerFactory.Instance)]);
            await using var source = new FolderArchiveSource(root);

            var results = await registry.DetectAllAsync(source, CancellationToken.None);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].FormatId, Is.EqualTo(SlackFormatConstants.FormatId));
            Assert.That(results[0].IsDetected, Is.False);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubProvider : IArchiveFormatProvider
    {
        private readonly bool detected;
        private readonly double confidence;

        public StubProvider(string formatId, bool detected, double confidence)
        {
            FormatId = formatId;
            DisplayName = formatId;
            Description = formatId;
            this.detected = detected;
            this.confidence = confidence;
        }

        public string FormatId { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public IArchiveFormatDetector CreateDetector() => new StubDetector(FormatId, detected, confidence);
        public IArchiveParser CreateParser() => throw new NotImplementedException();
    }

    private sealed class StubDetector(string formatId, bool detected, double confidence) : IArchiveFormatDetector
    {
        public Task<FormatDetectionResult> DetectAsync(IArchiveSource source, CancellationToken ct)
        {
            _ = source;
            _ = ct;
            return Task.FromResult(
                new FormatDetectionResult
                {
                    FormatId = formatId,
                    FormatDisplayName = formatId,
                    IsDetected = detected,
                    Confidence = confidence
                });
        }
    }

    private string CreateNonSlackFolder()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"registry-non-slack-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "readme.txt"), "not a slack export");
        Directory.CreateDirectory(Path.Combine(root, "misc"));
        File.WriteAllText(Path.Combine(root, "misc", "notes.txt"), "hello");
        return root;
    }

    private string CreateEmptyArchiveFolder()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"registry-empty-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        return root;
    }

    private string TrackTempDirectory(string path)
    {
        tempDirectories.Add(path);
        return path;
    }
}
