using ChatArchiveViewer.Core.Models;
using ChatArchiveViewer.Core.Services;

namespace ChatArchiveViewer.Core.Tests.Services;

[TestFixture]
public sealed class DateFilterServiceTests
{
    private readonly DateFilterService sut = new();

    // TP-080a: 指定日付を含む会話のみ返される
    [Test]
    public void UT_IT_080a__FilterByDate_ReturnsOnlyConversationsContainingDate()
    {
        var targetDate = new DateOnly(2026, 1, 2);
        var conversations = new[]
        {
            new Conversation
            {
                Id = "a",
                DisplayName = "a",
                AvailableDates = [new DateOnly(2026, 1, 1), targetDate]
            },
            new Conversation
            {
                Id = "b",
                DisplayName = "b",
                AvailableDates = [new DateOnly(2026, 1, 1)]
            }
        };

        var result = sut.FilterByDate(conversations, targetDate);

        Assert.That(result.Select(x => x.Id).ToArray(), Is.EqualTo(new[] { "a" }));
    }

    // TP-080b: 指定日付に 1 件だけ該当する場合、1 件のみ返される
    [Test]
    public void UT_IT_080b__FilterByDate_SingleMatch_ReturnsOneConversation()
    {
        var targetDate = new DateOnly(2026, 3, 15);
        var conversations = new[]
        {
            new Conversation { Id = "ch1", DisplayName = "ch1", AvailableDates = [targetDate] },
            new Conversation { Id = "ch2", DisplayName = "ch2", AvailableDates = [new DateOnly(2026, 3, 10)] },
            new Conversation { Id = "ch3", DisplayName = "ch3", AvailableDates = [new DateOnly(2026, 3, 20)] }
        };

        var result = sut.FilterByDate(conversations, targetDate);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("ch1"));
    }

    // TP-080c: 指定日付に 0 件のとき空リストが返される
    [Test]
    public void UT_IT_080c__FilterByDate_NoMatch_ReturnsEmpty()
    {
        var conversations = new[]
        {
            new Conversation { Id = "ch1", DisplayName = "ch1", AvailableDates = [new DateOnly(2026, 1, 1)] }
        };

        var result = sut.FilterByDate(conversations, new DateOnly(2099, 12, 31));

        Assert.That(result, Is.Empty);
    }

    // TP-080d: 最古日を指定するとその日付のある会話が返される
    [Test]
    public void UT_IT_080d__FilterByDate_EarliestDate_ReturnsMatchingConversation()
    {
        var earliest = new DateOnly(2020, 1, 1);
        var conversations = new[]
        {
            new Conversation { Id = "old", DisplayName = "old", AvailableDates = [earliest, new DateOnly(2022, 6, 1)] },
            new Conversation { Id = "new", DisplayName = "new", AvailableDates = [new DateOnly(2023, 1, 1)] }
        };

        var result = sut.FilterByDate(conversations, earliest);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("old"));
    }

    // TP-080e: 最新日を指定するとその日付のある会話が返される
    [Test]
    public void UT_IT_080e__FilterByDate_LatestDate_ReturnsMatchingConversation()
    {
        var latest = new DateOnly(2026, 12, 31);
        var conversations = new[]
        {
            new Conversation { Id = "ch1", DisplayName = "ch1", AvailableDates = [new DateOnly(2026, 1, 1)] },
            new Conversation { Id = "ch2", DisplayName = "ch2", AvailableDates = [new DateOnly(2026, 6, 1), latest] }
        };

        var result = sut.FilterByDate(conversations, latest);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("ch2"));
    }

    // TP-080f: AvailableDates が空のチャンネルは日付指定フィルタで除外される
    [Test]
    public void UT_IT_080f__FilterByDate_EmptyChannelExcluded()
    {
        var targetDate = new DateOnly(2026, 1, 1);
        var conversations = new[]
        {
            new Conversation { Id = "empty", DisplayName = "empty", AvailableDates = [] },
            new Conversation { Id = "active", DisplayName = "active", AvailableDates = [targetDate] }
        };

        var result = sut.FilterByDate(conversations, targetDate);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("active"));
    }
}
