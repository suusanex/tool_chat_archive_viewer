using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App.Tests.Services;

[TestFixture]
public sealed class CloudFetchFeatureOptionsTests
{
    [Test]
    public void CloudFetchFeatureOptions_WhenBootstrapConfigUrlIsNull_IsDisabled()
    {
        var options = new CloudFetchFeatureOptions(null);

        Assert.That(options.IsEnabled, Is.False);
        Assert.That(options.BootstrapConfigUrl, Is.Null);
    }

    [Test]
    public void CloudFetchFeatureOptions_WhenBootstrapConfigUrlIsWhitespace_IsDisabled()
    {
        var options = new CloudFetchFeatureOptions("   ");

        Assert.That(options.IsEnabled, Is.False);
    }

    [Test]
    public void CloudFetchFeatureOptions_WhenBootstrapConfigUrlIsAbsoluteUri_IsEnabled()
    {
        const string bootstrapConfigUrl = "https://contoso.example/bootstrap.json";
        var options = new CloudFetchFeatureOptions(bootstrapConfigUrl);

        Assert.That(options.IsEnabled, Is.True);
        Assert.That(options.BootstrapConfigUrl, Is.EqualTo(bootstrapConfigUrl));
    }
}
