using Azure.Core;

namespace ChatArchiveViewer.CloudFetch.Services;

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
