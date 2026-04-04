namespace ChatArchiveViewer.CloudFetch.Tests;

[TestFixture]
public sealed class CloudFetchConstantsTests
{
    [Test]
    public void GetBootstrapConfigUrl_ConfiguredValueIsNull_ReturnsNull()
    {
        var actual = CloudFetchConstants.GetBootstrapConfigUrl(null);

        Assert.That(actual, Is.Null);
    }

    [Test]
    public void GetBootstrapConfigUrl_ConfiguredValueIsWhitespace_ReturnsNull()
    {
        var actual = CloudFetchConstants.GetBootstrapConfigUrl("   ");

        Assert.That(actual, Is.Null);
    }

    [Test]
    public void GetBootstrapConfigUrl_ConfiguredValueIsAbsoluteUri_ReturnsConfiguredValue()
    {
        const string configuredValue = "https://contoso.example/bootstrap.json";

        var actual = CloudFetchConstants.GetBootstrapConfigUrl(configuredValue);

        Assert.That(actual, Is.EqualTo(configuredValue));
    }

    [Test]
    public void GetBootstrapConfigUrl_ConfiguredValueIsInvalid_ThrowsInvalidOperationException()
    {
        Assert.That(
            () => CloudFetchConstants.GetBootstrapConfigUrl("not-a-valid-uri"),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void MsalRedirectUri_IsLocalhost()
    {
        Assert.That(CloudFetchConstants.MsalRedirectUri, Is.EqualTo("http://localhost"));
    }
}
