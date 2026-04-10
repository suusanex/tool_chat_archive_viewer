using System.Text.Json.Serialization;

namespace ChatArchiveViewer.CloudArchiveUpdater.Models;

public sealed class BootstrapConfig
{
    [JsonPropertyName("tenantId")]
    public required string TenantId { get; init; }

    [JsonPropertyName("clientId")]
    public required string ClientId { get; init; }

    [JsonPropertyName("authority")]
    public string? Authority { get; init; }

    [JsonPropertyName("manifestUrl")]
    public required string ManifestUrl { get; init; }

    [JsonPropertyName("scopes")]
    public IReadOnlyList<string> Scopes { get; init; } = [];
}
