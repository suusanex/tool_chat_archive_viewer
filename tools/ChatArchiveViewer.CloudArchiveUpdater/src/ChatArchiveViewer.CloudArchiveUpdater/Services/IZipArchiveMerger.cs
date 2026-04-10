using System.IO.Compression;

namespace ChatArchiveViewer.CloudArchiveUpdater.Services;

public interface IZipArchiveMerger
{
    Task MergeAsync(string sourceZipPath, string additionalZipPath, string destinationZipPath, CancellationToken ct);
}

public sealed class ZipArchiveMerger : IZipArchiveMerger
{
    public Task MergeAsync(string sourceZipPath, string additionalZipPath, string destinationZipPath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceZipPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(additionalZipPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationZipPath);
        ct.ThrowIfCancellationRequested();

        var destinationDirectory = Path.GetDirectoryName(destinationZipPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        using var sourceStream = File.OpenRead(sourceZipPath);
        using var additionalStream = File.OpenRead(additionalZipPath);
        using var destinationStream = File.Create(destinationZipPath);
        using var sourceArchive = new ZipArchive(sourceStream, ZipArchiveMode.Read);
        using var additionalArchive = new ZipArchive(additionalStream, ZipArchiveMode.Read);
        using var destinationArchive = new ZipArchive(destinationStream, ZipArchiveMode.Create);

        var additionalEntries = BuildEntryMap(additionalArchive);
        foreach (var entry in BuildEntryMap(sourceArchive).Values)
        {
            ct.ThrowIfCancellationRequested();
            if (!additionalEntries.ContainsKey(entry.FullName))
            {
                CopyEntry(entry, destinationArchive);
            }
        }

        foreach (var entry in additionalEntries.Values)
        {
            ct.ThrowIfCancellationRequested();
            CopyEntry(entry, destinationArchive);
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, ZipArchiveEntry> BuildEntryMap(ZipArchive archive)
    {
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            entries[entry.FullName] = entry;
        }

        return entries;
    }

    private static void CopyEntry(ZipArchiveEntry sourceEntry, ZipArchive destinationArchive)
    {
        var destinationEntry = destinationArchive.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
        destinationEntry.LastWriteTime = sourceEntry.LastWriteTime;
        destinationEntry.ExternalAttributes = sourceEntry.ExternalAttributes;

        if (IsDirectory(sourceEntry))
        {
            return;
        }

        using var sourceStream = sourceEntry.Open();
        using var destinationStream = destinationEntry.Open();
        sourceStream.CopyTo(destinationStream);
    }

    private static bool IsDirectory(ZipArchiveEntry entry)
        => entry.FullName.EndsWith("/", StringComparison.Ordinal);
}
