using Azure.Core;
using ChatArchiveViewer.CloudFetch.Abstractions;
using ChatArchiveViewer.CloudFetch.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace ChatArchiveViewer.CloudFetch.Services;

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
            .WithRedirectUri(CloudFetchConstants.MsalRedirectUri)
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
