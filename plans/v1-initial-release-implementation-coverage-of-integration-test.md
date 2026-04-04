# v1 初版リリース — 実装カバレッジ（統合テスト観点）

> Plan: [v1-initial-release.md](./v1-initial-release.md)
> 観点: [v1-initial-release-integration-test-points.md](./v1-initial-release-integration-test-points.md)
> 更新日: 2026年3月28日

---

## 凡例

| 状態 | 意味 |
|---|---|
| `Automated` | 対応する UnitTest が存在し、Plan 準拠の実装前進がある |
| `RecordedButSkipped` | テストは記録済みだが通常 CI 実行対象外にした理由がある |
| `ManualOnly` | 手動確認のみで対応する |
| `NotImplementedOrMismatch` | 未実装または対応テストなし。理由を明記 |

---

## カバレッジ一覧

### TP-010: Slack 形式の自動検出

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-010a | `Automated` | `UT_IT_010a__DetectAsync_WithChannelsAndDailyJson_ReturnsDetected` | channels.json + users.json + 日付 JSON で Detected=true、Confidence>0.7 を確認 |
| TP-010b | `Automated` | `UT_IT_010b__DetectAsync_ChannelsJsonOnly_ReturnsDetected` | channels.json のみで IsDetected=true を確認 |
| TP-010c | `Automated` | `UT_IT_010c__DetectAsync_UsersJsonOnly_ReturnsDetected` | users.json のみで IsDetected=true を確認 |
| TP-010d | `Automated` | `UT_IT_010d__DetectAsync_WithDmAndGroupChannels_ReturnsDetected` | is_im=true / is_group=true チャンネルでも検出成功を確認 |
| TP-010e | `Automated` | `UT_IT_010e__DetectAsync_FormatIdAndDisplayNameAreNonEmpty` | FormatId / FormatDisplayName が非空であることを確認 |
| TP-010f | `Automated` | `UT_IT_010f__DetectAsync_WithAuxiliaryArtifacts_ReturnsDetected` | canvases.json 等の補助アーティファクトが存在しても検出に影響しないことを確認 |
| TP-010g | `Automated` | `UT_IT_010g__DetectAsync_UnicodeDirName_ReturnsDetected` | Unicode を含むディレクトリ名で検出が成功することを確認 |

---

### TP-020: Slack アーカイブの構造パース

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-020a | `Automated` | `UT_IT_020a__ParseAsync_StandardStructure_ReturnsMappedArchive` | ChatArchive の Conversations / Participants / Metadata が返ることを確認 |
| TP-020b | `Automated` | `UT_IT_020b__ParseAsync_UsersJsonMappedToParticipants` | users.json の U1 が Participant にマッピングされることを確認 |
| TP-020c | `Automated` | `UT_IT_020c__ParseAsync_ChannelsJsonMappedToConversations` | channels.json の general が Conversation にマッピングされることを確認 |
| TP-020d | `Automated` | `UT_IT_020d__ParseAsync_AvailableDatesAndMessageCountAreSet` | AvailableDates と MessageCount が正しく集計されることを確認 |
| TP-020e | `Automated` | `UT_IT_020e__LoadMessages_ThreadParentAndReplyCount_AreMapped` | IsThreadParent=true / ReplyCount>0 / ThreadId が設定されることを確認 |
| TP-020f | `Automated` | `UT_IT_020f__LoadMessages_ReactionsMapped` | Reactions の Name / Count が正しくマッピングされることを確認 |
| TP-020g | `Automated` | `UT_IT_020g__LoadMessages_FilesAttachments_Mapped` | files セクションが Attachments に名前・タイトル・URL付きでマッピングされることを確認 |
| TP-020h | `Automated` | `UT_IT_020h__LoadMessages_SystemSubtype_MapsToSystemType` | message_changed サブタイプが MessageType.System にマッピングされることを確認 |
| TP-020i | `Automated` | `UT_IT_020i__ParseAsync_DmAndGroupChannels_TypesSetCorrectly` | DM は DirectMessage、Group は Group に ConversationType がマッピングされることを確認 |
| TP-020j | `Automated` | `UT_IT_020j__ParseAsync_MetadataDateRange_IsCorrect` | EarliestDate / LatestDate が全日付の範囲で正しく算出されることを確認 |
| TP-020k | `Automated` | `UT_IT_020k__ParseAsync_TotalMessageCount_IsCorrect` | TotalMessageCount が全チャンネルの合計であることを確認 |
| TP-020l | `Automated` | `UT_IT_020l__ParseAsync_ReportsProgress` | IProgress が少なくとも 1 回呼ばれ Phase が設定されることを確認 |
| TP-020m | `Automated` | `UT_IT_020m__ParseAsync_NormalCase_DiagnosticsHasNoErrors` | 正常パース時に Error 診断がないことを確認 |
| TP-020n | `Automated` | `UT_IT_020n__ParseAsync_AuxiliaryArtifacts_NotIncludedInConversations` | canvases.json 等が Conversations に混入しないことを確認 |
| TP-020o | `Automated` | `UT_IT_020o__ParseAsync_AuxiliaryCount_StoredInMetadata` | auxiliary_root_json_count が ExtendedProperties に格納されることを確認 |
| TP-020p | `Automated` | `UT_IT_020p__LoadMessages_EditedMessage_IsEditedSetAndTextPreserved` | IsEdited=true / EditedAt が設定されることを確認 |
| TP-020q | `Automated` | `UT_IT_020q__ParseAsync_MessageDeleted_DoesNotCrashAndMapsToSystem` | message_deleted を含んでもパーサが継続し、System メッセージとして保持されることを確認 |
| TP-020r | `Automated` | `UT_IT_020r__LoadMessages_FilesSection_MappedToAttachmentsNoDownload` | files セクションが Attachments にマッピングされ、外部アクセスなしを確認 |
| TP-020s | `Automated` | `UT_IT_020s__ParseAsync_UserProfileFallback_ResolvedParticipant` `UT_IT_020s_b__LoadMessages_UserProfile_StoredInExtendedProperties` | user_profile フォールバックで参加者解決・ExtendedProperties への格納を確認 |
| TP-020t | `Automated` | `UT_IT_020t__ParseAsync_PrivateUrls_NeverFetched` | url_private_download を含む場合でも外部アクセスなしを確認 |
| TP-020u | `Automated` | `UT_IT_020u__ParseAsync_UnicodeDirName_AvailableDatesAndCountCollected` | Unicode ディレクトリ名で会話の AvailableDates / MessageCount が正しく集計されることを確認 |

---

### TP-030: メッセージの日付指定読み込み

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-030a | `Automated` | `UT_IT_030a__LoadMessagesAsync_ValidConversationAndDate_ReturnsMessages` | 有効な conversationId + date でメッセージが返ることを確認 |
| TP-030b | `Automated` | `UT_IT_030b__LoadMessagesAsync_Messages_AreSortedByTimestamp` | メッセージが Timestamp 昇順であることを確認 |
| TP-030c | `Automated` | `UT_IT_030c__LoadMessagesAsync_RequiredFieldsAreSet` | Id / ConversationId / Timestamp / Text が設定されることを確認 |
| TP-030d | `Automated` | `UT_IT_030d__LoadMessagesAsync_ParticipantId_ExistsInArchiveParticipants` | ParseAsync の Participants と LoadMessagesAsync の ParticipantId 参照整合を確認 |
| TP-030e | `Automated` | `UT_IT_030e__LoadMessagesAsync_NullDate_ReturnsAllDates` | date=null で全日付メッセージが返ることを確認 |
| TP-030f | `Automated` | `UT_IT_030f__LoadMessagesAsync_MixedMessageTypes_AllReturned` | Normal / System 混在で全件返ることを確認 |

---

### TP-040: 形式レジストリによる全形式自動検出

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-040a | `Automated` | `UT_IT_040a__DetectAllAsync_ReturnsDetectionResultsForAllProviders` | レジストリ単体はスタブ provider で近似したが、`src/ChatArchiveViewer.Formats.Slack/SlackFormatProvider.cs` と `src/ChatArchiveViewer.App/App.xaml.cs` の既定登録を確認済み |
| TP-040b | `Automated` | `UT_IT_040b__DetectAllAsync_AllProvidersReturnNotDetected_WhenNonMatchingSource` | スタブ provider で全件 IsDetected=false を確認しつつ、既定配線は `App.xaml.cs` で `IArchiveFormatProvider -> SlackFormatProvider` 登録済み |
| TP-040c | `Automated` | `UT_IT_040c__DetectAllAsync_EmptyRegistry_ReturnsEmpty` | 空レジストリで空リストが返ることを確認 |
| TP-040d | `Automated` | `UT_IT_040d__GetAllProviders_ReturnsAllRegisteredProviders` | スタブ provider で登録一覧を確認しつつ、実配線は `App.xaml.cs` の DI 登録で補完確認 |
| TP-040e | `Automated` | `UT_IT_040e__GetProvider_ExistingId_ReturnsProvider` | スタブ provider で ID 解決を確認しつつ、実実装 `SlackFormatProvider` が `src/` に存在することを確認 |
| TP-040f | `Automated` | `UT_IT_040f__GetProvider_NonExistingId_ReturnsNull` | レジストリ単体動作を確認。既定構成では `SlackFormatProvider` のみ登録されるため非存在 ID は null になる |

---

### TP-050: ZIP 入力ソースのファイルアクセスとリソース管理

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-050a | `Automated` | `UT_IT_050a__GetFilesAsync_ReturnsFileList` | GetFilesAsync でファイル一覧を取得できることを確認 |
| TP-050b | `Automated` | `UT_IT_050b__OpenFileAsync_ReadsExtractedFile` | OpenFileAsync のストリーム取得と内容読取を確認 |
| TP-050c | `Automated` | `UT_IT_050c__FileExistsAsync_ReturnsTrueForExistingFile` | FileExistsAsync が true を返すことを確認 |
| TP-050d | `Automated` | `UT_IT_050d__DirectoryExistsAsync_ReturnsTrueForExistingDirectory` | DirectoryExistsAsync が true を返すことを確認 |
| TP-050e | `Automated` | `UT_IT_050e__GetDirectoriesAsync_ReturnsDirectoryList` | GetDirectoriesAsync でディレクトリ一覧を取得できることを確認 |
| TP-050f | `Automated` | `UT_IT_050f__FileExistsAsync_ReturnsFalseForMissingFile` | FileExistsAsync が false を返すことを確認 |
| TP-050g | `Automated` | `UT_IT_050g__DisplayPath_ContainsZipFilePath` | DisplayPath が ZIP ファイルパスと一致することを確認 |
| TP-050h | `Automated` | `UT_IT_050h__DisposeAsync_RemovesTempDirectory` | Dispose 後に同じ ZIP を再オープンできる（一時ディレクトリが削除・再作成される）ことを確認 |

---

### TP-060: フォルダ入力ソースのファイルアクセス

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-060a | `Automated` | `UT_IT_060a__GetFilesAsync_ReturnsFilesInDirectory` | GetFilesAsync でファイル一覧を取得できることを確認 |
| TP-060b | `Automated` | `UT_IT_060b__OpenFileAsync_ReturnsReadableStream` | OpenFileAsync でストリームを取得できることを確認 |
| TP-060c | `Automated` | `UT_IT_060c__FileExistsAsync_ReturnsTrueForExistingFile` | FileExistsAsync が true を返すことを確認 |
| TP-060d | `Automated` | `UT_IT_060d__DirectoryExistsAsync_ReturnsTrueForExistingDirectory` | DirectoryExistsAsync が true を返すことを確認 |
| TP-060e | `Automated` | `UT_IT_060e__GetDirectoriesAsync_ReturnsDirectories` | GetDirectoriesAsync でディレクトリ一覧を取得できることを確認 |
| TP-060f | `Automated` | `UT_IT_060f__FileExistsAsync_ReturnsFalseForMissingFile` | FileExistsAsync が false を返すことを確認 |
| TP-060g | `Automated` | `UT_IT_060g__DisplayPath_ContainsFolderPath` | DisplayPath がフォルダパスと一致することを確認 |
| TP-060h | `Automated` | `UT_IT_060h__GetFilesAsync_WithPattern_ReturnsOnlyMatchingFiles` | パターン指定でフィルタが効くことを確認 |

---

### TP-070: キーワード検索の結果精度

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-070a | `Automated` | `UT_IT_070a__SearchAsync_WithMatchingKeyword_ReturnsMatchedMessages` | InMemory の `ChatArchive` / `ChatMessage` で近似したが、実装本体 `src/ChatArchiveViewer.Core/Services/SearchService.cs` と既定 DI `App.xaml.cs` を確認済み |
| TP-070b | `Automated` | `UT_IT_070b__SearchAsync_MultipleChannels_ReturnsMatchesFromAllChannels` | InMemory データで複数チャンネル横断検索を確認。既定構成でも `SearchService` は `App.xaml.cs` で登録済み |
| TP-070c | `Automated` | `UT_IT_070c__SearchAsync_NoMatch_ReturnsEmpty` | InMemory データで no-match を確認。実装本体は `src/ChatArchiveViewer.Core/Services/SearchService.cs` |
| TP-070d | `Automated` | `UT_IT_070d__SearchAsync_CaseInsensitive_Matches` `SearchServiceTests.SearchAsync_IsCaseInsensitive` | InMemory データで大文字小文字非依存を確認。既定配線は `App.xaml.cs` に存在 |
| TP-070e | `Automated` | `UT_IT_070e__SearchAsync_ResultsContainMessageAndChannelInfo` | InMemory データで結果 DTO 内容を確認。実実装は `src/` 側に存在し stub 置換ではない |
| TP-070f | `Automated` | `UT_IT_070f__SearchAsync_ResultsAreOrdered` | InMemory データで順序規則を確認。実装本体 `SearchService` は既定 DI 登録済み |

---

### TP-080: 日付フィルタによる会話絞り込み

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-080a | `Automated` | `UT_IT_080a__FilterByDate_ReturnsOnlyConversationsContainingDate` | InMemory `Conversation` 配列で近似したが、実装本体 `src/ChatArchiveViewer.Core/Services/DateFilterService.cs` と DI 登録を確認済み |
| TP-080b | `Automated` | `UT_IT_080b__FilterByDate_SingleMatch_ReturnsOneConversation` | InMemory データで単一ヒットを確認。既定構成でも `DateFilterService` は `App.xaml.cs` で登録済み |
| TP-080c | `Automated` | `UT_IT_080c__FilterByDate_NoMatch_ReturnsEmpty` | InMemory データで no-match を確認。実装本体は `src/ChatArchiveViewer.Core/Services/DateFilterService.cs` |
| TP-080d | `Automated` | `UT_IT_080d__FilterByDate_EarliestDate_ReturnsMatchingConversation` | InMemory データで境界日付を確認。既定配線は `App.xaml.cs` に存在 |
| TP-080e | `Automated` | `UT_IT_080e__FilterByDate_LatestDate_ReturnsMatchingConversation` | InMemory データで最新日境界を確認。実装本体は `src/` 側に存在 |
| TP-080f | `Automated` | `UT_IT_080f__FilterByDate_EmptyChannelExcluded` | InMemory データで空チャンネル除外を確認。stub のみでなく実サービスと既定 DI を確認済み |

---

### TP-090: About 情報の生成と必須表示項目

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-090a | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | AboutViewModel は WinUI 3 UI 実装。アプリ起動が必要なため手動確認 |
| TP-090b | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | 同上。バージョン番号は実行時アセンブリから取得 |
| TP-090c | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | 同上。非公式表記は UI 文字列の目視確認が必要 |
| TP-090d | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | 同上。外部通信なし表記は UI 文字列の目視確認が必要 |
| TP-090e | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | 同上。対応形式一覧はアプリ起動後の表示確認が必要 |
| TP-090f | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | 同上。URL リンクの活性状態は UI 確認が必要 |
| TP-090g | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | 同上。URL 未設定時の非表示は UI 確認が必要 |
| TP-090h | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | 同上。OSS リポジトリ URL は UI 確認が必要 |
| TP-090i | `ManualOnly` | `src/ChatArchiveViewer.App/ViewModels/AboutViewModel.cs` | 同上。ライブラリ一覧は UI 確認が必要 |

---

### TP-110: 未対応形式アーカイブの検出拒否

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-110a | `Automated` | `UT_IT_110a__DetectAsync_NoSlackFiles_ReturnsNotDetected` | Slack ファイルなしで IsDetected=false を確認 |
| TP-110b | `Automated` | `UT_IT_110b__DetectAsync_NonSlackChannelsJson_ReturnsNotDetected` | channels.json が非 Slack 構造で IsDetected=false を確認 |
| TP-110c | `Automated` | `UT_IT_110c__DetectAsync_BrokenChannelsJson_ReturnsNotDetectedWithoutThrowing` | channels.json が JSON 構文エラーで例外なく IsDetected=false を確認 |
| TP-110d | `Automated` | `UT_IT_110d__DetectAllAsync_WithSlackProviderAndNonSlackSource_ReturnsAllFalse` | SlackFormatProvider を実登録した Registry 経由で非 Slack ソースが未検出になることを確認 |

---

### TP-120: 破損 JSON 混入時の部分読み込み継続

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-120a | `Automated` | `UT_IT_120abc__ParseAsync_MixedBrokenAndNormalFiles_NormalMessagesKeptDiagnosticsRecorded` | 正常ファイルのメッセージが含まれることを確認 |
| TP-120b | `Automated` | `UT_IT_120abc__ParseAsync_MixedBrokenAndNormalFiles_NormalMessagesKeptDiagnosticsRecorded` | Diagnostics に Warning 以上が記録されることを確認 |
| TP-120c | `Automated` | `UT_IT_120c__ParseAsync_BrokenFile_DiagnosticContainsSourceHint` | SourceHint に破損ファイル名が含まれることを確認 |
| TP-120d | `Automated` | `UT_IT_120d__ParseAsync_DiagnosticMessage_DoesNotContainChatData` `UT_IT_120d_b__ParseAsync_DiagnosticMessage_IsFailedToParseMessage` | 診断 Message にチャットデータが含まれず、"Failed to parse day file." であることを確認 |
| TP-120e | `Automated` | `UT_IT_120e__ParseAsync_PartiallyBrokenChannel_NormalDatesStillLoaded` | 同チャンネルの正常日付メッセージが読み込まれることを確認 |
| TP-120f | `Automated` | `UT_IT_120f__ParseAsync_AllDatesCorrupted_ZeroCountAndAllDiagnosticsRecorded` | TotalMessageCount=0、全件 Error 診断を確認 |
| TP-120g | `Automated` | `UT_IT_120g__ParseAsync_BrokenFiles_DoesNotThrow_ReturnsChatArchive` | ParseAsync が例外ではなく ChatArchive を返すことを確認 |

---

### TP-130: メタデータファイル欠損への耐性

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-130a | `Automated` | `UT_IT_130ab__ParseAsync_MissingUsersJson_ContinuesWithDiagnostic` | users.json 欠損でパース継続を確認 |
| TP-130b | `Automated` | `UT_IT_130ab__ParseAsync_MissingUsersJson_ContinuesWithDiagnostic` | Diagnostics に users.json 欠損情報が記録されることを確認 |
| TP-130c | `Automated` | `UT_IT_130cd__ParseAsync_MissingChannelsJson_ContinuesWithDirectoryFallback` | channels.json 欠損でディレクトリ推定によりパース継続を確認 |
| TP-130d | `Automated` | `UT_IT_130d__ParseAsync_UserMissingRealName_MappedWithNullRealName` | users.json の real_name 欠損時に DisplayName は保持され、RealName は null のままマッピングされることを確認 |
| TP-130e | `Automated` | `UT_IT_130e__ParseAsync_NoAuxiliaryArtifacts_StoresZeroCount` | 補助アーティファクトが存在しない通常ケースで `auxiliary_root_json_count=0` が記録されることを確認 |
| TP-130f | `Automated` | `UT_IT_130f__ParseAsync_ContentFlagsAndFcFolders_NotMixedIntoConversations` | content_flags フォルダが Conversations に混入しないことを確認（FC: は Windows ファイルシステム制限のためディレクトリ名使用不可、コードフィルタは実装済み） |

---

### TP-140: ZIP 不正エントリ（スリップ攻撃）の防御

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-140a | `Automated` | `UT_IT_140a__ZipSlipEntry_Throws` `ZipArchiveSourceTests.Ctor_WithZipSlipEntry_Throws` | `../` を含むエントリで InvalidDataException がスローされることを確認 |
| TP-140b | `Automated` | `UT_IT_140a__ZipSlipEntry_Throws` | ルートディレクトリ外を指すエントリで展開が拒否されることを確認（TP-140a と同一テスト） |
| TP-140c | `Automated` | `UT_IT_140c__Ctor_WithCorruptedZip_ThrowsInvalidDataException` | 破損 ZIP バイト列から ZipArchiveSource を生成すると InvalidDataException がスローされることを確認 |
| TP-140d | `Automated` | `UT_IT_140d__EmptyZip_CanBeCreatedAsSource` | 空の ZIP で IArchiveSource として生成可能かつ中身が空であることを確認 |

---

### TP-150: 存在しないチャンネル・日付の読み込み要求

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-150a | `Automated` | `UT_IT_150a__LoadMessagesAsync_NonExistentConversation_ReturnsEmpty` | 存在しない conversationId で空リストが返ることを確認 |
| TP-150b | `Automated` | `UT_IT_150b__LoadMessagesAsync_ValidConversationMissingDate_ReturnsEmpty` | 存在しない date で空リストが返ることを確認 |
| TP-150c | `Automated` | `UT_IT_150c__LoadMessagesAsync_EmptyConversationId_ThrowsArgumentException` | 空文字列で ArgumentException がスローされることを確認 |

---

### TP-160: 空アーカイブ・空チャンネルの取り扱い

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-160a | `Automated` | `UT_IT_160a__ParseAsync_EmptyChannel_IncludedWithZeroDates` | 空チャンネルが Conversations に含まれ AvailableDates 空 / MessageCount=0 を確認 |
| TP-160b | `Automated` | `UT_IT_160b__LoadMessagesAsync_EmptyChannel_ReturnsEmpty` | 日付ファイルを持たない空チャンネルに対して LoadMessagesAsync が空リストを返すことを確認 |
| TP-160c | `Automated` | `UT_IT_160c__ParseAsync_AllEmptyChannels_ArchiveReturnedWithZeroMessages` | 全チャンネルが空の場合でも ChatArchive が返り TotalMessageCount=0 を確認 |
| TP-160d | `Automated` | `UT_IT_160d__ParseAsync_ChannelDefinedWithoutDirectory_IsIncluded` | channels.json に定義済みなら対応ディレクトリ未作成でも Conversation に含まれ 0 件として扱われることを確認 |

---

### TP-170: ログ出力にチャットデータが含まれないこと

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-170a | `Automated` | `UT_IT_170a__ParseAsync_LogsDoNotContainMessageText` | 正常メッセージ本文がログへ出力されないことを CapturingLogger で確認 |
| TP-170b | `Automated` | `UT_IT_170b__ParseAsync_BrokenFileLog_DoesNotContainChatData` | 破損日付ファイルのエラーログにもチャット本文が含まれないことを確認 |
| TP-170c | `Automated` | `UT_IT_170c__SearchAsync_LogsDoNotContainKeywordOrMessageText` | SearchService に件数メタデータのみの安全なログを追加し、検索キーワードや本文がログへ出ないことを CapturingLogger で確認 |
| TP-170d | `Automated` | `UT_IT_170d__ParseAsync_LogOutput_ContainsOnlyMetadata` | 完了ログに "Slack parse completed." のメタ情報のみが含まれることを確認 |

---

### TP-210: 大量チャンネル・大量メッセージの処理

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-210a | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_210abc__LargeArchive_LoadAndSearch_RequiresLoadEnvironment` | 100 チャンネル以上のテストは CI 実行時間を過度に増大させるため `[Explicit]` として記録 |
| TP-210b | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_210abc__LargeArchive_LoadAndSearch_RequiresLoadEnvironment` | 1 年分の日付ファイル生成は I/O コストが高く、専用負荷環境向けのため `[Explicit]` として記録 |
| TP-210c | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_210abc__LargeArchive_LoadAndSearch_RequiresLoadEnvironment` | 大量メッセージ検索は負荷テスト環境での実施が適切なため `[Explicit]` として記録 |
| TP-210d | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_210de__LargeArchive_ProgressAndCancellation_RequiresLoadEnvironment` | 大量データでの進捗報告検証は通常実行では重いため `[Explicit]` として記録 |
| TP-210e | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_210de__LargeArchive_ProgressAndCancellation_RequiresLoadEnvironment` | 大量データ処理中のキャンセル応答性は負荷テスト環境での実施が適切なため `[Explicit]` として記録 |

---

### TP-310: 読み込み中キャンセルとリソース解放

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-310a | `Automated` | `UT_IT_310a__ParseAsync_Cancelled_ThrowsOperationCanceledException` | 事前キャンセルで OperationCanceledException がスローされることを確認 |
| TP-310b | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_310bc__CancellationCleanup_RequiresIntegrationVerification` | キャンセル後の IArchiveSource リソース解放確認はタイミング制御が困難なため `[Explicit]` として記録。DisposeAsync の単体テストは TP-050h で確認済み |
| TP-310c | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_310bc__CancellationCleanup_RequiresIntegrationVerification` | キャンセル後の後続操作でリソースリークなし確認は E2E / 統合レベルの検証が必要なため `[Explicit]` として記録 |
| TP-310d | `Automated` | `UT_IT_310d__LoadMessagesAsync_Cancelled_ThrowsOperationCanceledException` | 事前キャンセルで OperationCanceledException がスローされることを確認 |
| TP-310e | `Automated` | `UT_IT_310e__SearchAsync_PreCancelledToken_ThrowsOperationCanceledException` | 事前キャンセル済みトークンで SearchAsync 開始時に OperationCanceledException がスローされることを確認 |

---

### TP-320: アーカイブ連続切り替え（再読み込み）

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-320a | `Automated` | `UT_IT_320a__ReopenSameZip_AfterDispose_WorksCorrectly` | Dispose 後に同じ ZIP を再オープンして正常動作することを確認 |
| TP-320b | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_320bcd__ArchiveSwitching_RequiresApplicationLevelVerification` | ZIP→フォルダ切り替えはアプリレベルの状態管理テストが必要なため `[Explicit]` として記録 |
| TP-320c | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_320bcd__ArchiveSwitching_RequiresApplicationLevelVerification` | フォルダ→ZIP 切り替えも同上の理由で `[Explicit]` として記録 |
| TP-320d | `RecordedButSkipped` | `ChatArchiveViewer.Core.Tests.CoverageRecordedButSkippedTests.UT_IT_320bcd__ArchiveSwitching_RequiresApplicationLevelVerification` | 連続 3 回以上の切り替えは E2E / アプリレベルの検証が必要なため `[Explicit]` として記録。TP-320a で基本的な再オープンは確認済み |

---

## 今回の変更サマリ

### Automated に前進した ID

TP-010a,b,c,d,e,f,g / TP-020a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u / TP-030a,b,c,d,e,f / TP-040a,b,c,d,e,f / TP-050a,b,c,d,e,f,g,h / TP-060a,b,c,d,e,f,g,h / TP-070a,b,c,d,e,f / TP-080a,b,c,d,e,f / TP-110a,b,c,d / TP-120a,b,c,d,e,f,g / TP-130a,b,c,d,e,f / TP-140a,b,c,d / TP-150a,b,c / TP-160a,b,c,d / TP-170a,b,c,d / TP-310a,d,e / TP-320a

### RecordedButSkipped に整理した ID

TP-210a,b,c,d,e（負荷テスト）/ TP-310b,c（タイミング制御困難）/ TP-320b,c,d（アプリレベル統合必要）

### ManualOnly に整理した ID

TP-090a,b,c,d,e,f,g,h,i（WinUI 3 UI / アプリ起動が必要）

### NotImplementedOrMismatch のまま残った ID

該当なし。

### 追加・更新した主な設計・実装

- **`SlackArchiveParser.cs`**: `TryParseDayFileAsync` を private メソッドとして抽出。ParseAsync で LoadMessagesAsync を呼ばず直接 TryParseDayFileAsync を使用。user_profile フォールバック参加者を ParseAsync で収集・マージ。ログメッセージから `Exception={Exception}` 重複出力を削除。`LoadMessagesAsync` は TryParseDayFileAsync を内部使用し診断を破棄。

### 追加・更新した主なテスト

- 新規: `FolderArchiveSourceTests.cs`（TP-060 全 8 ID）
- 新規: `CoverageRecordedButSkippedTests.cs`（TP-210 / TP-310b,c / TP-320b,c,d の `[Explicit]` 記録）
- 拡張: `ZipArchiveSourceTests.cs`（TP-050 全 8 ID + TP-320a + TP-140a,d）
- 拡張: `ArchiveFormatRegistryTests.cs`（TP-040 6 ID）
- 拡張: `DateFilterServiceTests.cs`（TP-080 全 6 ID）
- 拡張: `SearchServiceTests.cs`（TP-070 全 6 ID + TP-170c）
- 拡張: `SlackFormatDetectorTests.cs`（TP-010 全 7 ID + TP-110 a,b,c）
- 大幅拡張: `SlackArchiveParserTests.cs`（TP-020, TP-030, TP-120, TP-130, TP-150, TP-160, TP-170, TP-310 多数）

### Plan 上の要求対応サマリ

- **T-030（日付単位メッセージ抽出）**: TryParseDayFileAsync で ParseAsync と LoadMessagesAsync が共通ロジックを使用
- **T-040（破損 JSON 継続読み込み）**: TryParseDayFileAsync で Error 診断を返し ParseAsync が収集。TP-120 系テストで確認
- **T-060（空チャンネル・空日付）**: TP-160 系テストで確認
- **T-130（メタデータ不足継続）**: TP-130 系テストで確認
- **T-150（補助アーティファクト安全識別）**: TP-020n/o、TP-130f テストで確認
- **T-170（Slack URL 自動取得しない）**: TP-020t テストで確認
- **T-180（ログにチャットデータを含めない）**: TP-170 系テストで CapturingLogger による検証（SlackArchiveParser / SearchService）

### 人間が次に判断すべき項目

1. **TP-210 系**: 負荷テストをどの環境で常時運用するか判断
2. **TP-310b/c・TP-320b/c/d**: 統合/E2E 系シナリオを別テストレイヤに持つか判断
