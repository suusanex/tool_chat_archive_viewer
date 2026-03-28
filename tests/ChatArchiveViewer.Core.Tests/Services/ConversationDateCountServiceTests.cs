using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Services;

namespace ChatArchiveViewer.Core.Tests.Services;

[TestFixture]
public sealed class ConversationDateCountServiceTests
{
    // 月表示の遅延集計は、同じ日付を再要求してもソースを再読込しない
    [Test]
    public async Task UT_IT_330a__LoadMonthCountsAsync_UsesCacheForRepeatedDates()
    {
        var source = new FakeCountSource
        {
            Counts =
            {
                [new DateOnly(2026, 1, 1)] = 11,
                [new DateOnly(2026, 1, 2)] = 22,
                [new DateOnly(2026, 1, 3)] = 33
            }
        };
        var sut = new ConversationDateCountService(source);

        var first = await sut.LoadMonthCountsAsync(
            "general",
            [new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)],
            CancellationToken.None);

        var second = await sut.LoadMonthCountsAsync(
            "general",
            [new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3)],
            CancellationToken.None);

        Assert.That(first[new DateOnly(2026, 1, 1)], Is.EqualTo(11));
        Assert.That(first[new DateOnly(2026, 1, 2)], Is.EqualTo(22));
        Assert.That(second[new DateOnly(2026, 1, 2)], Is.EqualTo(22));
        Assert.That(second[new DateOnly(2026, 1, 3)], Is.EqualTo(33));
        Assert.That(source.Calls, Has.Count.EqualTo(3));
        Assert.That(source.Calls[0], Is.EqualTo(("general", new DateOnly(2026, 1, 1))));
        Assert.That(source.Calls[1], Is.EqualTo(("general", new DateOnly(2026, 1, 2))));
        Assert.That(source.Calls[2], Is.EqualTo(("general", new DateOnly(2026, 1, 3))));
    }

    // 空の日付一覧ではソースを呼ばず、空の結果を返す
    [Test]
    public async Task UT_IT_330b__LoadMonthCountsAsync_EmptyDates_ReturnsEmpty()
    {
        var source = new FakeCountSource();
        var sut = new ConversationDateCountService(source);

        var results = await sut.LoadMonthCountsAsync("general", [], CancellationToken.None);

        Assert.That(results, Is.Empty);
        Assert.That(source.Calls, Is.Empty);
    }

    // アーカイブ切り替え時にキャッシュをクリアすると、同じ日付でも再集計される
    [Test]
    public async Task UT_IT_330c__ClearCache_RemovesCachedCounts()
    {
        var source = new FakeCountSource
        {
            Counts =
            {
                [new DateOnly(2026, 1, 1)] = 7
            }
        };
        var sut = new ConversationDateCountService(source);

        await sut.LoadMonthCountsAsync("general", [new DateOnly(2026, 1, 1)], CancellationToken.None);
        sut.ClearCache();
        await sut.LoadMonthCountsAsync("general", [new DateOnly(2026, 1, 1)], CancellationToken.None);

        Assert.That(source.Calls, Has.Count.EqualTo(2));
    }

    private sealed class FakeCountSource : IConversationDayMessageCountSource
    {
        public Dictionary<DateOnly, int> Counts { get; } = new();

        public List<(string ConversationId, DateOnly Date)> Calls { get; } = [];

        public Task<int> GetMessageCountAsync(string conversationId, DateOnly date, CancellationToken ct)
        {
            Calls.Add((conversationId, date));
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Counts.TryGetValue(date, out var count) ? count : 0);
        }
    }
}
