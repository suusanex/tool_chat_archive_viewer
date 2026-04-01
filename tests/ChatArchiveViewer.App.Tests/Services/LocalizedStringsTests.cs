using System.Globalization;
using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App.Tests.Services;

[TestFixture]
public sealed class LocalizedStringsTests
{
    [Test]
    public void Get_WithJapaneseCulture_ReturnsJapaneseResource()
    {
        var value = LocalizedStrings.Get("Nav.Browse", CultureInfo.GetCultureInfo("ja-JP"));

        Assert.That(value, Is.EqualTo("閲覧"));
    }

    [Test]
    public void Get_WithEnglishCulture_ReturnsEnglishResource()
    {
        var value = LocalizedStrings.Get("Nav.Browse", CultureInfo.GetCultureInfo("en-US"));

        Assert.That(value, Is.EqualTo("Browse"));
    }

    [Test]
    public void Get_WithUnsupportedCulture_FallsBackToJapanesePrimaryResource()
    {
        var value = LocalizedStrings.Get("Nav.Settings", CultureInfo.GetCultureInfo("fr-FR"));

        Assert.That(value, Is.EqualTo("設定"));
    }
}
