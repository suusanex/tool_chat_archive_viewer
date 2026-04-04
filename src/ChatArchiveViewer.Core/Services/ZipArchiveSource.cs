using System.IO.Compression;
using ChatArchiveViewer.Core.Abstractions;

namespace ChatArchiveViewer.Core.Services;

public sealed class ZipArchiveSource : IArchiveSource
{
    private readonly string zipPath;
    private readonly string extractRoot;

    public ZipArchiveSource(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
        {
            throw new ArgumentException("Zip path is required.", nameof(zipPath));
        }

        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("Archive zip file not found.", zipPath);
        }

        this.zipPath = Path.GetFullPath(zipPath);
        extractRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"chat-archive-viewer-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(extractRoot);
        DisplayPath = this.zipPath;

        try
        {
            ExtractSafely();
        }
        catch
        {
            DeleteExtractRoot();
            throw;
        }
    }

    public string DisplayPath { get; }

    public Task<IReadOnlyList<string>> GetFilesAsync(string relativePath, string pattern, CancellationToken ct)
        => new FolderArchiveSource(extractRoot).GetFilesAsync(relativePath, pattern, ct);

    public Task<Stream> OpenFileAsync(string relativePath, CancellationToken ct)
        => new FolderArchiveSource(extractRoot).OpenFileAsync(relativePath, ct);

    public Task<bool> FileExistsAsync(string relativePath, CancellationToken ct)
        => new FolderArchiveSource(extractRoot).FileExistsAsync(relativePath, ct);

    public Task<bool> DirectoryExistsAsync(string relativePath, CancellationToken ct)
        => new FolderArchiveSource(extractRoot).DirectoryExistsAsync(relativePath, ct);

    public Task<IReadOnlyList<string>> GetDirectoriesAsync(string relativePath, CancellationToken ct)
        => new FolderArchiveSource(extractRoot).GetDirectoriesAsync(relativePath, ct);

    public ValueTask DisposeAsync()
    {
        DeleteExtractRoot();

        return ValueTask.CompletedTask;
    }

    private void DeleteExtractRoot()
    {
        if (Directory.Exists(extractRoot))
        {
            Directory.Delete(extractRoot, recursive: true);
        }
    }

    private void ExtractSafely()
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var destinationPath = Path.GetFullPath(Path.Combine(extractRoot, entry.FullName));
            var relativePath = Path.GetRelativePath(extractRoot, destinationPath);
            if (relativePath.Equals("..", StringComparison.Ordinal) ||
                relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                Path.IsPathRooted(relativePath))
            {
                throw new InvalidDataException($"Invalid zip entry path: {entry.FullName}");
            }

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }
}
