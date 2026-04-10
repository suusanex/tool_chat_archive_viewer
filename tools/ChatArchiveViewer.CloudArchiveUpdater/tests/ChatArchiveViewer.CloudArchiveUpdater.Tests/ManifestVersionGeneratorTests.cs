using ChatArchiveViewer.CloudArchiveUpdater.Services;

namespace ChatArchiveViewer.CloudArchiveUpdater.Tests;

[TestFixture]
public sealed class ManifestVersionGeneratorTests
{
    [Test]
    public void CreateNextVersion_WhenCurrentVersionHasSameUtcDate_IncrementsVersion()
    {
        var sut = new ManifestVersionGenerator();

        var actual = sut.CreateNextVersion("2026-04-10-v2", new DateTimeOffset(2026, 04, 10, 9, 0, 0, TimeSpan.Zero));

        Assert.That(actual, Is.EqualTo("2026-04-10-v3"));
    }

    [Test]
    public void CreateNextVersion_WhenCurrentVersionUsesDifferentDate_StartsFromV1()
    {
        var sut = new ManifestVersionGenerator();

        var actual = sut.CreateNextVersion("2026-04-09-v7", new DateTimeOffset(2026, 04, 10, 9, 0, 0, TimeSpan.Zero));

        Assert.That(actual, Is.EqualTo("2026-04-10-v1"));
    }
}
