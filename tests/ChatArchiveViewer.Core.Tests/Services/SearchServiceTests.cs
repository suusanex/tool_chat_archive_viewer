using ChatArchiveViewer.Core.Models;
using ChatArchiveViewer.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatArchiveViewer.Core.Tests.Services;

[TestFixture]
public sealed class SearchServiceTests
{
    private readonly SearchService sut = new(NullLogger<SearchService>.Instance);

    // TP-070a: マッチするキーワードで該当メッセージが返される
    [Test]
    public async Task UT_IT_070a__SearchAsync_WithMatchingKeyword_ReturnsMatchedMessages()
    {
        var archive = CreateArchive();
        var messages = CreateMessages();

        var results = await sut.SearchAsync(archive, messages, "hello", CancellationToken.None);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(x => x.Message.Text.Contains("hello", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    // TP-070c: 空白キーワードは空結果として扱う
    [Test]
    public async Task UT_IT_070c_b__SearchAsync_WithEmptyKeyword_ReturnsEmpty()
    {
        var archive = CreateArchive();
        var messages = CreateMessages();

        var results = await sut.SearchAsync(archive, messages, " ", CancellationToken.None);

        Assert.That(results, Is.Empty);
    }

    // TP-070d: 大文字小文字を区別せず検索する
    [Test]
    public async Task UT_IT_070d_b__SearchAsync_IsCaseInsensitive()
    {
        var archive = CreateArchive();
        var messages = CreateMessages();

        var results = await sut.SearchAsync(archive, messages, "HELLO", CancellationToken.None);

        Assert.That(results, Has.Count.EqualTo(2));
    }

    // TP-070b: 複数チャンネルにまたがるマッチが結果に含まれる
    [Test]
    public async Task UT_IT_070b__SearchAsync_MultipleChannels_ReturnsMatchesFromAllChannels()
    {
        var archive = new ChatArchive
        {
            FormatId = "test",
            FormatDisplayName = "Test",
            Metadata = new ArchiveMetadata(),
            Conversations =
            [
                new Conversation { Id = "ch1", DisplayName = "Channel1", AvailableDates = [new DateOnly(2026, 1, 1)] },
                new Conversation { Id = "ch2", DisplayName = "Channel2", AvailableDates = [new DateOnly(2026, 1, 1)] }
            ],
            Participants = [],
            Diagnostics = []
        };
        var messages = new[]
        {
            new ChatMessage { Id = "1", ConversationId = "ch1", Timestamp = DateTimeOffset.UtcNow, Text = "hello from channel1" },
            new ChatMessage { Id = "2", ConversationId = "ch2", Timestamp = DateTimeOffset.UtcNow.AddMinutes(1), Text = "hello from channel2" },
            new ChatMessage { Id = "3", ConversationId = "ch1", Timestamp = DateTimeOffset.UtcNow.AddMinutes(2), Text = "unrelated" }
        };

        var results = await sut.SearchAsync(archive, messages, "hello", CancellationToken.None);

        Assert.That(results, Has.Count.EqualTo(2));
        var channelIds = results.Select(r => r.ConversationId).ToHashSet();
        Assert.That(channelIds, Contains.Item("ch1"));
        Assert.That(channelIds, Contains.Item("ch2"));
    }

    // TP-070c: マッチなしキーワードで空リストが返される
    [Test]
    public async Task UT_IT_070c__SearchAsync_NoMatch_ReturnsEmpty()
    {
        var archive = CreateArchive();
        var messages = CreateMessages();

        var results = await sut.SearchAsync(archive, messages, "xyznomatch", CancellationToken.None);

        Assert.That(results, Is.Empty);
    }

    // TP-070e: 検索結果の各エントリにメッセージ本文・チャンネル情報・日付が含まれる
    [Test]
    public async Task UT_IT_070e__SearchAsync_ResultsContainMessageAndChannelInfo()
    {
        var archive = CreateArchive();
        var messages = CreateMessages();

        var results = await sut.SearchAsync(archive, messages, "hello", CancellationToken.None);

        Assert.That(results, Is.Not.Empty);
        foreach (var result in results)
        {
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message.Text, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ConversationId, Is.Not.Null.And.Not.Empty);
            Assert.That(result.ConversationDisplayName, Is.Not.Null.And.Not.Empty);
            // 日付がメッセージの Timestamp から設定されている
            Assert.That(result.Date, Is.EqualTo(DateOnly.FromDateTime(result.Message.Timestamp.UtcDateTime)));
        }
    }

    // TP-070f: 結果がチャンネル順・日時順で返される
    [Test]
    public async Task UT_IT_070f__SearchAsync_ResultsAreOrdered()
    {
        var archive = new ChatArchive
        {
            FormatId = "test",
            FormatDisplayName = "Test",
            Metadata = new ArchiveMetadata(),
            Conversations =
            [
                new Conversation { Id = "beta", DisplayName = "Beta", AvailableDates = [new DateOnly(2026, 1, 1)] },
                new Conversation { Id = "alpha", DisplayName = "Alpha", AvailableDates = [new DateOnly(2026, 1, 1)] }
            ],
            Participants = [],
            Diagnostics = []
        };
        var baseTime = DateTimeOffset.UtcNow;
        var messages = new[]
        {
            new ChatMessage { Id = "3", ConversationId = "beta", Timestamp = baseTime, Text = "match beta early" },
            new ChatMessage { Id = "1", ConversationId = "alpha", Timestamp = baseTime.AddMinutes(5), Text = "match alpha late" },
            new ChatMessage { Id = "2", ConversationId = "alpha", Timestamp = baseTime.AddMinutes(1), Text = "match alpha early" }
        };

        var results = await sut.SearchAsync(archive, messages, "match", CancellationToken.None);

        // チャンネル（Alpha < Beta）順、同チャンネル内は日時昇順
        Assert.That(results[0].ConversationId, Is.EqualTo("alpha"));
        Assert.That(results[1].ConversationId, Is.EqualTo("alpha"));
        Assert.That(results[2].ConversationId, Is.EqualTo("beta"));
        Assert.That(results[0].Message.Timestamp, Is.LessThan(results[1].Message.Timestamp));
    }

    // TP-070d: 大文字小文字を区別しないマッチ
    [Test]
    public async Task UT_IT_070d__SearchAsync_CaseInsensitive_Matches()
    {
        var archive = CreateArchive();
        var messages = CreateMessages();

        var resultsLower = await sut.SearchAsync(archive, messages, "hello", CancellationToken.None);
        var resultsUpper = await sut.SearchAsync(archive, messages, "HELLO", CancellationToken.None);
        var resultsMixed = await sut.SearchAsync(archive, messages, "Hello", CancellationToken.None);

        Assert.That(resultsLower.Count, Is.EqualTo(resultsUpper.Count));
        Assert.That(resultsLower.Count, Is.EqualTo(resultsMixed.Count));
    }

    // TP-310e: 事前キャンセル済みトークンなら検索開始前に OperationCanceledException
    [Test]
    public void UT_IT_310e__SearchAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var archive = CreateArchive();
        var messages = CreateMessages();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await sut.SearchAsync(archive, messages, "hello", cts.Token);
        });
    }

    // TP-170c: 検索時ログにキーワードや本文を含めず、件数メタデータのみを出力する
    [Test]
    public async Task UT_IT_170c__SearchAsync_LogsDoNotContainKeywordOrMessageText()
    {
        var logger = new CapturingLogger<SearchService>();
        var searchService = new SearchService(logger);
        var archive = CreateArchive();
        var messages = CreateMessages();

        var results = await searchService.SearchAsync(archive, messages, "hello", CancellationToken.None);

        Assert.That(results, Has.Count.EqualTo(2));
        var combined = string.Join(Environment.NewLine, logger.Messages);
        Assert.That(combined, Does.Contain("Search completed."));
        Assert.That(combined, Does.Contain("ResultCount"));
        Assert.That(combined, Does.Not.Contain("hello world"));
        Assert.That(combined, Does.Not.Contain("HELLO again"));
        Assert.That(combined, Does.Not.Contain("hello"));
    }

    private static ChatArchive CreateArchive()
    {
        return new ChatArchive
        {
            FormatId = "test",
            FormatDisplayName = "Test",
            Metadata = new ArchiveMetadata(),
            Conversations =
            [
                new Conversation
                {
                    Id = "general",
                    DisplayName = "general",
                    AvailableDates = [new DateOnly(2026, 1, 1)]
                }
            ],
            Participants = [],
            Diagnostics = []
        };
    }

    private static IReadOnlyList<ChatMessage> CreateMessages()
    {
        return
        [
            new ChatMessage
            {
                Id = "1",
                ConversationId = "general",
                Timestamp = DateTimeOffset.UtcNow,
                Text = "hello world"
            },
            new ChatMessage
            {
                Id = "2",
                ConversationId = "general",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(1),
                Text = "HELLO again"
            },
            new ChatMessage
            {
                Id = "3",
                ConversationId = "general",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(2),
                Text = "other"
            }
        ];
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _ = logLevel;
            _ = eventId;
            var message = formatter(state, exception);
            Messages.Add(message);
        }
    }
}
