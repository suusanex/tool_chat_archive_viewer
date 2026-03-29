using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App.Tests.Services;

[TestFixture]
public sealed class AppLaunchOptionsTests
{
    [Test]
    public void Parse_WithFolderOption_SelectsFolderSample()
    {
        var options = AppLaunchOptions.Parse(["app.exe", "--ui-test-load-sample-folder"]);

        Assert.That(options.AutoLoadSample, Is.EqualTo(BundledSampleKind.Folder));
    }

    [Test]
    public void Parse_WithZipOption_SelectsZipSample()
    {
        var options = AppLaunchOptions.Parse(["app.exe", "--ui-test-load-sample-zip"]);

        Assert.That(options.AutoLoadSample, Is.EqualTo(BundledSampleKind.Zip));
    }

    [Test]
    public void Parse_WithoutKnownOption_KeepsAutoLoadDisabled()
    {
        var options = AppLaunchOptions.Parse(["app.exe", "--unknown"]);

        Assert.That(options.AutoLoadSample, Is.Null);
    }
}
