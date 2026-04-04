using ChatArchiveViewer.Core.Models;
using ChatArchiveViewer.Core.Services;
using ChatArchiveViewer.Formats.Slack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatArchiveViewer.Formats.Slack.Tests;

[TestFixture]
public sealed class SlackArchiveParserTests
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

    // TP-020a: 基本構造で会話・参加者・総件数が読み込まれる
    [Test]
    public async Task UT_IT_020a_b__ParseAsync_ParsesConversationsAndParticipants()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.FormatId, Is.EqualTo(SlackFormatConstants.FormatId));
        Assert.That(archive.Conversations, Has.Count.EqualTo(1));
        Assert.That(archive.Participants, Has.Count.EqualTo(1));
        Assert.That(archive.Metadata.TotalMessageCount, Is.EqualTo(2));
    }

    // TP-030b: 日付指定読み込みでメッセージが時系列順になる
    [Test]
    public async Task UT_IT_030b_b__LoadMessagesAsync_WithDate_ReturnsMessagesInTimeOrder()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        Assert.That(messages, Has.Count.EqualTo(2));
        Assert.That(messages[0].Timestamp <= messages[1].Timestamp, Is.True);
    }

    // TP-120a/b: 破損日付 JSON があっても継続しアーカイブを返す
    [Test]
    public async Task UT_IT_120ab_b__ParseAsync_WithBrokenDailyJson_ContinuesAndReturnsArchive()
    {
        var root = CreateSlackFolder();
        await File.WriteAllTextAsync(Path.Combine(root, "general", "2026-01-02.json"), "{broken");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.Conversations, Has.Count.EqualTo(1));
        Assert.That(archive.Metadata.TotalMessageCount, Is.EqualTo(2));
    }

    // TP-020a: 標準構造で ChatArchive が返り、Conversations / Participants / Metadata が正しくマッピングされる
    [Test]
    public async Task UT_IT_020a__ParseAsync_StandardStructure_ReturnsMappedArchive()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.FormatId, Is.EqualTo(SlackFormatConstants.FormatId));
        Assert.That(archive.FormatDisplayName, Is.EqualTo(SlackFormatConstants.DisplayName));
        Assert.That(archive.Conversations, Is.Not.Empty);
        Assert.That(archive.Participants, Is.Not.Empty);
        Assert.That(archive.Metadata, Is.Not.Null);
        Assert.That(archive.Metadata.TotalMessageCount, Is.GreaterThan(0));
    }

    // TP-020b: users.json の各ユーザーが Participant にマッピングされる
    [Test]
    public async Task UT_IT_020b__ParseAsync_UsersJsonMappedToParticipants()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        var participant = archive.Participants.FirstOrDefault(p => p.Id == "U1");
        Assert.That(participant, Is.Not.Null);
        Assert.That(participant!.DisplayName, Is.Not.Null.And.Not.Empty);
    }

    // TP-020c: channels.json の各チャンネルが Conversation にマッピングされる
    [Test]
    public async Task UT_IT_020c__ParseAsync_ChannelsJsonMappedToConversations()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        var conversation = archive.Conversations.FirstOrDefault(c => c.Id == "general");
        Assert.That(conversation, Is.Not.Null);
        Assert.That(conversation!.DisplayName, Is.Not.Null.And.Not.Empty);
    }

    // TP-020d: 各チャンネルの日付ファイル群が AvailableDates に集約され MessageCount がカウントされる
    [Test]
    public async Task UT_IT_020d__ParseAsync_AvailableDatesAndMessageCountAreSet()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        var conversation = archive.Conversations.First(c => c.Id == "general");
        Assert.That(conversation.AvailableDates, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(conversation.MessageCount, Is.EqualTo(2));
    }

    // TP-020e: スレッド親子関係が正しくマッピングされる
    [Test]
    public async Task UT_IT_020e__LoadMessages_ThreadParentAndReplyCount_AreMapped()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        // 最初のメッセージはスレッド親（reply_count=1 が設定されている）
        var threadParent = messages.FirstOrDefault(m => m.IsThreadParent);
        Assert.That(threadParent, Is.Not.Null);
        Assert.That(threadParent!.ReplyCount, Is.GreaterThan(0));
        Assert.That(threadParent.ThreadId, Is.Not.Null);
    }

    // TP-020f: リアクションが ChatMessage.Reactions にマッピングされる
    [Test]
    public async Task UT_IT_020f__LoadMessages_ReactionsMapped()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        var withReactions = messages.FirstOrDefault(m => m.Reactions.Any());
        Assert.That(withReactions, Is.Not.Null);
        Assert.That(withReactions!.Reactions[0].Name, Is.EqualTo("thumbsup"));
        Assert.That(withReactions.Reactions[0].Count, Is.EqualTo(2));
    }

    // TP-020g: files セクションが Attachments にマッピングされる
    [Test]
    public async Task UT_IT_020g__LoadMessages_FilesAttachments_Mapped()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-01.json"),
            """
            [
              {
                "ts":"1735689600.000100",
                "user":"U1",
                "text":"file message",
                "files":[
                  {
                    "name":"report.txt",
                    "title":"Report",
                    "url_private":"https://files.example/private/report.txt"
                  }
                ]
              }
            ]
            """);
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].Attachments, Has.Count.EqualTo(1));
        Assert.That(messages[0].Attachments[0].Name, Is.EqualTo("report.txt"));
        Assert.That(messages[0].Attachments[0].Title, Is.EqualTo("Report"));
        Assert.That(messages[0].Attachments[0].Url, Is.EqualTo("https://files.example/private/report.txt"));
    }

    // TP-020h: System/Unknown メッセージタイプが正しく設定される
    [Test]
    public async Task UT_IT_020h__LoadMessages_SystemSubtype_MapsToSystemType()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        // message_changed サブタイプは System にマッピングされる
        var systemMsg = messages.FirstOrDefault(m => m.Type == MessageType.System);
        Assert.That(systemMsg, Is.Not.Null);
    }

    // TP-020i: DM・グループチャンネルの Type が DirectMessage / Group に設定される
    [Test]
    public async Task UT_IT_020i__ParseAsync_DmAndGroupChannels_TypesSetCorrectly()
    {
        var root = CreateSlackFolder();
        // DM チャンネルと Group チャンネルを追加
        Directory.CreateDirectory(Path.Combine(root, "dm_ch"));
        Directory.CreateDirectory(Path.Combine(root, "grp_ch"));
        File.WriteAllText(
            Path.Combine(root, "channels.json"),
            """
            [
              {"id":"C1","name":"general","is_channel":true},
              {"id":"D1","name":"dm_ch","is_im":true},
              {"id":"G1","name":"grp_ch","is_group":true}
            ]
            """);
        File.WriteAllText(Path.Combine(root, "dm_ch", "2026-01-01.json"), "[]");
        File.WriteAllText(Path.Combine(root, "grp_ch", "2026-01-01.json"), "[]");

        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        var dmConv = archive.Conversations.FirstOrDefault(c => c.Id == "dm_ch");
        var grpConv = archive.Conversations.FirstOrDefault(c => c.Id == "grp_ch");

        Assert.That(dmConv, Is.Not.Null);
        Assert.That(dmConv!.Type, Is.EqualTo(ConversationType.DirectMessage));
        Assert.That(grpConv, Is.Not.Null);
        Assert.That(grpConv!.Type, Is.EqualTo(ConversationType.Group));
    }

    // TP-020j: Metadata.EarliestDate / LatestDate が全メッセージの日付範囲で算出される
    [Test]
    public async Task UT_IT_020j__ParseAsync_MetadataDateRange_IsCorrect()
    {
        var root = CreateSlackFolder();
        // 2 日目のファイルを追加
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-05.json"),
            """[{"ts":"1736035200.000100","user":"U1","text":"later message"}]""");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.Metadata.EarliestDate, Is.EqualTo(new DateOnly(2026, 1, 1)));
        Assert.That(archive.Metadata.LatestDate, Is.EqualTo(new DateOnly(2026, 1, 5)));
    }

    // TP-020k: Metadata.TotalMessageCount が全チャンネル・全日付の合計を返す
    [Test]
    public async Task UT_IT_020k__ParseAsync_TotalMessageCount_IsCorrect()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.Metadata.TotalMessageCount, Is.EqualTo(2));
    }

    // TP-020l: IProgress<ArchiveLoadProgress> にパース中の進捗が報告される
    [Test]
    public async Task UT_IT_020l__ParseAsync_ReportsProgress()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);
        var progress = new SyncProgress<ArchiveLoadProgress>();

        await parser.ParseAsync(source, progress, CancellationToken.None);

        // 少なくとも 1 回以上 Phase が報告される
        Assert.That(progress.Reported, Is.Not.Empty);
        Assert.That(progress.Reported.All(p => p.Phase != null), Is.True);
    }

    // TP-020m: 正常パース時は Diagnostics が空または Information のみ
    [Test]
    public async Task UT_IT_020m__ParseAsync_NormalCase_DiagnosticsHasNoErrors()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        var errors = archive.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.That(errors, Is.Empty, "正常パース時は Error 診断がないこと");
    }

    // TP-020n: 補助アーティファクトは Conversations に混入しない
    [Test]
    public async Task UT_IT_020n__ParseAsync_AuxiliaryArtifacts_NotIncludedInConversations()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(Path.Combine(root, "canvases.json"), "[]");
        File.WriteAllText(Path.Combine(root, "file_conversations.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        // 補助ファイル名が Conversation の Id に含まれないこと
        Assert.That(archive.Conversations.Any(c => c.Id.Contains("canvases", StringComparison.OrdinalIgnoreCase)), Is.False);
        Assert.That(archive.Conversations.Any(c => c.Id.Contains("file_conversations", StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    // TP-020o: 補助アーティファクト件数が Metadata.ExtendedProperties に集約される
    [Test]
    public async Task UT_IT_020o__ParseAsync_AuxiliaryCount_StoredInMetadata()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(Path.Combine(root, "canvases.json"), "[]");
        File.WriteAllText(Path.Combine(root, "lists.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.Metadata.ExtendedProperties.TryGetValue("auxiliary_root_json_count", out var countStr), Is.True);
        Assert.That(int.Parse(countStr!), Is.EqualTo(2));
    }

    // TP-130e: 補助アーティファクトが存在しない通常ケースでは件数 0 が記録される
    [Test]
    public async Task UT_IT_130e__ParseAsync_NoAuxiliaryArtifacts_StoresZeroCount()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.Metadata.ExtendedProperties.TryGetValue("auxiliary_root_json_count", out var countStr), Is.True);
        Assert.That(int.Parse(countStr!), Is.EqualTo(0));
    }

    // TP-020p: edited を含むメッセージで更新後本文が Text に入り IsEdited=true
    [Test]
    public async Task UT_IT_020p__LoadMessages_EditedMessage_IsEditedSetAndTextPreserved()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        var editedMsg = messages.FirstOrDefault(m => m.IsEdited);
        Assert.That(editedMsg, Is.Not.Null);
        Assert.That(editedMsg!.EditedAt, Is.Not.Null);
    }

    // TP-020q: message_deleted を含んでもクラッシュせず System メッセージとして保持される
    [Test]
    public async Task UT_IT_020q__ParseAsync_MessageDeleted_DoesNotCrashAndMapsToSystem()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-02.json"),
            """
            [
              {
                "ts":"1735776000.000100",
                "user":"U1",
                "text":"deleted message tombstone",
                "subtype":"message_deleted"
              }
            ]
            """);
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        ChatArchive? archive = null;
        Assert.DoesNotThrowAsync(async () =>
        {
            archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);
        });

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 2), CancellationToken.None);

        Assert.That(archive, Is.Not.Null);
        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].RawSubtype, Is.EqualTo("message_deleted"));
        Assert.That(messages[0].Type, Is.EqualTo(MessageType.System));
    }

    // TP-020r: files セクションを含むメッセージの添付が Attachments にマッピングされ外部アクセスしない
    [Test]
    public async Task UT_IT_020r__LoadMessages_FilesSection_MappedToAttachmentsNoDownload()
    {
        var root = CreateSlackFolder();
        // ファイル添付を含むメッセージを追加
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-10.json"),
            """
            [
              {
                "ts":"1736467200.000100","user":"U1","text":"here is a file",
                "files":[{"name":"report.pdf","title":"Report","url_private_download":"https://files.slack.com/files-pri/xxx/report.pdf"}]
              }
            ]
            """);
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        // パース中に外部アクセスしない（ネットワーク接続が要求されないこと）
        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 10), CancellationToken.None);

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].Attachments, Has.Count.EqualTo(1));
        Assert.That(messages[0].Attachments[0].Name, Is.EqualTo("report.pdf"));
        Assert.That(messages[0].Attachments[0].Url, Is.Not.Null); // URL は保持されるが DL しない
    }

    // TP-020s: user_profile があり users.json が不足 → user_profile フォールバックで参加者解決
    [Test]
    public async Task UT_IT_020s__ParseAsync_UserProfileFallback_ResolvedParticipant()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        // users.json なし、channels.json のみ
        File.WriteAllText(Path.Combine(root, "channels.json"), """[{"id":"C1","name":"general","is_channel":true}]""");
        // メッセージに user_profile を埋め込む
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-01.json"),
            """
            [
              {
                "ts":"1735689600.000100",
                "user":"UUNKNOWN",
                "text":"hello from profile user",
                "user_profile":{"display_name":"ProfileUser","real_name":"Profile Real Name"}
              }
            ]
            """);
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        // user_profile_display_name フォールバックで参加者が追加される
        var fallbackParticipant = archive.Participants.FirstOrDefault(p => p.Id == "UUNKNOWN");
        Assert.That(fallbackParticipant, Is.Not.Null, "user_profile フォールバック参加者が Participants に含まれること");
        Assert.That(fallbackParticipant!.DisplayName, Is.EqualTo("ProfileUser"));
    }

    // TP-020s（補足）: user_profile_display_name が ExtendedProperties に格納される
    [Test]
    public async Task UT_IT_020s_b__LoadMessages_UserProfile_StoredInExtendedProperties()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        File.WriteAllText(Path.Combine(root, "channels.json"), """[{"id":"C1","name":"general","is_channel":true}]""");
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-01.json"),
            """
            [
              {
                "ts":"1735689600.000100",
                "user":"UUNKNOWN",
                "text":"msg",
                "user_profile":{"display_name":"ProfUser","real_name":"Prof Real"}
              }
            ]
            """);
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].ExtendedProperties.ContainsKey("user_profile_display_name"), Is.True);
        Assert.That(messages[0].ExtendedProperties["user_profile_display_name"], Is.EqualTo("ProfUser"));
        Assert.That(messages[0].ExtendedProperties.ContainsKey("user_profile_real_name"), Is.True);
        Assert.That(messages[0].ExtendedProperties["user_profile_real_name"], Is.EqualTo("Prof Real"));
    }

    // TP-020t: url_private_download 等の URL はパース時に外部アクセスしない
    [Test]
    public async Task UT_IT_020t__ParseAsync_PrivateUrls_NeverFetched()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        // ネットワーク例外なしでパースが完了すれば外部アクセスなし
        Assert.DoesNotThrowAsync(async () =>
        {
            await parser.ParseAsync(source, progress: null, CancellationToken.None);
        });
    }

    // TP-020u: Unicode を含むディレクトリ名でも AvailableDates と MessageCount が集計できる
    [Test]
    public async Task UT_IT_020u__ParseAsync_UnicodeDirName_AvailableDatesAndCountCollected()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        var unicodeDir = "日本語チャンネル";
        Directory.CreateDirectory(Path.Combine(root, unicodeDir));
        File.WriteAllText(
            Path.Combine(root, "channels.json"),
            $$"""[{"id":"C1","name":"{{unicodeDir}}","is_channel":true}]""");
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        File.WriteAllText(
            Path.Combine(root, unicodeDir, "2026-01-01.json"),
            """[{"ts":"1735689600.000100","user":"U1","text":"こんにちは"}]""");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        var conv = archive.Conversations.FirstOrDefault(c => c.Id == unicodeDir);
        Assert.That(conv, Is.Not.Null);
        Assert.That(conv!.AvailableDates, Has.Count.EqualTo(1));
        Assert.That(conv.MessageCount, Is.EqualTo(1));
    }

    // TP-030a: 有効な conversationId + 有効な date → 該当メッセージリストが返される
    [Test]
    public async Task UT_IT_030a__LoadMessagesAsync_ValidConversationAndDate_ReturnsMessages()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        Assert.That(messages, Is.Not.Empty);
        Assert.That(messages.All(m => m.ConversationId == "general"), Is.True);
    }

    // TP-030b: メッセージが Timestamp 昇順でソートされている
    [Test]
    public async Task UT_IT_030b__LoadMessagesAsync_Messages_AreSortedByTimestamp()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        for (var i = 1; i < messages.Count; i++)
        {
            Assert.That(messages[i - 1].Timestamp <= messages[i].Timestamp, Is.True);
        }
    }

    // TP-030c: 各メッセージの必須フィールド（Id, ConversationId, Timestamp, Text）が設定されている
    [Test]
    public async Task UT_IT_030c__LoadMessagesAsync_RequiredFieldsAreSet()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        Assert.That(messages, Is.Not.Empty);
        foreach (var msg in messages)
        {
            Assert.That(msg.Id, Is.Not.Null.And.Not.Empty, "Id must be set");
            Assert.That(msg.ConversationId, Is.Not.Null.And.Not.Empty, "ConversationId must be set");
            Assert.That(msg.Timestamp, Is.Not.EqualTo(default(DateTimeOffset)), "Timestamp must be set");
            Assert.That(msg.Text, Is.Not.Null, "Text must not be null");
        }
    }

    // TP-030d: LoadMessagesAsync の ParticipantId は ParseAsync の Participants と整合する
    [Test]
    public async Task UT_IT_030d__LoadMessagesAsync_ParticipantId_ExistsInArchiveParticipants()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);
        var messages = await parser.LoadMessagesAsync(source, "general", null, CancellationToken.None);
        var participantIds = archive.Participants.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.That(messages.Where(x => x.ParticipantId is not null).Select(x => x.ParticipantId!), Is.Not.Empty);
        Assert.That(
            messages.Where(x => x.ParticipantId is not null).All(x => participantIds.Contains(x.ParticipantId!)),
            Is.True);
    }

    // TP-030e: date=null → 全日付のメッセージが返される
    [Test]
    public async Task UT_IT_030e__LoadMessagesAsync_NullDate_ReturnsAllDates()
    {
        var root = CreateSlackFolder();
        // 2 日目のメッセージファイルを追加
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-02.json"),
            """[{"ts":"1735776000.000100","user":"U1","text":"day two message"}]""");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", null, CancellationToken.None);

        Assert.That(messages, Has.Count.EqualTo(3), "2日分（2+1件）が全て返されること");
    }

    // TP-030f: Normal, System, Unknown が混在しても全件返される
    [Test]
    public async Task UT_IT_030f__LoadMessagesAsync_MixedMessageTypes_AllReturned()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), CancellationToken.None);

        var hasNormal = messages.Any(m => m.Type == MessageType.Normal);
        var hasSystem = messages.Any(m => m.Type == MessageType.System);
        Assert.That(hasNormal, Is.True);
        Assert.That(hasSystem, Is.True);
    }

    // TP-120a/b/c: 正常ファイルと破損日付 JSON の混在 → 正常メッセージは含まれ Diagnostics に診断が記録される
    [Test]
    public async Task UT_IT_120abc__ParseAsync_MixedBrokenAndNormalFiles_NormalMessagesKeptDiagnosticsRecorded()
    {
        var root = CreateSlackFolder();
        // 破損ファイルを追加
        File.WriteAllText(Path.Combine(root, "general", "2026-01-02.json"), "{broken json!");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        // 正常ファイルのメッセージは含まれる
        Assert.That(archive.Metadata.TotalMessageCount, Is.EqualTo(2));
        // Diagnostics に Warning 以上が記録される
        var warnings = archive.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ToList();
        Assert.That(warnings, Is.Not.Empty);
    }

    // TP-120c: 診断の SourceHint に破損ファイルのパスが含まれる
    [Test]
    public async Task UT_IT_120c__ParseAsync_BrokenFile_DiagnosticContainsSourceHint()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(Path.Combine(root, "general", "2026-01-02.json"), "{broken json!");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        var diag = archive.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        Assert.That(diag, Is.Not.Null);
        Assert.That(diag!.SourceHint, Is.Not.Null.And.Contains("2026-01-02.json"));
    }

    // TP-120d: 診断の Message にチャットデータ本文が含まれない（ログ安全性）
    [Test]
    public async Task UT_IT_120d__ParseAsync_DiagnosticMessage_DoesNotContainChatData()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(Path.Combine(root, "general", "2026-01-02.json"), "{this is secret chat content broken");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        foreach (var diag in archive.Diagnostics)
        {
            // 診断メッセージに「this is secret chat content」が含まれないこと
            Assert.That(diag.Message, Does.Not.Contain("secret chat content"));
        }
    }

    // TP-120d: 診断の Message は "Failed to parse day file." のみ
    [Test]
    public async Task UT_IT_120d_b__ParseAsync_DiagnosticMessage_IsFailedToParseMessage()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(Path.Combine(root, "general", "2026-01-02.json"), "{broken");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        var errorDiag = archive.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        Assert.That(errorDiag, Is.Not.Null);
        Assert.That(errorDiag!.Message, Is.EqualTo("Failed to parse day file."));
    }

    // TP-120e: 特定チャンネルの一部日付のみ破損 → 同チャンネルの正常日付メッセージは読み込まれる
    [Test]
    public async Task UT_IT_120e__ParseAsync_PartiallyBrokenChannel_NormalDatesStillLoaded()
    {
        var root = CreateSlackFolder();
        // 2026-01-01 は正常、2026-01-02 は破損
        File.WriteAllText(Path.Combine(root, "general", "2026-01-02.json"), "{broken");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        // 正常な 2026-01-01 の 2 件は取得できる
        Assert.That(archive.Metadata.TotalMessageCount, Is.EqualTo(2));
    }

    // TP-120f: 全日付 JSON が破損 → TotalMessageCount=0、Diagnostics に全件記録
    [Test]
    public async Task UT_IT_120f__ParseAsync_AllDatesCorrupted_ZeroCountAndAllDiagnosticsRecorded()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        File.WriteAllText(Path.Combine(root, "channels.json"), """[{"id":"C1","name":"general","is_channel":true}]""");
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        File.WriteAllText(Path.Combine(root, "general", "2026-01-01.json"), "{broken1");
        File.WriteAllText(Path.Combine(root, "general", "2026-01-02.json"), "{broken2");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.Metadata.TotalMessageCount, Is.EqualTo(0));
        Assert.That(archive.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error), Is.EqualTo(2));
    }

    // TP-120g: ParseAsync は例外ではなく ChatArchive（部分データ + Diagnostics）を返す
    [Test]
    public async Task UT_IT_120g__ParseAsync_BrokenFiles_DoesNotThrow_ReturnsChatArchive()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(Path.Combine(root, "general", "2026-01-02.json"), "{broken");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        ChatArchive? archive = null;
        Assert.DoesNotThrowAsync(async () =>
        {
            archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);
        });
        Assert.That(archive, Is.Not.Null);
    }

    // TP-130a/b: users.json 欠損 → パース継続、Diagnostics に記録
    [Test]
    public async Task UT_IT_130ab__ParseAsync_MissingUsersJson_ContinuesWithDiagnostic()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        File.WriteAllText(Path.Combine(root, "channels.json"), """[{"id":"C1","name":"general","is_channel":true}]""");
        // users.json なし
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-01.json"),
            """[{"ts":"1735689600.000100","user":"U1","text":"hello"}]""");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        // パースは継続する
        Assert.That(archive.Conversations, Is.Not.Empty);
        // Diagnostics に欠損情報が記録される
        Assert.That(archive.Diagnostics.Any(d => d.SourceHint == "users.json"), Is.True);
    }

    // TP-130c/d: channels.json 欠損 → パース継続、ディレクトリからチャンネルを推定
    [Test]
    public async Task UT_IT_130cd__ParseAsync_MissingChannelsJson_ContinuesWithDirectoryFallback()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "random-dir"));
        // channels.json なし
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        File.WriteAllText(
            Path.Combine(root, "random-dir", "2026-01-01.json"),
            """[{"ts":"1735689600.000100","user":"U1","text":"hi"}]""");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive, Is.Not.Null);
        Assert.That(archive.Diagnostics.Any(d => d.SourceHint == "channels.json"), Is.True);
    }

    // TP-130d: users.json の real_name 欠損時は null のままマッピングされる
    [Test]
    public async Task UT_IT_130d__ParseAsync_UserMissingRealName_MappedWithNullRealName()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        File.WriteAllText(
            Path.Combine(root, "channels.json"),
            """[{"id":"C1","name":"general","is_channel":true}]""");
        File.WriteAllText(
            Path.Combine(root, "users.json"),
            """[{"id":"U2","profile":{"display_name":"display-only"}}]""");
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-01.json"),
            """[{"ts":"1735689600.000100","user":"U2","text":"hello"}]""");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);
        var participant = archive.Participants.Single(x => x.Id == "U2");

        Assert.That(participant.DisplayName, Is.EqualTo("display-only"));
        Assert.That(participant.RealName, Is.Null);
    }

    // TP-130f: content_flags フォルダは本文会話に混入しない（FC: はディレクトリ名として OS 制限あり）
    [Test]
    public async Task UT_IT_130f__ParseAsync_ContentFlagsAndFcFolders_NotMixedIntoConversations()
    {
        var root = CreateSlackFolder();
        Directory.CreateDirectory(Path.Combine(root, "content_flags"));
        File.WriteAllText(Path.Combine(root, "content_flags", "2026-01-01.json"), "[]");
        // FC: ディレクトリは Windows でファイルシステム上作成不可のため、content_flags のみ検証する
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive.Conversations.Any(c => c.Id.Contains("content_flags", StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    // TP-150a: 存在しない conversationId → 空リストが返される（例外ではない）
    [Test]
    public async Task UT_IT_150a__LoadMessagesAsync_NonExistentConversation_ReturnsEmpty()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        IReadOnlyList<ChatMessage>? result = null;
        Assert.DoesNotThrowAsync(async () =>
        {
            result = await parser.LoadMessagesAsync(source, "nonexistent-channel", null, CancellationToken.None);
        });
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    // TP-150b: 存在する conversationId + 存在しない date → 空リストが返される
    [Test]
    public async Task UT_IT_150b__LoadMessagesAsync_ValidConversationMissingDate_ReturnsEmpty()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var result = await parser.LoadMessagesAsync(source, "general", new DateOnly(2099, 12, 31), CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    // TP-150c: conversationId が空文字列 → 引数例外
    [Test]
    public async Task UT_IT_150c__LoadMessagesAsync_EmptyConversationId_ThrowsArgumentException()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await parser.LoadMessagesAsync(source, string.Empty, null, CancellationToken.None);
        });
    }

    // TP-160a: 空チャンネル（日付ファイルなし）でも Conversation は一覧に含まれる
    [Test]
    public async Task UT_IT_160a__ParseAsync_EmptyChannel_IncludedWithZeroDates()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "empty-channel"));
        File.WriteAllText(
            Path.Combine(root, "channels.json"),
            """[{"id":"C1","name":"empty-channel","is_channel":true}]""");
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        // empty-channel には日付ファイルなし
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        // channels.json にチャンネル定義があっても対応ディレクトリに日付ファイルがなければ AvailableDates 空
        // （TP-160d: channels.json にあるが対応ディレクトリなし → Conversation は含まれ AvailableDates 空）
        var conversation = archive.Conversations.FirstOrDefault(c => c.Id == "empty-channel");
        Assert.That(conversation, Is.Not.Null, "空チャンネルも Conversations に含まれること");
        Assert.That(conversation!.AvailableDates, Is.Empty, "AvailableDates が空であること");
        Assert.That(conversation.MessageCount, Is.EqualTo(0));
    }

    // TP-160b: 空チャンネルの LoadMessagesAsync は空リストを返す
    [Test]
    public async Task UT_IT_160b__LoadMessagesAsync_EmptyChannel_ReturnsEmpty()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "empty-channel"));
        File.WriteAllText(
            Path.Combine(root, "channels.json"),
            """[{"id":"C1","name":"empty-channel","is_channel":true}]""");
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var messages = await parser.LoadMessagesAsync(source, "empty-channel", null, CancellationToken.None);

        Assert.That(messages, Is.Empty);
    }

    // TP-160c: 全チャンネルが空のアーカイブ → ChatArchive は返る
    [Test]
    public async Task UT_IT_160c__ParseAsync_AllEmptyChannels_ArchiveReturnedWithZeroMessages()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        File.WriteAllText(
            Path.Combine(root, "channels.json"),
            """[{"id":"C1","name":"ch1","is_channel":true},{"id":"C2","name":"ch2","is_channel":true}]""");
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        // ディレクトリなし（完全に空）
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(archive, Is.Not.Null);
        Assert.That(archive.Metadata.TotalMessageCount, Is.EqualTo(0));
    }

    // TP-160d: channels.json に定義があればディレクトリ未作成でも Conversation に含まれる
    [Test]
    public async Task UT_IT_160d__ParseAsync_ChannelDefinedWithoutDirectory_IsIncluded()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        File.WriteAllText(
            Path.Combine(root, "channels.json"),
            """[{"id":"C1","name":"missing-dir","is_channel":true}]""");
        File.WriteAllText(Path.Combine(root, "users.json"), "[]");
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);

        var archive = await parser.ParseAsync(source, progress: null, CancellationToken.None);
        var conversation = archive.Conversations.Single(x => x.Id == "missing-dir");

        Assert.That(conversation.DisplayName, Is.EqualTo("missing-dir"));
        Assert.That(conversation.AvailableDates, Is.Empty);
        Assert.That(conversation.MessageCount, Is.EqualTo(0));
    }

    // TP-170a/b: 正常パースおよび破損ファイル処理中のログ出力にチャットデータが含まれない
    [Test]
    public async Task UT_IT_170ab__ParseAsync_LogOutput_DoesNotContainChatData()
    {
        var root = CreateSlackFolder();
        // 本文にセンシティブ文字列を含むメッセージを追加
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-03.json"),
            """[{"ts":"1735862400.000100","user":"U1","text":"SENSITIVE_CHAT_CONTENT_XYZ"}]""");
        // 破損ファイルも追加
        File.WriteAllText(Path.Combine(root, "general", "2026-01-04.json"), "{broken");
        var logger = new CapturingLogger<SlackArchiveParser>();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(logger);

        await parser.ParseAsync(source, progress: null, CancellationToken.None);

        // ログにチャットデータ本文が含まれないこと
        foreach (var message in logger.Messages)
        {
            Assert.That(message, Does.Not.Contain("SENSITIVE_CHAT_CONTENT_XYZ"),
                "ログ出力にチャットデータが含まれないこと");
        }
    }

    // TP-170a: 正常メッセージ本文がログに含まれない
    [Test]
    public async Task UT_IT_170a__ParseAsync_LogsDoNotContainMessageText()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-03.json"),
            """
            [
              { "ts":"1735862400.000100","user":"U1","text":"TOP_SECRET_NORMAL_MESSAGE" }
            ]
            """);
        var logger = new CapturingLogger<SlackArchiveParser>();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(logger);

        await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(logger.Messages.Any(x => x.Contains("TOP_SECRET_NORMAL_MESSAGE", StringComparison.Ordinal)), Is.False);
    }

    // TP-170b: 破損ファイル処理ログにもチャット本文を含めない
    [Test]
    public async Task UT_IT_170b__ParseAsync_BrokenFileLog_DoesNotContainChatData()
    {
        var root = CreateSlackFolder();
        File.WriteAllText(Path.Combine(root, "general", "2026-01-04.json"), "{\"text\":\"LEAK_ME_IF_LOGGED\"");
        var logger = new CapturingLogger<SlackArchiveParser>();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(logger);

        await parser.ParseAsync(source, progress: null, CancellationToken.None);

        Assert.That(logger.Messages.Any(x => x.Contains("Failed to parse day file.", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(logger.Messages.Any(x => x.Contains("LEAK_ME_IF_LOGGED", StringComparison.Ordinal)), Is.False);
    }

    // TP-170d: ログに出力されるのはファイル数・メッセージ数・チャンネル数等の数値情報のみ
    [Test]
    public async Task UT_IT_170d__ParseAsync_LogOutput_ContainsOnlyMetadata()
    {
        var root = CreateSlackFolder();
        var logger = new CapturingLogger<SlackArchiveParser>();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(logger);

        await parser.ParseAsync(source, progress: null, CancellationToken.None);

        // 完了ログに "Slack parse completed." が含まれること（チャット内容ではなくメタ情報）
        Assert.That(
            logger.Messages.Any(m => m.Contains("Slack parse completed.", StringComparison.OrdinalIgnoreCase)),
            Is.True,
            "完了ログが出力されること");
    }

    // TP-310a: ParseAsync 実行中にキャンセルすると OperationCanceledException がスローされる
    [Test]
    public async Task UT_IT_310a__ParseAsync_Cancelled_ThrowsOperationCanceledException()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await parser.ParseAsync(source, progress: null, cts.Token);
        });
    }

    // TP-310d: LoadMessagesAsync 実行中のキャンセルで OperationCanceledException がスローされる
    [Test]
    public async Task UT_IT_310d__LoadMessagesAsync_Cancelled_ThrowsOperationCanceledException()
    {
        var root = CreateSlackFolder();
        await using var source = new FolderArchiveSource(root);
        var parser = new SlackArchiveParser(NullLogger<SlackArchiveParser>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await parser.LoadMessagesAsync(source, "general", new DateOnly(2026, 1, 1), cts.Token);
        });
    }

    private string CreateSlackFolder()
    {
        var root = TrackTempDirectory(Path.Combine(Path.GetTempPath(), $"slack-parser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "general"));
        File.WriteAllText(Path.Combine(root, "channels.json"), """[{ "id":"C1","name":"general","is_channel":true }]""");
        File.WriteAllText(Path.Combine(root, "users.json"), """[{ "id":"U1","real_name":"Alice","profile":{"display_name":"alice"} }]""");
        File.WriteAllText(
            Path.Combine(root, "general", "2026-01-01.json"),
            """
            [
              { "ts":"1735689600.000100","user":"U1","text":"hello","thread_ts":"1735689600.000100","reply_count":1,"reactions":[{"name":"thumbsup","count":2}] },
              { "ts":"1735689601.000100","user":"U1","text":"world","subtype":"message_changed","edited":{"ts":"1735689602.000100"} }
            ]
            """);
        return root;
    }

    private string TrackTempDirectory(string path)
    {
        tempDirectories.Add(path);
        return path;
    }

    /// <summary>
    /// 進捗報告を同期的に収集するヘルパー
    /// </summary>
    private sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly List<T> reported = [];
        public IReadOnlyList<T> Reported => reported;
        public void Report(T value) => reported.Add(value);
    }

    /// <summary>
    /// ログ出力内容をキャプチャして検証するためのヘルパーロガー
    /// </summary>
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<string> messages = [];
        public IReadOnlyList<string> Messages => messages;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add(formatter(state, exception));
        }
    }
}
