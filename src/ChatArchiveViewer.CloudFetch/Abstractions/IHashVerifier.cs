using ChatArchiveViewer.CloudFetch.Models;

namespace ChatArchiveViewer.CloudFetch.Abstractions;

public interface IHashVerifier
{
    Task<HashVerifyResult> VerifyAsync(string filePath, string expectedSha256, CancellationToken ct);
}
