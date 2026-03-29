using System.Globalization;
using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App.Tests.Services;

[TestFixture]
public sealed class ApplicationCultureTests
{
    [Test]
    public void ResolveSupportedCulture_WithJapaneseCulture_ReturnsJapanesePrimaryCulture()
    {
        var culture = ApplicationCulture.ResolveSupportedCulture(CultureInfo.GetCultureInfo("ja"));

        Assert.That(culture.Name, Is.EqualTo("ja-JP"));
    }

    [Test]
    public void ResolveSupportedCulture_WithEnglishCulture_ReturnsEnglishSecondaryCulture()
    {
        var culture = ApplicationCulture.ResolveSupportedCulture(CultureInfo.GetCultureInfo("en-GB"));

        Assert.That(culture.Name, Is.EqualTo("en-US"));
    }

    [Test]
    public void ResolveSupportedCulture_WithUnsupportedCulture_FallsBackToJapanesePrimaryCulture()
    {
        var culture = ApplicationCulture.ResolveSupportedCulture(CultureInfo.GetCultureInfo("fr-FR"));

        Assert.That(culture.Name, Is.EqualTo("ja-JP"));
    }
}
