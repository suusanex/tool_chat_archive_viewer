using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App.Tests.Services;

[TestFixture]
public sealed class AppLoggingTests
{
    [Test]
    public void EnsureNLogConfigured_LoadsConfiguration()
    {
        Assert.DoesNotThrow(() => AppLogging.EnsureNLogConfigured());
    }
}
