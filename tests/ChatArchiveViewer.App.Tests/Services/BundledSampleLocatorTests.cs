using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App.Tests.Services;

[TestFixture]
public sealed class BundledSampleLocatorTests
{
    [Test]
    public void Ctor_BuildsExpectedPathsFromBaseDirectory()
    {
        var locator = new BundledSampleLocator(@"D:\app");

        Assert.That(locator.SampleFolderPath, Is.EqualTo(Path.GetFullPath(@"D:\app\Samples\Sample Slack export")));
        Assert.That(locator.SampleZipPath, Is.EqualTo(Path.GetFullPath(@"D:\app\Samples\Sample Slack export.zip")));
    }

    [Test]
    public void HasSampleFlags_ReflectExistingFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"sample-locator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "Samples", "Sample Slack export"));
        File.WriteAllText(Path.Combine(root, "Samples", "Sample Slack export.zip"), "zip");

        try
        {
            var locator = new BundledSampleLocator(root);

            Assert.That(locator.HasSampleFolder, Is.True);
            Assert.That(locator.HasSampleZip, Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
