namespace ChatArchiveViewer.Core.Models;

public sealed class ChatArchive
{
    public required string FormatId { get; init; }

    public required string FormatDisplayName { get; init; }

    public required ArchiveMetadata Metadata { get; init; }

    public required IReadOnlyList<Conversation> Conversations { get; init; }

    public required IReadOnlyList<Participant> Participants { get; init; }

    public required IReadOnlyList<LoadDiagnostic> Diagnostics { get; init; }
}
