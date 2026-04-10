using Azure.Core;
using ChatArchiveViewer.CloudArchiveUpdater.Models;
using ChatArchiveViewer.CloudArchiveUpdater.Services;

namespace ChatArchiveViewer.CloudArchiveUpdater.Tests;

[TestFixture]
public sealed class ArchiveUpdaterTests
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

    [Test]
    public async Task UpdateAsync_DownloadsMergesUploadsArchiveAndManifest()
    {
        var workingDirectory = CreateTempDirectory();
        var additionalZipPath = Path.Combine(workingDirectory, "additional.zip");
        CreateZip(
            additionalZipPath,
            ("keep/new.json", "{\"fresh\":true}"),
            ("shared.json", "{\"source\":\"additional\"}"));

        var uploadedFiles = new List<(Uri uri, string contentType, string content)>();
        var sut = new ArchiveUpdater(
            new StubBootstrapConfigProvider(),
            new StubCloudAuthService(),
            new StubManifestProvider(),
            new StubArchiveDownloader(),
            new ZipArchiveMerger(),
            new DelegateBlobFileUploader(
                async (uri, _, sourcePath, contentType, _) =>
                {
                    var content = string.Equals(contentType, "application/zip", StringComparison.Ordinal)
                        ? ReadZipEntries(sourcePath)
                        : await File.ReadAllTextAsync(sourcePath);
                    uploadedFiles.Add((uri, contentType, content));
                }),
            new StubFileHasher("deadbeef"),
            new StubManifestVersionGenerator("2026-04-10-v3"),
            new StubClock(new DateTimeOffset(2026, 04, 10, 12, 34, 56, TimeSpan.Zero)));

        var result = await sut.UpdateAsync(new ArchiveUpdaterOptions { AdditionalZipPath = additionalZipPath }, CancellationToken.None);

        Assert.That(result.Version, Is.EqualTo("2026-04-10-v3"));
        Assert.That(result.Sha256, Is.EqualTo("deadbeef"));
        Assert.That(uploadedFiles, Has.Count.EqualTo(2));
        Assert.That(uploadedFiles.Single(x => x.contentType == "application/zip").content, Does.Contain("keep/original.json={\"source\":\"original\"}"));
        Assert.That(uploadedFiles.Single(x => x.contentType == "application/zip").content, Does.Contain("keep/new.json={\"fresh\":true}"));
        Assert.That(uploadedFiles.Single(x => x.contentType == "application/zip").content, Does.Contain("shared.json={\"source\":\"additional\"}"));

        var manifestUpload = uploadedFiles.Single(x => x.contentType == "application/json");
        Assert.That(manifestUpload.content, Does.Contain("\"version\": \"2026-04-10-v3\""));
        Assert.That(manifestUpload.content, Does.Contain("\"sha256\": \"deadbeef\""));
        Assert.That(manifestUpload.content, Does.Contain("\"downloadUrl\": \"archive.zip\""));
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"archive-updater-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        tempDirectories.Add(path);
        return path;
    }

    private static void CreateZip(string zipPath, params (string path, string content)[] entries)
    {
        using var stream = File.Create(zipPath);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create);
        foreach (var (path, content) in entries)
        {
            var entry = archive.CreateEntry(path);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
    }

    private static string ReadZipEntries(string zipPath)
    {
        using var stream = File.OpenRead(zipPath);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        var values = archive.Entries
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .Select(
                entry =>
                {
                    using var reader = new StreamReader(entry.Open());
                    return $"{entry.FullName}={reader.ReadToEnd()}";
                });
        return string.Join("|", values);
    }

    private sealed class StubBootstrapConfigProvider : IBootstrapConfigProvider
    {
        public Task<BootstrapConfig> GetConfigAsync(CancellationToken ct)
        {
            _ = ct;
            return Task.FromResult(
                new BootstrapConfig
                {
                    TenantId = "tenant",
                    ClientId = "client",
                    Authority = "https://login.microsoftonline.com/tenant",
                    ManifestUrl = "https://example.com/manifest.json",
                    Scopes = ["https://storage.azure.com/.default"]
                });
        }
    }

    private sealed class StubCloudAuthService : ICloudAuthService
    {
        public Task<TokenCredential> AuthenticateAsync(BootstrapConfig config, CancellationToken ct)
        {
            _ = config;
            _ = ct;
            return Task.FromResult<TokenCredential>(new StaticTokenCredential());
        }
    }

    private sealed class StubManifestProvider : ICloudManifestProvider
    {
        public Task<CloudManifest> GetManifestAsync(BootstrapConfig config, TokenCredential credential, CancellationToken ct)
        {
            _ = config;
            _ = credential;
            _ = ct;
            return Task.FromResult(
                new CloudManifest
                {
                    Version = "2026-04-09-v2",
                    DownloadUrl = "archive.zip",
                    Sha256 = "oldhash",
                    PublishedAt = new DateTimeOffset(2026, 04, 09, 0, 0, 0, TimeSpan.Zero),
                    DownloadUri = new Uri("https://example.com/archive.zip")
                });
        }
    }

    private sealed class StubArchiveDownloader : ICloudArchiveDownloader
    {
        public Task DownloadAsync(CloudManifest manifest, TokenCredential credential, string destinationPath, CancellationToken ct)
        {
            _ = manifest;
            _ = credential;
            _ = ct;
            CreateZip(
                destinationPath,
                ("keep/original.json", "{\"source\":\"original\"}"),
                ("shared.json", "{\"source\":\"original\"}"));
            return Task.CompletedTask;
        }
    }

    private sealed class DelegateBlobFileUploader(
        Func<Uri, TokenCredential, string, string, CancellationToken, Task> handler) : IBlobFileUploader
    {
        public Task UploadAsync(Uri blobUri, TokenCredential credential, string sourcePath, string contentType, CancellationToken ct)
            => handler(blobUri, credential, sourcePath, contentType, ct);
    }

    private sealed class StubFileHasher(string sha256) : IFileHasher
    {
        public Task<string> ComputeSha256Async(string path, CancellationToken ct)
        {
            _ = path;
            _ = ct;
            return Task.FromResult(sha256);
        }
    }

    private sealed class StubManifestVersionGenerator(string version) : IManifestVersionGenerator
    {
        public string CreateNextVersion(string currentVersion, DateTimeOffset publishedAtUtc)
        {
            _ = currentVersion;
            _ = publishedAtUtc;
            return version;
        }
    }

    private sealed class StubClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset GetUtcNow()
            => value;
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
}
