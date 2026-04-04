using System.Text.Json.Serialization;

namespace ChatArchiveViewer.CloudFetch.Models;

public sealed class CacheState
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("downloadedAt")]
    public required DateTimeOffset DownloadedAt { get; init; }

    [JsonPropertyName("bootstrapUrl")]
    public string? BootstrapUrl { get; init; }
}
