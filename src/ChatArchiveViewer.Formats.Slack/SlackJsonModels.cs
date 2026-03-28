using System.Text.Json.Serialization;

namespace ChatArchiveViewer.Formats.Slack;

internal sealed class SlackChannel
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("is_channel")]
    public bool? IsChannel { get; init; }

    [JsonPropertyName("is_group")]
    public bool? IsGroup { get; init; }

    [JsonPropertyName("is_im")]
    public bool? IsIm { get; init; }

    [JsonPropertyName("is_mpim")]
    public bool? IsMpim { get; init; }

    [JsonPropertyName("topic")]
    public SlackChannelTextValue? Topic { get; init; }

    [JsonPropertyName("purpose")]
    public SlackChannelTextValue? Purpose { get; init; }
}

internal sealed class SlackChannelTextValue
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

internal sealed class SlackUser
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("real_name")]
    public string? RealName { get; init; }

    [JsonPropertyName("profile")]
    public SlackUserProfile? Profile { get; init; }
}

internal sealed class SlackUserProfile
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("real_name")]
    public string? RealName { get; init; }
}

internal sealed class SlackMessage
{
    [JsonPropertyName("ts")]
    public string? Ts { get; init; }

    [JsonPropertyName("user")]
    public string? User { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("subtype")]
    public string? Subtype { get; init; }

    [JsonPropertyName("thread_ts")]
    public string? ThreadTs { get; init; }

    [JsonPropertyName("reply_count")]
    public int? ReplyCount { get; init; }

    [JsonPropertyName("edited")]
    public SlackEditedInfo? Edited { get; init; }

    [JsonPropertyName("reactions")]
    public List<SlackReaction>? Reactions { get; init; }

    [JsonPropertyName("files")]
    public List<SlackFile>? Files { get; init; }

    [JsonPropertyName("user_profile")]
    public SlackUserProfile? UserProfile { get; init; }

    [JsonPropertyName("bot_id")]
    public string? BotId { get; init; }
}

internal sealed class SlackEditedInfo
{
    [JsonPropertyName("ts")]
    public string? Ts { get; init; }
}

internal sealed class SlackReaction
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("count")]
    public int? Count { get; init; }
}

internal sealed class SlackFile
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("url_private")]
    public string? UrlPrivate { get; init; }

    [JsonPropertyName("url_private_download")]
    public string? UrlPrivateDownload { get; init; }
}
