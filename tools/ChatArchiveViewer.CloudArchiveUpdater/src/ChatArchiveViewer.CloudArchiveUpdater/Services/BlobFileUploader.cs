using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ChatArchiveViewer.CloudArchiveUpdater.Services;

public interface IBlobFileUploader
{
    Task UploadAsync(Uri blobUri, TokenCredential credential, string sourcePath, string contentType, CancellationToken ct);
}

public sealed class BlobFileUploader : IBlobFileUploader
{
    public async Task UploadAsync(Uri blobUri, TokenCredential credential, string sourcePath, string contentType, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(blobUri);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var blobClient = new BlobClient(blobUri, credential);
        await using var stream = File.OpenRead(sourcePath);
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            },
            ct);
    }
}
