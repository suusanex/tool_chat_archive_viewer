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
        Assert.That(options.DebugPrimaryLanguageOverride, Is.Null);
    }

    [Test]
    public void Parse_WithDebugEnglishOption_SetsEnglishOverride()
    {
        var options = AppLaunchOptions.Parse(["app.exe", "--debug-language-en-us"]);

        Assert.That(options.DebugPrimaryLanguageOverride, Is.EqualTo("en-US"));
    }

    [Test]
    public void Parse_WithDebugLanguageOffOption_ClearsLanguageOverride()
    {
        var options = AppLaunchOptions.Parse(["app.exe", "--debug-language-off"]);

        Assert.That(options.DebugPrimaryLanguageOverride, Is.EqualTo("ja-JP"));
    }

    [Test]
    public void Parse_WithConflictingDebugLanguageOptions_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => AppLaunchOptions.Parse(["app.exe", "--debug-language-en-us", "--debug-language-off"]));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ParamName, Is.EqualTo("args"));
    }
}
