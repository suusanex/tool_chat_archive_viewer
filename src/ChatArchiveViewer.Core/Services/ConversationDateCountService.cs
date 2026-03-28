using ChatArchiveViewer.Core.Abstractions;

namespace ChatArchiveViewer.Core.Services;

public sealed class ConversationDateCountService : IConversationDateCountService
{
    private readonly IConversationDayMessageCountSource countSource;
    private readonly object gate = new();
    private readonly Dictionary<string, Dictionary<DateOnly, int>> cache = new(StringComparer.Ordinal);
    private int cacheGeneration;

    public ConversationDateCountService(IConversationDayMessageCountSource countSource)
    {
        this.countSource = countSource ?? throw new ArgumentNullException(nameof(countSource));
    }

    public async Task<IReadOnlyDictionary<DateOnly, int>> LoadMonthCountsAsync(
        string conversationId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(dates);

        ct.ThrowIfCancellationRequested();

        var distinctDates = dates.Distinct().OrderBy(x => x).ToArray();
        var result = new Dictionary<DateOnly, int>();
        var missingDates = new List<DateOnly>();
        var generation = 0;

        lock (gate)
        {
            generation = cacheGeneration;
            if (cache.TryGetValue(conversationId, out var conversationCache))
            {
                foreach (var date in distinctDates)
                {
                    if (conversationCache.TryGetValue(date, out var count))
                    {
                        result[date] = count;
                    }
                    else
                    {
                        missingDates.Add(date);
                    }
                }
            }
            else
            {
                missingDates.AddRange(distinctDates);
            }
        }

        foreach (var date in missingDates)
        {
            ct.ThrowIfCancellationRequested();
            lock (gate)
            {
                if (generation != cacheGeneration)
                {
                    return result;
                }
            }

            var count = await countSource.GetMessageCountAsync(conversationId, date, ct).ConfigureAwait(false);
            lock (gate)
            {
                if (generation != cacheGeneration)
                {
                    return result;
                }

                if (!cache.TryGetValue(conversationId, out var conversationCache))
                {
                    conversationCache = new Dictionary<DateOnly, int>();
                    cache[conversationId] = conversationCache;
                }

                conversationCache[date] = count;
            }

            result[date] = count;
        }

        return result;
    }

    public void ClearCache()
    {
        lock (gate)
        {
            cache.Clear();
            cacheGeneration++;
        }
    }
}
