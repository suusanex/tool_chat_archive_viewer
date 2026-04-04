namespace ChatArchiveViewer.App.Services;

public sealed class CloudFetchFeatureOptions
{
    public CloudFetchFeatureOptions(string? bootstrapConfigUrl)
    {
        BootstrapConfigUrl = bootstrapConfigUrl;
        IsEnabled = !string.IsNullOrWhiteSpace(bootstrapConfigUrl);
    }

    public bool IsEnabled { get; }

    public string? BootstrapConfigUrl { get; }
}