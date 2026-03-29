using ChatArchiveViewer.Core.Models;
using ChatArchiveViewer.Core.Services;
using ChatArchiveViewer.Formats.Slack;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatArchiveViewer.Formats.Slack.Tests;

[TestFixture]
public sealed class SlackFormatDetectorTests
{
    private readonly List<string> tempDirectories = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var tempDirectory in tempDirectories)
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        tempDirectories.Clear();
    }

    // TP-010a: channels.json + users.json + 日付 JSON で検出できる
    [Test]
    public async Task UT_IT_010a__DetectAsync_WithChannelsAndDailyJson_ReturnsDetected()
    {
        var root = CreateSlackLikeFolder();
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.True);
        Assert.That(result.Confidence, Is.GreaterThan(0.7));
    }

    // TP-110a: 日付 JSON がない不完全構造は未検出となる
    [Test]
    public async Task UT_IT_110a_b__DetectAsync_WithoutDailyJson_ReturnsNotDetected()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "channels.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.False);
    }

    // TP-010b: channels.json のみ存在 → IsDetected=true（部分検出）
    [Test]
    public async Task UT_IT_010b__DetectAsync_ChannelsJsonOnly_ReturnsDetected()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        File.WriteAllText(Path.Combine(root, "channels.json"), """[{"id":"C1","name":"general","is_channel":true}]""");
        // users.json は存在しない
        File.WriteAllText(Path.Combine(root, "general", "2026-01-01.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.True);
    }

    // TP-010c: users.json のみ存在 → IsDetected=true（部分検出）
    [Test]
    public async Task UT_IT_010c__DetectAsync_UsersJsonOnly_ReturnsDetected()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        // channels.json は存在しない
        File.WriteAllText(Path.Combine(root, "users.json"), """[{"id":"U1","real_name":"Alice"}]""");
        File.WriteAllText(Path.Combine(root, "general", "2026-01-01.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.True);
    }

    // TP-010d: DM・グループチャンネル混在でも IsDetected=true
    [Test]
    public async Task UT_IT_010d__DetectAsync_WithDmAndGroupChannels_ReturnsDetected()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "D123")); // DM channel
        Directory.CreateDirectory(Path.Combine(root, "G456")); // Group channel
        File.WriteAllText(
            Path.Combine(root, "channels.json"),
            """[{"id":"C1","name":"D123","is_im":true},{"id":"G2","name":"G456","is_group":true}]""");
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        File.WriteAllText(Path.Combine(root, "D123", "2026-01-01.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.True);
    }

    // TP-010e: FormatId / FormatDisplayName が非空
    [Test]
    public async Task UT_IT_010e__DetectAsync_FormatIdAndDisplayNameAreNonEmpty()
    {
        var root = CreateSlackLikeFolder();
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.FormatId, Is.Not.Null.And.Not.Empty);
        Assert.That(result.FormatDisplayName, Is.Not.Null.And.Not.Empty);
    }

    // TP-010f: 補助アーティファクト（canvases.json 等）の有無に影響されず検出できる
    [Test]
    public async Task UT_IT_010f__DetectAsync_WithAuxiliaryArtifacts_ReturnsDetected()
    {
        var root = CreateSlackLikeFolder();
        // 補助アーティファクトを追加
        File.WriteAllText(Path.Combine(root, "canvases.json"), "[]");
        File.WriteAllText(Path.Combine(root, "file_conversations.json"), "[]");
        File.WriteAllText(Path.Combine(root, "integration_logs.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.True);
    }

    // TP-010g: Unicode を含むディレクトリ名でも検出できる
    [Test]
    public async Task UT_IT_010g__DetectAsync_UnicodeDirName_ReturnsDetected()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        var unicodeDir = Path.Combine(root, "日本語チャンネル");
        Directory.CreateDirectory(unicodeDir);
        File.WriteAllText(Path.Combine(root, "channels.json"), "[]");
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        File.WriteAllText(Path.Combine(unicodeDir, "2026-01-01.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.True);
    }

    // TP-110a: Slack 関連ファイルが一切存在しない → IsDetected=false
    [Test]
    public async Task UT_IT_110a__DetectAsync_NoSlackFiles_ReturnsNotDetected()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        // 完全に空のフォルダ（Slack ファイルなし）
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.False);
    }

    // TP-110b: channels.json は有効 JSON だが Slack 構造ではない → IsDetected=false
    [Test]
    public async Task UT_IT_110b__DetectAsync_NonSlackChannelsJson_ReturnsNotDetected()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        // Slack の channels.json 形式ではない JSON
        File.WriteAllText(Path.Combine(root, "channels.json"), """{"not":"a_slack_structure"}""");
        // channels.json はあるが日付 JSON ファイルなし（ディレクトリなし）
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        var result = await detector.DetectAsync(source, CancellationToken.None);

        Assert.That(result.IsDetected, Is.False);
    }

    // TP-110c: channels.json が JSON 構文エラー → IsDetected=false（例外ではなく検出失敗）
    [Test]
    public async Task UT_IT_110c__DetectAsync_BrokenChannelsJson_ReturnsNotDetectedWithoutThrowing()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "channels.json"), "{broken json!");
        // 日付 JSON なし
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);

        FormatDetectionResult? result = null;
        Assert.DoesNotThrowAsync(async () =>
        {
            result = await detector.DetectAsync(source, CancellationToken.None);
        });
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsDetected, Is.False);
    }

    // TP-010h: 非グレゴリオ暦カルチャでも日付ファイル名を安定して検出できる
    [Test]
    public async Task UT_IT_010h__DetectAsync_WithNonGregorianCulture_ReturnsDetected()
    {
        var root = CreateSlackLikeFolder();
        await using var source = new FolderArchiveSource(root);
        var detector = new SlackFormatDetector(NullLogger<SlackFormatDetector>.Instance);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = new CultureInfo("ar-SA");

            var result = await detector.DetectAsync(source, CancellationToken.None);

            Assert.That(result.IsDetected, Is.True);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private string CreateSlackLikeFolder()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-detector-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        File.WriteAllText(Path.Combine(root, "channels.json"), """[{ "id":"C1","name":"general","is_channel":true }]""");
        File.WriteAllText(Path.Combine(root, "users.json"), """[{ "id":"U1","profile":{"display_name":"Alice"} }]""");
        File.WriteAllText(Path.Combine(root, "general", "2026-01-01.json"), "[]");
        return root;
    }

    private string TrackTempDirectory(string path)
    {
        tempDirectories.Add(path);
        return path;
    }
}
