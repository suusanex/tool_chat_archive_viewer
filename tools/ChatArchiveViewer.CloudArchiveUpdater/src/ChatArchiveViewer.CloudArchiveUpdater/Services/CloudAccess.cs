using System.Text.Json;
using Azure.Core;
using Azure.Storage.Blobs;
using ChatArchiveViewer.CloudArchiveUpdater.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace ChatArchiveViewer.CloudArchiveUpdater.Services;

public interface IBootstrapConfigProvider
{
    Task<BootstrapConfig> GetConfigAsync(CancellationToken ct);
}

public interface ICloudAuthService
{
    Task<TokenCredential> AuthenticateAsync(BootstrapConfig config, CancellationToken ct);
}

public interface ICloudManifestProvider
{
    Task<CloudManifest> GetManifestAsync(BootstrapConfig config, TokenCredential credential, CancellationToken ct);
}

public interface ICloudArchiveDownloader
{
    Task DownloadAsync(CloudManifest manifest, TokenCredential credential, string destinationPath, CancellationToken ct);
}

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

        if (string.IsNullOrWhiteSpace(config.ManifestUrl) || !Uri.TryCreate(config.ManifestUrl, UriKind.Absolute, out _))
        {
            throw new InvalidDataException("bootstrap.json manifestUrl must be absolute URI.");
        }

        var authority = string.IsNullOrWhiteSpace(config.Authority)
            ? $"https://login.microsoftonline.com/{config.TenantId}"
            : config.Authority;
        if (!Uri.TryCreate(authority, UriKind.Absolute, out _))
        {
            throw new InvalidDataException("bootstrap.json authority must be absolute URI.");
        }

        var scopes = config.Scopes.Count == 0
            ? ["https://storage.azure.com/.default"]
            : config.Scopes.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

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

public sealed class MsalAuthService : ICloudAuthService
{
    private readonly ILogger<MsalAuthService> logger;

    public MsalAuthService(ILogger<MsalAuthService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TokenCredential> AuthenticateAsync(BootstrapConfig config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(config);

        var authority = string.IsNullOrWhiteSpace(config.Authority)
            ? $"https://login.microsoftonline.com/{config.TenantId}"
            : config.Authority;
        var scopes = config.Scopes.Count == 0
            ? ["https://storage.azure.com/.default"]
            : config.Scopes.ToArray();

        var app = PublicClientApplicationBuilder
            .Create(config.ClientId)
            .WithAuthority(authority)
            .WithRedirectUri(CloudArchiveUpdaterConstants.MsalRedirectUri)
            .Build();

        AuthenticationResult? tokenResult = null;
        try
        {
            var account = (await app.GetAccountsAsync()).FirstOrDefault();
            if (account is not null)
            {
                tokenResult = await app.AcquireTokenSilent(scopes, account).ExecuteAsync(ct);
            }
        }
        catch (MsalUiRequiredException ex)
        {
            logger.LogInformation("Silent authentication is unavailable. Exception={Exception}", ex.ToString());
        }

        try
        {
            tokenResult ??= await app.AcquireTokenInteractive(scopes).ExecuteAsync(ct);
        }
        catch (MsalClientException ex) when (string.Equals(ex.ErrorCode, "authentication_canceled", StringComparison.OrdinalIgnoreCase))
        {
            throw new OperationCanceledException("Authentication was canceled by user.", ex, ct);
        }

        return new MsalTokenCredential(tokenResult.AccessToken, tokenResult.ExpiresOn);
    }
}

public sealed class MsalTokenCredential : TokenCredential
{
    private readonly AccessToken accessToken;

    public MsalTokenCredential(string token, DateTimeOffset expiresOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        accessToken = new AccessToken(token, expiresOn);
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        _ = requestContext;
        _ = cancellationToken;
        return accessToken;
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        _ = requestContext;
        _ = cancellationToken;
        return ValueTask.FromResult(accessToken);
    }
}

public sealed class CloudManifestProvider : ICloudManifestProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<CloudManifestProvider> logger;

    public CloudManifestProvider(ILogger<CloudManifestProvider> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CloudManifest> GetManifestAsync(BootstrapConfig config, TokenCredential credential, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(credential);

        var manifestUri = new Uri(config.ManifestUrl, UriKind.Absolute);
        var blobClient = new BlobClient(manifestUri, credential);
        var response = await blobClient.DownloadContentAsync(ct);
        var json = response.Value.Content.ToString();
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("manifest.json is empty.");
        }

        RawManifest? rawManifest;
        try
        {
            rawManifest = JsonSerializer.Deserialize<RawManifest>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("manifest.json format is invalid.", ex);
        }

        if (rawManifest is null)
        {
            throw new InvalidDataException("manifest.json cannot be deserialized.");
        }

        if (string.IsNullOrWhiteSpace(rawManifest.Version))
        {
            throw new InvalidDataException("manifest.json version is required.");
        }

        if (string.IsNullOrWhiteSpace(rawManifest.DownloadUrl))
        {
            throw new InvalidDataException("manifest.json downloadUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(rawManifest.Sha256))
        {
            throw new InvalidDataException("manifest.json sha256 is required.");
        }

        var normalizedSha = rawManifest.Sha256.Trim();
        if (normalizedSha.Length != 64)
        {
            throw new InvalidDataException("manifest.json sha256 must be 64 hex chars.");
        }

        _ = Convert.FromHexString(normalizedSha);

        var downloadUri = Uri.TryCreate(rawManifest.DownloadUrl, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri(manifestUri, rawManifest.DownloadUrl);

        logger.LogInformation("Manifest loaded. Version={Version} DownloadUri={DownloadUri}", rawManifest.Version, downloadUri);
        return new CloudManifest
        {
            Version = rawManifest.Version,
            DownloadUrl = rawManifest.DownloadUrl,
            Sha256 = normalizedSha.ToLowerInvariant(),
            PublishedAt = rawManifest.PublishedAt,
            DownloadUri = downloadUri
        };
    }

    private sealed class RawManifest
    {
        public string? Version { get; init; }

        public string? DownloadUrl { get; init; }

        public string? Sha256 { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }
    }
}

public sealed class CloudArchiveDownloader : ICloudArchiveDownloader
{
    public async Task DownloadAsync(CloudManifest manifest, TokenCredential credential, string destinationPath, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var blobClient = new BlobClient(manifest.DownloadUri, credential);
        await blobClient.DownloadToAsync(destinationPath, ct);
    }
}
