using System.Security.Cryptography;

namespace ChatArchiveViewer.CloudArchiveUpdater.Services;

public interface IFileHasher
{
    Task<string> ComputeSha256Async(string path, CancellationToken ct);
}

public sealed class Sha256FileHasher : IFileHasher
{
    public async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
