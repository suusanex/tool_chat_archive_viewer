namespace ChatArchiveViewer.CloudFetch;

public static class CloudFetchConstants
{
    public const string BootstrapConfigUrlConfigurationKey = "CloudFetch:BootstrapConfigUrl";
    public const string MsalRedirectUri = "http://localhost";
    public const string? DefaultBootstrapConfigUrl = null;

    /// <summary>
    /// appsettings の設定値から bootstrap 設定 URL を取得して返します。
    /// </summary>
    /// <param name="configuredValue">appsettings の <c>CloudFetch:BootstrapConfigUrl</c> 設定値。</param>
    /// <returns>検証済みの絶対 URL。未設定時は <see langword="null"/>。</returns>
    /// <exception cref="InvalidOperationException">設定値が絶対 URI として不正な場合。</exception>
    public static string? GetBootstrapConfigUrl(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return DefaultBootstrapConfigUrl;
        }

        var value = configuredValue;

        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"CloudFetch setting '{BootstrapConfigUrlConfigurationKey}' must be an absolute URI. Current value: '{value}'.");
        }

        return value;
    }
}
