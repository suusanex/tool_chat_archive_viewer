namespace ChatArchiveViewer.Formats.Slack;

public static class SlackFormatConstants
{
    public const string FormatId = "slack-json-export";

    public const string DisplayName = "Slack JSON Export";

    public static readonly string[] AuxiliaryRootJsonFiles =
    [
        "canvases.json",
        "file_conversations.json",
        "huddle_transcripts.json",
        "integration_logs.json",
        "lists.json"
    ];
}
