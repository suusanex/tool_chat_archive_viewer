using System.Security.Cryptography;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Services;

public sealed class Sha256Verifier : IHashVerifier
{
    public async Task<HashVerifyResult> VerifyAsync(string filePath, string expectedSha256, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSha256);
        ct.ThrowIfCancellationRequested();

        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        var actual = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var expected = expectedSha256.Trim().ToLowerInvariant();
        return new HashVerifyResult(string.Equals(actual, expected, StringComparison.Ordinal), actual);
    }
}
