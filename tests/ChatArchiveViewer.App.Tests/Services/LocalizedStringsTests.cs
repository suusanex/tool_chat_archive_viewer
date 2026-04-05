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

    [Test]
    public void Get_WithoutCulture_ResolvesFromRuntimeResources()
    {
        var value = LocalizedStrings.Get("Nav.Browse");

        Assert.That(value, Is.EqualTo("閲覧"));
    }

    [Test]
    public void ResolveResourcePath_WithJapaneseCulture_FindsReswFile()
    {
        var path = LocalizedStrings.ResolveResourcePath("ja-JP");

        Assert.That(path, Is.Not.Null);
        Assert.That(Path.GetFileName(path), Is.EqualTo("Resources.resw"));
    }

    [Test]
    public void GetResourceSearchRoots_ReturnsAtLeastOneSearchRoot()
    {
        var roots = LocalizedStrings.GetResourceSearchRoots();

        Assert.That(roots, Is.Not.Empty);
    }
}
