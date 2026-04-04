using System.Text.Json;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.CloudFetch.Services;

public sealed class BootstrapConfigProvider : IBootstrapConfigProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient;
    private readonly ILogger<BootstrapConfigProvider> logger;
    private readonly string bootstrapConfigUrl;

    public BootstrapConfigProvider(HttpClient httpClient, ILogger<BootstrapConfigProvider> logger, string bootstrapConfigUrl)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentException.ThrowIfNullOrWhiteSpace(bootstrapConfigUrl);
        this.bootstrapConfigUrl = bootstrapConfigUrl;
    }

    public async Task<BootstrapConfig> GetConfigAsync(CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(bootstrapConfigUrl, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        BootstrapConfig? config;
        try
        {
            config = await JsonSerializer.DeserializeAsync<BootstrapConfig>(stream, JsonOptions, ct);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("bootstrap.json format is invalid.", ex);
        }

        if (config is null)
        {
            throw new InvalidDataException("bootstrap.json is empty.");
        }

        if (string.IsNullOrWhiteSpace(config.TenantId))
        {
            throw new InvalidDataException("bootstrap.json tenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(config.ClientId))
        {
            throw new InvalidDataException("bootstrap.json clientId is required.");
        }

        if (string.IsNullOrWhiteSpace(config.ManifestUrl))
        {
            throw new InvalidDataException("bootstrap.json manifestUrl is required.");
        }

        if (!Uri.TryCreate(config.ManifestUrl, UriKind.Absolute, out _))
        {
            throw new InvalidDataException("bootstrap.json manifestUrl must be absolute URI.");
        }

        var scopes = config.Scopes.Count == 0
            ? ["https://storage.azure.com/.default"]
            : config.Scopes.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        var authority = string.IsNullOrWhiteSpace(config.Authority)
            ? $"https://login.microsoftonline.com/{config.TenantId}"
            : config.Authority;

        if (!Uri.TryCreate(authority, UriKind.Absolute, out _))
        {
            throw new InvalidDataException("bootstrap.json authority must be absolute URI.");
        }

        logger.LogInformation("Bootstrap config loaded. ManifestUrl={ManifestUrl}", config.ManifestUrl);
        return new BootstrapConfig
        {
            TenantId = config.TenantId,
            ClientId = config.ClientId,
            ManifestUrl = config.ManifestUrl,
            Authority = authority,
            Scopes = scopes
        };
    }
}
