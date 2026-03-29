using ChatArchiveViewer.Core.Abstractions;

namespace ChatArchiveViewer.Core.Services;

public sealed class FolderArchiveSource : IArchiveSource
{
    private readonly string rootPath;

    public FolderArchiveSource(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path is required.", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Archive folder not found: {folderPath}");
        }

        rootPath = Path.GetFullPath(folderPath);
        DisplayPath = rootPath;
    }

    public string DisplayPath { get; }

    public async Task<IReadOnlyList<string>> GetFilesAsync(string relativePath, string pattern, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var basePath = ResolveDirectory(relativePath);
        if (!Directory.Exists(basePath))
        {
            return Array.Empty<string>();
        }

        return await Task.Run(
            () => (IReadOnlyList<string>)Directory
                .EnumerateFiles(basePath, pattern, SearchOption.TopDirectoryOnly)
                .Select(path => ToRelative(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            ct);
    }

    public async Task<Stream> OpenFileAsync(string relativePath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var absolutePath = ResolveFile(relativePath);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("File not found in archive source.", absolutePath);
        }

        return await Task.Run(
            () => (Stream)new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read),
            ct);
    }

    public Task<bool> FileExistsAsync(string relativePath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var absolutePath = ResolveFile(relativePath);
        return Task.FromResult(File.Exists(absolutePath));
    }

    public Task<bool> DirectoryExistsAsync(string relativePath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var absolutePath = ResolveDirectory(relativePath);
        return Task.FromResult(Directory.Exists(absolutePath));
    }

    public async Task<IReadOnlyList<string>> GetDirectoriesAsync(string relativePath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var basePath = ResolveDirectory(relativePath);
        if (!Directory.Exists(basePath))
        {
            return Array.Empty<string>();
        }

        return await Task.Run(
            () => (IReadOnlyList<string>)Directory
                .EnumerateDirectories(basePath, "*", SearchOption.TopDirectoryOnly)
                .Select(path => ToRelative(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private string ResolveDirectory(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return rootPath;
        }

        return EnsureUnderRoot(Path.GetFullPath(Path.Combine(rootPath, NormalizeRelative(relativePath))));
    }

    private string ResolveFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative file path is required.", nameof(relativePath));
        }

        return EnsureUnderRoot(Path.GetFullPath(Path.Combine(rootPath, NormalizeRelative(relativePath))));
    }

    private string EnsureUnderRoot(string fullPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        if (relativePath.Equals("..", StringComparison.Ordinal) ||
            relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Path escape is not allowed.");
        }

        return fullPath;
    }

    private string ToRelative(string fullPath)
    {
        return Path.GetRelativePath(rootPath, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string NormalizeRelative(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar);
    }
}
