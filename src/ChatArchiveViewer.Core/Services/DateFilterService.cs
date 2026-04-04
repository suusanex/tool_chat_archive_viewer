using ChatArchiveViewer.Core.Models;

namespace ChatArchiveViewer.Core.Services;

public sealed class DateFilterService
{
    public IReadOnlyList<Conversation> FilterByDate(IEnumerable<Conversation> conversations, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(conversations);

        return conversations
            .Where(conversation => conversation.AvailableDates.Contains(date))
            .ToArray();
    }
}
