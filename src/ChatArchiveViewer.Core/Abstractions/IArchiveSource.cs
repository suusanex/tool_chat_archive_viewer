namespace ChatArchiveViewer.Core.Abstractions;

public interface IArchiveSource : IAsyncDisposable
{
    string DisplayPath { get; }

    Task<IReadOnlyList<string>> GetFilesAsync(string relativePath, string pattern, CancellationToken ct);

    Task<Stream> OpenFileAsync(string relativePath, CancellationToken ct);

    Task<bool> FileExistsAsync(string relativePath, CancellationToken ct);

    Task<bool> DirectoryExistsAsync(string relativePath, CancellationToken ct);

    Task<IReadOnlyList<string>> GetDirectoriesAsync(string relativePath, CancellationToken ct);
}
