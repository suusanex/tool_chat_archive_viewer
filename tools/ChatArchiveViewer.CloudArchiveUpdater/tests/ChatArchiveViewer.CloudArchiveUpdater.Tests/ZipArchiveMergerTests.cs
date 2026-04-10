using System.IO.Compression;
using ChatArchiveViewer.CloudArchiveUpdater.Services;

namespace ChatArchiveViewer.CloudArchiveUpdater.Tests;

[TestFixture]
public sealed class ZipArchiveMergerTests
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
    public async Task MergeAsync_PreservesExistingEntriesAndOverridesDuplicates()
    {
        var tempDirectory = CreateTempDirectory();
        var sourceZipPath = Path.Combine(tempDirectory, "source.zip");
        var additionalZipPath = Path.Combine(tempDirectory, "additional.zip");
        var destinationZipPath = Path.Combine(tempDirectory, "merged.zip");

        CreateZip(
            sourceZipPath,
            ("folder/a.txt", "A"),
            ("folder/shared.txt", "source"),
            ("root.txt", "root"));
        CreateZip(
            additionalZipPath,
            ("folder/shared.txt", "additional"),
            ("folder/b.txt", "B"));

        var sut = new ZipArchiveMerger();

        await sut.MergeAsync(sourceZipPath, additionalZipPath, destinationZipPath, CancellationToken.None);

        using var stream = File.OpenRead(destinationZipPath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.That(ReadEntry(archive, "folder/a.txt"), Is.EqualTo("A"));
        Assert.That(ReadEntry(archive, "folder/b.txt"), Is.EqualTo("B"));
        Assert.That(ReadEntry(archive, "folder/shared.txt"), Is.EqualTo("additional"));
        Assert.That(ReadEntry(archive, "root.txt"), Is.EqualTo("root"));
        Assert.That(archive.Entries.Count(x => x.FullName == "folder/shared.txt"), Is.EqualTo(1));
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"zip-merge-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        tempDirectories.Add(path);
        return path;
    }

    private static void CreateZip(string zipPath, params (string path, string content)[] entries)
    {
        using var stream = File.Create(zipPath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (path, content) in entries)
        {
            var entry = archive.CreateEntry(path);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
    }

    private static string ReadEntry(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new AssertionException($"Entry '{path}' was not found.");
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
