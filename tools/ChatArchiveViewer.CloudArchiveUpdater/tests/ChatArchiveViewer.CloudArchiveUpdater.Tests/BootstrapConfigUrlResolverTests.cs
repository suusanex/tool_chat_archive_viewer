using ChatArchiveViewer.CloudArchiveUpdater.Services;

namespace ChatArchiveViewer.CloudArchiveUpdater.Tests;

[TestFixture]
public sealed class BootstrapConfigUrlResolverTests
{
    [Test]
    public void Resolve_WhenAppSettingsUsesCamelCaseKey_ReturnsUrl()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"resolver-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(
                Path.Combine(tempDir, "appsettings.json"),
                """
                {
                  "CloudFetch": {
                    "bootstrapConfigUrl": "https://example.com/bootstrap.json"
                  }
                }
                """);

            var originalCurrentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(tempDir);
                var actual = BootstrapConfigUrlResolver.Resolve();
                Assert.That(actual, Is.EqualTo("https://example.com/bootstrap.json"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCurrentDirectory);
            }
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
