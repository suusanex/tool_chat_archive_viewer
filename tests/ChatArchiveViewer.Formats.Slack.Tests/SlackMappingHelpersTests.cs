using ChatArchiveViewer.Core.Models;
using ChatArchiveViewer.Formats.Slack;

namespace ChatArchiveViewer.Formats.Slack.Tests;

[TestFixture]
public sealed class SlackMappingHelpersTests
{
    [Test]
    public void ToMessageType_WithKnownSystemSubtype_ReturnsSystem()
    {
        var messageType = SlackMappingHelpers.ToMessageType("channel_join");
        Assert.That(messageType, Is.EqualTo(MessageType.System));
    }

    [Test]
    public void ToMessageType_WithUnknownSubtype_ReturnsUnknown()
    {
        var messageType = SlackMappingHelpers.ToMessageType("foo_bar");
        Assert.That(messageType, Is.EqualTo(MessageType.Unknown));
    }

    [Test]
    public void ResolveDisplayName_PrioritizesDisplayName()
    {
        var user = new SlackUser { Id = "U1", RealName = "Real", Profile = new SlackUserProfile { DisplayName = "Display" } };
        var display = SlackMappingHelpers.ResolveDisplayName(user, null, "U1", null);
        Assert.That(display, Is.EqualTo("Display"));
    }

    [Test]
    public void ParseSlackTimestamp_WithFractionalMilliseconds_TruncatesInsteadOfRoundingUp()
    {
        var timestamp = SlackMappingHelpers.ParseSlackTimestamp("1700000000.9996");

        Assert.That(
            timestamp,
            Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_999L)));
    }
}
