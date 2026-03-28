using System.Globalization;
using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.Formats.Slack;

internal static class SlackMappingHelpers
{
    public static DateTimeOffset ParseSlackTimestamp(string ts)
    {
        if (string.IsNullOrWhiteSpace(ts))
        {
            throw new FormatException("Slack timestamp is empty.");
        }

        var normalized = ts.Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var raw))
        {
            throw new FormatException($"Invalid Slack timestamp format: {ts}");
        }

        var unixSeconds = (double)raw;
        return DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Round(unixSeconds * 1000d));
    }

    public static ConversationType ToConversationType(SlackChannel channel)
    {
        if (channel.IsIm is true)
        {
            return ConversationType.DirectMessage;
        }

        if (channel.IsGroup is true || channel.IsMpim is true)
        {
            return ConversationType.Group;
        }

        if (channel.IsChannel is true)
        {
            return ConversationType.Channel;
        }

        return ConversationType.Other;
    }

    public static MessageType ToMessageType(string? subtype)
    {
        if (string.IsNullOrWhiteSpace(subtype))
        {
            return MessageType.Normal;
        }

        return subtype switch
        {
            "channel_join" or "channel_leave" or "channel_topic" or "channel_purpose" or "message_deleted" or
                "message_changed" => MessageType.System,
            _ => MessageType.Unknown
        };
    }

    public static string ResolveDisplayName(SlackUser? user, SlackUserProfile? profile, string? userId, string? botId)
    {
        if (!string.IsNullOrWhiteSpace(user?.Profile?.DisplayName))
        {
            return user.Profile.DisplayName!;
        }

        if (!string.IsNullOrWhiteSpace(user?.RealName))
        {
            return user.RealName!;
        }

        if (!string.IsNullOrWhiteSpace(profile?.DisplayName))
        {
            return profile.DisplayName!;
        }

        if (!string.IsNullOrWhiteSpace(profile?.RealName))
        {
            return profile.RealName!;
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        if (!string.IsNullOrWhiteSpace(botId))
        {
            return botId;
        }

        return "Unknown participant";
    }
}
