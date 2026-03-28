using System.Text.Json;
using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Models;
using Microsoft.Extensions.Logging;

namespace ChatArchiveViewer.Formats.Slack;

public sealed class SlackArchiveParser : IArchiveParser
{
    private readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<SlackArchiveParser> logger;

    public SlackArchiveParser(ILogger<SlackArchiveParser> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatArchive> ParseAsync(IArchiveSource source, IProgress<ArchiveLoadProgress>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ct.ThrowIfCancellationRequested();

        progress?.Report(new ArchiveLoadProgress { Phase = "root-inventory", Current = 0, Total = 5 });
        var diagnostics = new List<LoadDiagnostic>();

        var users = await LoadUsersAsync(source, diagnostics, ct);
        progress?.Report(new ArchiveLoadProgress { Phase = "users", Current = 1, Total = 5 });

        var channels = await LoadChannelsAsync(source, diagnostics, ct);
        progress?.Report(new ArchiveLoadProgress { Phase = "channels", Current = 2, Total = 5 });

        var userMap = users.Where(u => !string.IsNullOrWhiteSpace(u.Id)).ToDictionary(u => u.Id!, StringComparer.OrdinalIgnoreCase);

        var conversationBuilders = new Dictionary<string, ConversationBuilder>(StringComparer.OrdinalIgnoreCase);
        foreach (var channel in channels)
        {
            if (string.IsNullOrWhiteSpace(channel.Name))
            {
                continue;
            }

            var id = channel.Name!;
            conversationBuilders[id] = new ConversationBuilder
            {
                Id = id,
                DirectoryName = channel.Name!,
                DisplayName = channel.Name!,
                Topic = channel.Topic?.Value,
                Purpose = channel.Purpose?.Value,
                Type = SlackMappingHelpers.ToConversationType(channel),
                SlackChannelId = channel.Id
            };
        }

        var directories = await source.GetDirectoriesAsync(string.Empty, ct);
        var supportedDirectories = directories
            .Where(
                d => !d.Contains("content_flags", StringComparison.OrdinalIgnoreCase) &&
                     !d.StartsWith("FC:", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var directory in supportedDirectories)
        {
            ct.ThrowIfCancellationRequested();
            var files = await source.GetFilesAsync(directory, "*.json", ct);
            var dates = files
                .Select(file => TryParseDateFromPath(file))
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            if (dates.Length == 0)
            {
                continue;
            }

            var knownConversation = conversationBuilders.Values.FirstOrDefault(
                x => string.Equals(x.DirectoryName, directory, StringComparison.OrdinalIgnoreCase));
            if (knownConversation is null)
            {
                var generatedId = directory;
                knownConversation = new ConversationBuilder
                {
                    Id = generatedId,
                    DirectoryName = directory,
                    DisplayName = directory,
                    Type = ConversationType.Other
                };
                conversationBuilders[generatedId] = knownConversation;
            }

            knownConversation.AvailableDates = dates;
        }

        // user_profile フォールバック参加者マップ（users.json 未収録ユーザーを蓄積）
        var profileParticipantMap = new Dictionary<string, (string DisplayName, string? RealName)>(StringComparer.OrdinalIgnoreCase);

        var conversations = new List<Conversation>(conversationBuilders.Count);
        var totalMessageCount = 0;
        DateOnly? earliestDate = null;
        DateOnly? latestDate = null;

        foreach (var builder in conversationBuilders.Values.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            var messageCount = 0;
            foreach (var date in builder.AvailableDates)
            {
                ct.ThrowIfCancellationRequested();
                var relativePath = $"{builder.DirectoryName}/{date:yyyy-MM-dd}.json";
                if (!await source.FileExistsAsync(relativePath, ct))
                {
                    continue;
                }

                var (messages, diagnostic) = await TryParseDayFileAsync(source, builder.Id, relativePath, ct);
                if (diagnostic is not null)
                {
                    diagnostics.Add(diagnostic);
                }

                messageCount += messages.Count;

                // user_profile フォールバック参加者を収集
                foreach (var message in messages)
                {
                    if (message.ParticipantId is not null &&
                        !userMap.ContainsKey(message.ParticipantId) &&
                        message.ExtendedProperties.TryGetValue("user_profile_display_name", out var displayName))
                    {
                        message.ExtendedProperties.TryGetValue("user_profile_real_name", out var realName);
                        profileParticipantMap[message.ParticipantId] = (displayName, realName);
                    }
                }

                if (earliestDate is null || date < earliestDate)
                {
                    earliestDate = date;
                }

                if (latestDate is null || date > latestDate)
                {
                    latestDate = date;
                }
            }

            totalMessageCount += messageCount;
            conversations.Add(
                new Conversation
                {
                    Id = builder.Id,
                    DisplayName = builder.DisplayName,
                    Topic = builder.Topic,
                    Purpose = builder.Purpose,
                    Type = builder.Type,
                    AvailableDates = builder.AvailableDates,
                    MessageCount = messageCount
                });
        }

        // userMap 参加者 + user_profile フォールバック参加者をマージして順序付け
        var userMapParticipants = userMap.Values.Select(
            user => new Participant
            {
                Id = user.Id ?? "unknown",
                DisplayName = SlackMappingHelpers.ResolveDisplayName(user, null, user.Id, null),
                RealName = user.RealName
            });

        var profileOnlyParticipants = profileParticipantMap
            .Where(kvp => !userMap.ContainsKey(kvp.Key))
            .Select(kvp => new Participant
            {
                Id = kvp.Key,
                DisplayName = kvp.Value.DisplayName,
                RealName = kvp.Value.RealName
            });

        var participants = userMapParticipants
            .Concat(profileOnlyParticipants)
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var auxiliaryCount = 0;
        foreach (var file in SlackFormatConstants.AuxiliaryRootJsonFiles)
        {
            if (await source.FileExistsAsync(file, ct))
            {
                auxiliaryCount++;
            }
        }

        var metadata = new ArchiveMetadata
        {
            DisplayName = Path.GetFileName(source.DisplayPath),
            EarliestDate = earliestDate,
            LatestDate = latestDate,
            TotalMessageCount = totalMessageCount,
            ExtendedProperties = new Dictionary<string, string>
            {
                ["auxiliary_root_json_count"] = auxiliaryCount.ToString()
            }
        };

        progress?.Report(new ArchiveLoadProgress { Phase = "summary", Current = 5, Total = 5 });
        logger.LogInformation(
            "Slack parse completed. Conversations={ConversationCount} Participants={ParticipantCount} Messages={MessageCount} AuxiliaryCount={AuxiliaryCount}",
            conversations.Count,
            participants.Length,
            totalMessageCount,
            auxiliaryCount);

        return new ChatArchive
        {
            FormatId = SlackFormatConstants.FormatId,
            FormatDisplayName = SlackFormatConstants.DisplayName,
            Metadata = metadata,
            Conversations = conversations,
            Participants = participants,
            Diagnostics = diagnostics
        };
    }

    public async Task<IReadOnlyList<ChatMessage>> LoadMessagesAsync(
        IArchiveSource source,
        string conversationId,
        DateOnly? date,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("Conversation id is required.", nameof(conversationId));
        }

        ct.ThrowIfCancellationRequested();
        var directories = await source.GetDirectoriesAsync(string.Empty, ct);
        var directory = directories.FirstOrDefault(
            d => string.Equals(d, conversationId, StringComparison.OrdinalIgnoreCase))
            ?? directories.FirstOrDefault(
                d => string.Equals(d, conversationId.TrimStart('#'), StringComparison.OrdinalIgnoreCase));

        if (directory is null)
        {
            return Array.Empty<ChatMessage>();
        }

        var targetDates = new List<DateOnly>();
        if (date.HasValue)
        {
            targetDates.Add(date.Value);
        }
        else
        {
            var files = await source.GetFilesAsync(directory, "*.json", ct);
            targetDates.AddRange(
                files.Select(TryParseDateFromPath)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .Distinct()
                    .OrderBy(x => x));
        }

        var results = new List<ChatMessage>();
        foreach (var targetDate in targetDates)
        {
            ct.ThrowIfCancellationRequested();
            var relativePath = $"{directory}/{targetDate:yyyy-MM-dd}.json";
            if (!await source.FileExistsAsync(relativePath, ct))
            {
                continue;
            }

            // TryParseDayFileAsync を使用し、診断は呼び出し元に返さない（LoadMessages は診断収集しない）
            var (messages, _) = await TryParseDayFileAsync(source, conversationId, relativePath, ct);
            results.AddRange(messages);
        }

        return results.OrderBy(x => x.Timestamp).ToArray();
    }

    /// <summary>
    /// 1日分のメッセージファイルを読み込み、パース結果と診断情報を返す。
    /// ファイルが破損している場合は空リストと Error 診断を返す。
    /// </summary>
    private async Task<(IReadOnlyList<ChatMessage> messages, LoadDiagnostic? diagnostic)> TryParseDayFileAsync(
        IArchiveSource source,
        string conversationId,
        string relativePath,
        CancellationToken ct)
    {
        List<SlackMessage>? rawMessages;
        try
        {
            await using var stream = await source.OpenFileAsync(relativePath, ct);
            rawMessages = await JsonSerializer.DeserializeAsync<List<SlackMessage>>(stream, serializerOptions, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse day file. Path={Path}", relativePath);
            return (
                Array.Empty<ChatMessage>(),
                new LoadDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = "Failed to parse day file.",
                    SourceHint = relativePath
                });
        }

        if (rawMessages is null)
        {
            return (Array.Empty<ChatMessage>(), null);
        }

        var results = new List<ChatMessage>(rawMessages.Count);
        foreach (var raw in rawMessages)
        {
            if (string.IsNullOrWhiteSpace(raw.Ts))
            {
                continue;
            }

            DateTimeOffset timestamp;
            try
            {
                timestamp = SlackMappingHelpers.ParseSlackTimestamp(raw.Ts);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Skipped message due to invalid timestamp.");
                continue;
            }

            var attachmentList = raw.Files?
                .Select(
                    file => new MessageAttachment
                    {
                        Name = file.Name,
                        Title = file.Title,
                        Url = file.UrlPrivateDownload ?? file.UrlPrivate
                    })
                .ToArray() ?? Array.Empty<MessageAttachment>();

            var reactionList = raw.Reactions?
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => new MessageReaction { Name = x.Name!, Count = x.Count ?? 0 })
                .ToArray() ?? Array.Empty<MessageReaction>();

            var extendedProperties = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(raw.BotId))
            {
                extendedProperties["bot_id"] = raw.BotId!;
            }

            if (!string.IsNullOrWhiteSpace(raw.Subtype))
            {
                extendedProperties["subtype"] = raw.Subtype!;
            }

            // user_profile フォールバック情報を ExtendedProperties に格納
            if (raw.UserProfile is not null)
            {
                if (!string.IsNullOrWhiteSpace(raw.UserProfile.DisplayName))
                {
                    extendedProperties["user_profile_display_name"] = raw.UserProfile.DisplayName!;
                }

                if (!string.IsNullOrWhiteSpace(raw.UserProfile.RealName))
                {
                    extendedProperties["user_profile_real_name"] = raw.UserProfile.RealName!;
                }
            }

            results.Add(
                new ChatMessage
                {
                    Id = raw.Ts!,
                    ConversationId = conversationId,
                    ParticipantId = raw.User,
                    Timestamp = timestamp,
                    Text = raw.Text ?? string.Empty,
                    RawSubtype = raw.Subtype,
                    ThreadId = raw.ThreadTs,
                    IsThreadParent = raw.ReplyCount.GetValueOrDefault() > 0,
                    ReplyCount = raw.ReplyCount ?? 0,
                    IsEdited = raw.Edited is not null,
                    EditedAt = raw.Edited?.Ts is null ? null : SlackMappingHelpers.ParseSlackTimestamp(raw.Edited.Ts),
                    Type = SlackMappingHelpers.ToMessageType(raw.Subtype),
                    Attachments = attachmentList,
                    Reactions = reactionList,
                    ExtendedProperties = extendedProperties
                });
        }

        return (results.OrderBy(x => x.Timestamp).ToArray(), null);
    }

    private async Task<List<SlackChannel>> LoadChannelsAsync(
        IArchiveSource source,
        List<LoadDiagnostic> diagnostics,
        CancellationToken ct)
    {
        if (!await source.FileExistsAsync("channels.json", ct))
        {
            diagnostics.Add(
                new LoadDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = "channels.json was not found.",
                    SourceHint = "channels.json"
                });
            return new List<SlackChannel>();
        }

        try
        {
            await using var stream = await source.OpenFileAsync("channels.json", ct);
            return await JsonSerializer.DeserializeAsync<List<SlackChannel>>(stream, serializerOptions, ct) ??
                   new List<SlackChannel>();
        }
        catch (Exception ex)
        {
            diagnostics.Add(
                new LoadDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = "Failed to parse channels.json.",
                    SourceHint = "channels.json"
                });
            logger.LogError(ex, "Failed parsing channels.json. Exception={Exception}", ex.ToString());
            return new List<SlackChannel>();
        }
    }

    private async Task<List<SlackUser>> LoadUsersAsync(IArchiveSource source, List<LoadDiagnostic> diagnostics, CancellationToken ct)
    {
        if (!await source.FileExistsAsync("users.json", ct))
        {
            diagnostics.Add(
                new LoadDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = "users.json was not found.",
                    SourceHint = "users.json"
                });
            return new List<SlackUser>();
        }

        try
        {
            await using var stream = await source.OpenFileAsync("users.json", ct);
            return await JsonSerializer.DeserializeAsync<List<SlackUser>>(stream, serializerOptions, ct) ??
                   new List<SlackUser>();
        }
        catch (Exception ex)
        {
            diagnostics.Add(
                new LoadDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = "Failed to parse users.json.",
                    SourceHint = "users.json"
                });
            logger.LogError(ex, "Failed parsing users.json. Exception={Exception}", ex.ToString());
            return new List<SlackUser>();
        }
    }

    private static DateOnly? TryParseDateFromPath(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        return DateOnly.TryParseExact(fileName, "yyyy-MM-dd", out var date) ? date : null;
    }

    private sealed class ConversationBuilder
    {
        public required string Id { get; init; }

        public required string DirectoryName { get; init; }

        public required string DisplayName { get; init; }

        public string? Topic { get; init; }

        public string? Purpose { get; init; }

        public ConversationType Type { get; init; }

        public string? SlackChannelId { get; init; }

        public IReadOnlyList<DateOnly> AvailableDates { get; set; } = Array.Empty<DateOnly>();
    }
}
