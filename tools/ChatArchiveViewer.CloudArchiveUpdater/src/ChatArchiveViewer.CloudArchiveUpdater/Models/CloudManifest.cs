using System.Text.Json.Serialization;

namespace ChatArchiveViewer.CloudArchiveUpdater.Models;

public sealed class CloudManifest
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("downloadUrl")]
    public required string DownloadUrl { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset? PublishedAt { get; init; }

    [JsonIgnore]
    public required Uri DownloadUri { get; init; }
}
