namespace ChatArchiveViewer.CloudArchiveUpdater;

public static class CloudArchiveUpdaterConstants
{
    public const string BootstrapConfigUrlConfigurationKey = "CloudFetch:BootstrapConfigUrl";
    public const string MsalRedirectUri = "http://localhost";

    public static string? GetBootstrapConfigUrl(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return null;
        }

        if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"CloudFetch setting '{BootstrapConfigUrlConfigurationKey}' must be an absolute URI. Current value: '{configuredValue}'.");
        }

        return configuredValue;
    }
}
