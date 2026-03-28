# v1 初版リリース — チャットログアーカイブビューア

## Background / Goal

ローカルに保存されたチャットログアーカイブを、日時・チャンネル・会話単位で読みやすく閲覧するための Windows デスクトップアプリケーション（WinUI 3 / .NET 10 / MSIX）を新規作成する。

初版では Slack エクスポート形式をサポートするが、アプリ全体の設計は **汎用チャットログアーカイブビューア** として成立させ、将来の形式追加を容易にする。

Microsoft Store 公開を前提とし、完全ローカル動作・ログイン不要・外部通信なし・読み取り専用を厳守する。

### 関連ドキュメント

- Runtime Evidence: [v1-initial-release-runtime-evidence.md](./v1-initial-release-runtime-evidence.md)
- Integration Test Points: [v1-initial-release-integration-test-points.md](./v1-initial-release-integration-test-points.md)

---

## Non-goals

| # | 明示的な非目標 |
|---|---|
| NG-1 | Slack クライアント機能（API 通信・認証・トークン）は一切実装しない |
| NG-2 | アーカイブの編集・変換・再エクスポートは行わない |
| NG-3 | クラウドストレージ連携・同期機能は提供しない |
| NG-4 | リアルタイムチャット機能は提供しない |
| NG-5 | AI 要約・OCR・画像解析は行わない |
| NG-6 | 初版では Slack 以外の形式を実装しない（ただし設計は pluggable にする） |
| NG-7 | 初版ではリッチな添付プレビュー（画像インライン表示等）は不要 |
| NG-8 | Slack 公式・認定・提携を示唆する表現は一切使用しない |
| NG-9 | 初版では Slack TXT エクスポート本文の閲覧や、Canvas / Huddle / Lists の個別ビューアは実装しない |

---

## Current state summary

- リポジトリには LICENSE (MIT)、README.md、.gitignore、エージェント定義のみが存在
- ソースコード・プロジェクトファイル・ソリューションファイルは未作成
- すべて新規作成となる

---

## Proposed design / architecture delta

### ソリューション構成

```
ChatArchiveViewer.sln
src/
  ChatArchiveViewer.App/                  ← WinUI 3 MSIX パッケージアプリ
  ChatArchiveViewer.Core/                 ← ドメインモデル・抽象・サービス
  ChatArchiveViewer.Formats.Slack/        ← Slack 形式プロバイダ
tests/
  ChatArchiveViewer.Core.Tests/           ← Core のユニットテスト
  ChatArchiveViewer.Formats.Slack.Tests/  ← Slack 形式のユニットテスト
```

### プロジェクト依存関係

```
ChatArchiveViewer.App
  ├── ChatArchiveViewer.Core
  ├── ChatArchiveViewer.Formats.Slack
  ├── Microsoft.WindowsAppSDK
  ├── CommunityToolkit.Mvvm
  ├── Microsoft.Extensions.DependencyInjection
  ├── Microsoft.Extensions.Logging
  ├── Microsoft.Extensions.Hosting
  └── Serilog.Extensions.Logging + Serilog.Sinks.File

ChatArchiveViewer.Core
  ├── Microsoft.Extensions.Logging.Abstractions
  └── (System.Text.Json, System.IO.Compression は BCL 組み込み)

ChatArchiveViewer.Formats.Slack
  ├── ChatArchiveViewer.Core
  └── Microsoft.Extensions.Logging.Abstractions
```

### NuGet パッケージとライセンス

| パッケージ | ライセンス | 用途 |
|---|---|---|
| Microsoft.WindowsAppSDK | MIT | WinUI 3 フレームワーク |
| Microsoft.Windows.SDK.BuildTools | MIT | Windows SDK ビルドツール |
| CommunityToolkit.Mvvm | MIT | MVVM パターン基盤 |
| Microsoft.Extensions.DependencyInjection | MIT | DI コンテナ |
| Microsoft.Extensions.Logging | MIT | ログ抽象化 |
| Microsoft.Extensions.Hosting | MIT | ホストビルダーパターン |
| Serilog.Extensions.Logging | Apache 2.0 | Serilog ↔ ILogger ブリッジ |
| Serilog.Sinks.File | Apache 2.0 | ファイルログ出力 |
| NUnit | MIT | テストフレームワーク |
| NUnit3TestAdapter | MIT | テストランナー |
| NSubstitute | BSD-3 | モックフレームワーク（テスト専用） |
| FluentAssertions | Apache 2.0 | アサーション補助（テスト専用） |

> **注**: NSubstitute はリフレクションを内部使用するが、テストインフラとしてのみ使用し、プロダクションコードではリフレクションを使用しない。copilot-instructions.md のリフレクション制限はプロダクションコードに対する方針として扱う。

### C4 Vocabulary（Component レベル）

| ID | Kind | Formal Name | 役割 | 実装アドレス |
|---|---|---|---|---|
| C-App | Container | ChatArchiveViewer | WinUI 3 デスクトップアプリ | `src/ChatArchiveViewer.App/` |
| M-Shell | Component | AppShell | メインウインドウ・ナビゲーション管理 | `App/MainWindow.xaml.cs` |
| M-NavSvc | Component | NavigationService | ページ遷移サービス | `App/Services/NavigationService.cs` |
| M-OpenSvc | Component | ArchiveOpenService | ファイル/フォルダピッカー経由のアーカイブオープン | `Core/Services/ArchiveOpenService.cs` |
| M-FormatReg | Component | FormatRegistry | 形式プロバイダの登録・検索・自動検出 | `Core/Services/ArchiveFormatRegistry.cs` |
| M-LoadSvc | Component | ArchiveLoadService | アーカイブ読み込みオーケストレーション | `Core/Services/ArchiveLoadService.cs` |
| M-ZipSrc | Component | ZipArchiveSource | ZIP 入力ソース（安全な展開） | `Core/Services/ZipArchiveSource.cs` |
| M-FolderSrc | Component | FolderArchiveSource | フォルダ入力ソース | `Core/Services/FolderArchiveSource.cs` |
| M-GenModel | Component | GenericChatModel | 汎用チャットモデル群 | `Core/Models/` |
| M-DateFilter | Component | DateFilterService | 日付フィルタリング | `Core/Services/DateFilterService.cs` |
| M-Search | Component | SearchService | キーワード検索 | `Core/Services/SearchService.cs` |
| M-SlackFmt | Component | SlackFormatProvider | Slack 形式検出・パース・変換 | `Formats.Slack/` |
| M-OverviewVM | Component | ArchiveOverviewVM | アーカイブ概要情報（ArchiveBrowsePage 内ヘッダー等で使用） | `App/ViewModels/ArchiveOverviewViewModel.cs` |
| M-BrowseVM | Component | ArchiveBrowseVM | list/details 閲覧画面のメイン ViewModel（チャンネル・日付選択状態管理） | `App/ViewModels/ArchiveBrowseViewModel.cs` |
| M-MsgListVM | Component | MessageListVM | メッセージ一覧表示 ViewModel | `App/ViewModels/MessageListViewModel.cs` |
| M-SearchVM | Component | SearchVM | 検索画面 ViewModel | `App/ViewModels/SearchViewModel.cs` |
| M-AboutVM | Component | AboutVM | About 画面 ViewModel | `App/ViewModels/AboutViewModel.cs` |
| M-SettingsVM | Component | SettingsVM | 設定画面 ViewModel（テーマ選択と保存） | `App/ViewModels/SettingsViewModel.cs` |
| X-Picker | External | WindowsFilePicker | Windows ファイル/フォルダピッカー | WinRT API |
| X-FS | External | FileSystem | ローカルファイルシステム | OS |
| X-Browser | External | DefaultBrowser | デフォルトブラウザ | OS |
| X-TempDir | External | TempDirectory | 一時展開先ディレクトリ | OS temp |

### Core 抽象（インターフェース設計）

```csharp
// アーカイブ入力ソースの抽象化（ZIP/フォルダ共通）
public interface IArchiveSource : IAsyncDisposable
{
    string DisplayPath { get; }
    Task<IReadOnlyList<string>> GetFilesAsync(string relativePath, string pattern, CancellationToken ct);
    Task<Stream> OpenFileAsync(string relativePath, CancellationToken ct);
    Task<bool> FileExistsAsync(string relativePath, CancellationToken ct);
    Task<bool> DirectoryExistsAsync(string relativePath, CancellationToken ct);
    Task<IReadOnlyList<string>> GetDirectoriesAsync(string relativePath, CancellationToken ct);
}

// アーカイブ形式プロバイダ
public interface IArchiveFormatProvider
{
    string FormatId { get; }
    string DisplayName { get; }
    string Description { get; }
    IArchiveFormatDetector CreateDetector();
    IArchiveParser CreateParser();
}

// アーカイブ形式検出
public interface IArchiveFormatDetector
{
    Task<FormatDetectionResult> DetectAsync(IArchiveSource source, CancellationToken ct);
}

// アーカイブパーサ
public interface IArchiveParser
{
    Task<ChatArchive> ParseAsync(
        IArchiveSource source,
        IProgress<ArchiveLoadProgress>? progress,
        CancellationToken ct);

    Task<IReadOnlyList<ChatMessage>> LoadMessagesAsync(
        IArchiveSource source,
        string conversationId,
        DateOnly? date,
        CancellationToken ct);
}

// 形式レジストリ
public interface IArchiveFormatRegistry
{
    IReadOnlyList<IArchiveFormatProvider> GetAllProviders();
    IArchiveFormatProvider? GetProvider(string formatId);
    Task<IReadOnlyList<FormatDetectionResult>> DetectAllAsync(
        IArchiveSource source, CancellationToken ct);
}
```

### 汎用チャットモデル

```csharp
// アーカイブ全体
public class ChatArchive
{
    public required string FormatId { get; init; }
    public required string FormatDisplayName { get; init; }
    public required ArchiveMetadata Metadata { get; init; }
    public required IReadOnlyList<Conversation> Conversations { get; init; }
    public required IReadOnlyList<Participant> Participants { get; init; }
    public required IReadOnlyList<LoadDiagnostic> Diagnostics { get; init; }
}

public class ArchiveMetadata
{
    public string? DisplayName { get; init; }
    public DateTimeOffset? ExportedAt { get; init; }
    public DateOnly? EarliestDate { get; init; }
    public DateOnly? LatestDate { get; init; }
    public int TotalMessageCount { get; init; }
    public IReadOnlyDictionary<string, string> ExtendedProperties { get; init; }
        = new Dictionary<string, string>();
}

// チャンネル・会話グループ
public class Conversation
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Topic { get; init; }
    public string? Purpose { get; init; }
    public ConversationType Type { get; init; } = ConversationType.Channel;
    public IReadOnlyList<DateOnly> AvailableDates { get; init; } = [];
    public int MessageCount { get; init; }
}

public enum ConversationType { Channel, DirectMessage, Group, Other }

// 参加者
public class Participant
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? RealName { get; init; }
}

// メッセージ
public class ChatMessage
{
    public required string Id { get; init; }
    public required string ConversationId { get; init; }
    public string? ParticipantId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Text { get; init; }
    public string? RawSubtype { get; init; }
    public string? ThreadId { get; init; }
    public bool IsThreadParent { get; init; }
    public int ReplyCount { get; init; }
    public bool IsEdited { get; init; }
    public DateTimeOffset? EditedAt { get; init; }
    public MessageType Type { get; init; } = MessageType.Normal;
    public IReadOnlyList<MessageAttachment> Attachments { get; init; } = [];
    public IReadOnlyList<MessageReaction> Reactions { get; init; } = [];
    public IReadOnlyDictionary<string, string> ExtendedProperties { get; init; }
        = new Dictionary<string, string>();
}

public enum MessageType { Normal, System, Unknown }

public class MessageAttachment
{
    public string? Name { get; init; }
    public string? Title { get; init; }
    public string? Url { get; init; }
}

public class MessageReaction
{
    public required string Name { get; init; }
    public int Count { get; init; }
}

// 読み込み診断
public class LoadDiagnostic
{
    public required DiagnosticSeverity Severity { get; init; }
    public required string Message { get; init; }
    public string? SourceHint { get; init; }
}

public enum DiagnosticSeverity { Information, Warning, Error }

// 読み込み進捗
public class ArchiveLoadProgress
{
    public required string Phase { get; init; }
    public int? Current { get; init; }
    public int? Total { get; init; }
}

// 形式検出結果
public class FormatDetectionResult
{
    public required string FormatId { get; init; }
    public required string FormatDisplayName { get; init; }
    public required bool IsDetected { get; init; }
    public double Confidence { get; init; }
}
```

### Slack 形式実装方針

- `SlackFormatProvider : IArchiveFormatProvider` — 形式メタデータと Detector/Parser のファクトリ
- `SlackFormatDetector : IArchiveFormatDetector` — `channels.json` または `users.json` の存在と構造で判定
- `SlackArchiveParser : IArchiveParser` — Slack JSON を読み込み、汎用モデルへ変換
- Slack 固有のデータモデル（`SlackChannel`, `SlackMessage`, `SlackUser`）は `SlackModels/` に定義し、パーサ内でのみ使用

#### 初版で対応する Slack エクスポート種別

- **対応対象**: Slack の **JSON 形式エクスポート一式**（`channels.json` / `users.json` / チャンネル別ディレクトリ / 日次 JSON）
- **非対応**: TXT 形式エクスポートの本文表示、Slack への再接続、Slack 上の URL を使った追加取得
- **理由**: 本アプリは完全ローカル・外部通信なしを厳守するため、JSON バンドルに含まれるローカルデータのみで閲覧を完結させる

#### 参照した情報源

- Slack 公式ヘルプ「Slack からエクスポートしたデータの読み方」
- リポジトリ内の汎用化サンプル `Sample Slack export`
- ユーザー指定の実データ（**構造観察のみ**。本文・チャンネル名・人物名などの実データは Plan に転記しない）

#### 観測されたルート構成と v1 対応方針

| 種別 | 典型パス / 名称 | v1 扱い | 用途 / 方針 |
|---|---|---|---|
| 必須候補 | `channels.json` | **一次読込対象** | 会話一覧、会話 ID、名前、topic、purpose、一般チャンネル判定の取得 |
| 必須候補 | `users.json` | **一次読込対象** | 参加者一覧、表示名、real name、bot 判定補助の取得 |
| 必須候補 | `<channel-name>\YYYY-MM-DD.json` | **一次読込対象** | 日次メッセージ本体。1 ファイル = 1 会話の 1 日分 |
| 補助 | `canvases.json` | **インベントリのみ** | Canvas 情報を持つが、v1 では会話本文に展開しない。存在件数のみ診断 / 概要へ反映 |
| 補助 | `file_conversations.json` | **インベントリのみ** | Canvas コメントや file conversation 参照用。v1 では独立会話化しない |
| 補助 | `huddle_transcripts.json` | **インベントリのみ** | 将来拡張候補。v1 では未対応要素として件数把握のみ |
| 補助 | `integration_logs.json` | **インベントリのみ** | 将来拡張候補。v1 では本文会話として表示しない |
| 補助 | `lists.json` | **インベントリのみ** | 将来拡張候補。v1 では本文会話として表示しない |
| 高度エクスポート | `content_flags\*` | **無視 + 診断** | 公式ヘルプにある高度エクスポート要素。v1 では対象外だが、安全にスキップする |
| 高度エクスポート | `FC:*` 形式のフォルダ | **無視 + 診断** | Canvas コメント系補助フォルダ。通常チャンネルと混同せず別扱いにする |

> 重要: `url_private_download` やファイル URL のような Slack 上のリンクは **表示用データとして保持するだけ** とし、パース中・表示中に自動アクセスしない。
> 重要: 会話ディレクトリ名は ASCII 前提にしない。v1 ではディレクトリ名を **不透明なローカル識別子** として扱い、表示名は `channels.json.name` を優先する。

#### Slack 形式の検出ルール

`SlackFormatDetector` は以下を総合して `Confidence` を返す:

1. ルートに `channels.json` または `users.json` が存在する
2. ルート配下にディレクトリが存在し、その中に `YYYY-MM-DD.json` 形式の日次ファイルが 1 件以上ある
3. `channels.json` / `users.json` の先頭要素が Slack らしいキー（例: `id`, `name`, `topic`, `profile`）を持つ
4. 補助ファイル（`canvases.json` 等）が存在しても、一次判定は上記 1〜3 を優先する

判定の考え方:

- `channels.json` + 日次 JSON あり → 高信頼
- `users.json` + 日次 JSON あり → 中信頼
- `channels.json` / `users.json` のどちらかのみ → 部分検出
- ルート JSON はあるが日次 JSON がない → Slack 断片データとして低信頼

#### 読込フェーズ設計

`SlackArchiveParser.ParseAsync()` は次の順で処理する:

1. **Root Inventory**  
   ルート直下の JSON / ディレクトリを列挙し、一次読込対象・補助アーティファクト・未知ファイルを分類する
2. **User Catalog**  
   `users.json` を読み、`Participant` 辞書を構築する
3. **Conversation Catalog**  
   `channels.json` を読み、`Conversation` 辞書を構築する
4. **Directory Reconciliation**  
   実在するディレクトリと `channels.json` 上の会話定義を突合し、欠損・余剰を診断する
5. **Date Index Build**  
   各会話ディレクトリから `YYYY-MM-DD.json` のみを収集し、`AvailableDates` と `MessageCount` を集計する
6. **Archive Summary Build**  
   `EarliestDate` / `LatestDate` / `TotalMessageCount` / 補助アーティファクト件数を確定する
7. **Lazy Day Load**  
   実メッセージ本文は `LoadMessagesAsync(conversationId, date)` で日単位に遅延読込する

補足:

- 日付ファイルが存在しない日は「その日にメッセージがない」状態として扱い、欠損エラーにしない
- 会話ディレクトリ名と `channels.json.name` が完全一致しない場合でも、まずディレクトリ実在を優先して索引を構築し、表示名はメタデータで補正する

#### 日次メッセージ JSON の読み方

Slack の日次 JSON は「メッセージ配列」であり、配列順は会話の時系列順として扱う。初版のマッピング方針は次のとおり:

| Slack キー | v1 の扱い | 汎用モデルへの反映 |
|---|---|---|
| `ts` | 必須。Slack 独自の秒.マイクロ秒文字列 | `ChatMessage.Id` の基礎、`Timestamp` へ変換 |
| `user` | 発言者 ID | `ParticipantId` に反映。`users.json` と突合 |
| `user_profile` | メッセージ時点のプロフィール断片 | `users.json` 不足時の表示名フォールバックに使用 |
| `text` | プレーンテキスト本文 | `ChatMessage.Text` の基本値 |
| `blocks` | リッチテキスト構造 | v1 では再レンダリングしない。将来拡張用に存在を保持し、必要時のみ `ExtendedProperties` へ記録 |
| `subtype` | システムイベントや編集/削除種別 | `RawSubtype` と `MessageType` 判定に使用 |
| `thread_ts` | スレッド親 ID | `ThreadId` に反映 |
| `reply_count`, `replies` | スレッド集約情報 | `IsThreadParent`, `ReplyCount` に反映 |
| `reactions` | リアクション配列 | `MessageReaction` に変換 |
| `files` | 共有ファイル参照 | `MessageAttachment` に変換。URL は保持のみで未取得 |
| `edited` | 編集情報 | `IsEdited`, `EditedAt` に反映 |
| `previous`, `original_ts` | 編集 / 削除前情報 | v1 では削除・編集イベントの補助メタデータとして `ExtendedProperties` に保持 |
| `bot_id`, `display_as_bot` | bot / app 投稿の手掛かり | `ParticipantId` 不在時の種別判定補助 |

#### 表示名解決の優先順位

`Participant` の表示名は以下の順で決定する:

1. `users.json.profile.display_name`
2. `users.json.real_name`
3. `user_profile.display_name`
4. `user_profile.real_name`
5. `user`
6. `bot_id`
7. `"Unknown participant"`

#### subtype とイベントの扱い

| subtype | v1 の扱い |
|---|---|
| なし | 通常メッセージ |
| `channel_join`, `channel_leave`, `channel_topic`, `channel_purpose` など | `MessageType.System` として表示 |
| `message_changed` | 更新後の本文を主表示対象とし、編集済みフラグを立てる |
| `message_deleted` | 削除イベントとして保持。元本文は `ExtendedProperties` に退避し、UI では既定で「削除済みメッセージ」として扱う |
| その他未知 subtype | `MessageType.Unknown` とし、破棄せず残す |

#### v1 の対応境界

- **表示する**: 通常本文、日時、投稿者、スレッド関係、リアクション件数、共有ファイル参照、主要な system subtype
- **保持するが本文展開しない**: `blocks` の詳細、Canvas 本文、Huddle transcript、List 内容、Content flag 詳細
- **行わない**: `url_private_download` や file URL へのアクセス、Slack API 呼び出し、HTML / 外部コンテンツの自動取得
- **診断に出す**: 補助アーティファクト件数、未対応 subtype 数、破損 JSON 数、会話定義とディレクトリ不一致

### DI 登録パターン

```csharp
// App.xaml.cs での DI 構成
public partial class App : Application
{
    public IHost Host { get; }

    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Core サービス
                services.AddSingleton<IArchiveFormatRegistry, ArchiveFormatRegistry>();
                services.AddSingleton<IArchiveLoadService, ArchiveLoadService>();

                // 形式プロバイダ
                services.AddSingleton<IArchiveFormatProvider, SlackFormatProvider>();

                // ViewModel
                services.AddTransient<ArchiveOverviewViewModel>();
                services.AddTransient<ArchiveBrowseViewModel>();
                services.AddTransient<MessageListViewModel>();
                services.AddTransient<SearchViewModel>();
                services.AddTransient<AboutViewModel>();
                services.AddTransient<SettingsViewModel>();

                // ナビゲーション
                services.AddSingleton<INavigationService, NavigationService>();

                // ウインドウ
                services.AddTransient<MainWindow>();
            })
            .ConfigureLogging(logging =>
            {
                // Serilog をファイル出力で構成
                // ログ先: ApplicationData.Current.LocalFolder/Logs/
            })
            .Build();
    }
}
```

### テーマ対応

- WinUI 3 の `ElementTheme` に準拠し、ライト / ダーク / システム追従の 3 モードをサポートする
- デフォルトはシステム追従（`ElementTheme.Default`）
- ユーザーが手動切り替え可能な導線を設定画面（または About 画面内）に用意する
- 設定値はアプリローカル設定（`ApplicationData.Current.LocalSettings`）に保存し、次回起動時に復元する
- すべてのカスタムスタイル・テンプレートは WinUI 3 のテーマリソース（`ThemeResource`）を使用し、ハードコードされた色指定を避ける
- メッセージ表示領域のチャット風 UI もテーマ切り替えに追従すること

### 画面設計 — list/details ベースの閲覧 UI

#### 設計方針

このアプリは「情報検索ツール」ではなく「時系列に沿って読みやすく閲覧するビューア」である。
画面設計は全文検索的な UI ではなく、ログを読み進め、別の日・別チャンネルへ素早く移動しやすい構成とする。

ウィザード的な一方向の順次操作画面にはしない。
Web アプリ / PWA のように一覧から選択して内容を切り替えながら閲覧できる list/details 構成を採用する。

#### 情報構造

チャットログは以下の階層を持つ:

1. **チャンネル**（会話のまとまりを表す最上位カテゴリ）
2. **日付**（チャンネル内の年月日）
3. **メッセージ列**（その日のチャットログ本文）

ユーザーはまずチャンネルを選び、次に日付を選び、その結果としてコンテンツ領域で 1 日のチャットログを読む。

#### 画面領域の構成

画面は「選択ナビゲーション」と「選択結果の表示」に分かれる:

| 領域 | 役割 |
|---|---|
| **チャンネル選択領域** | ログの最上位カテゴリ（チャンネル）を選ぶリスト。アーカイブ内の全チャンネルを一覧表示する |
| **日付選択領域** | 選択中チャンネルに属する日付を選ぶリスト。年月のまとまりが分かる構成で、日単位で素早く選択可能 |
| **コンテンツ表示領域** | 選択された「1 チャンネル・1 日」のチャットログ本文を時系列で表示する。発言者・日時・本文をチャット風に並べる |
| **BreadcrumbBar** | 現在の選択位置（チャンネル > 年月日）を常に表示し、上位階層へ戻れる導線を提供 |

```
┌─────────────────────────────────────────────────────────────────┐
│ [BreadcrumbBar: Archive > #general > 2024-01-15]               │
├──────────┬──────────┬──────────────────────────────────────────┤
│ チャンネル │  日付     │  コンテンツ（メッセージ一覧）              │
│ 選択      │  選択     │                                          │
│           │           │  [Alice 09:00] おはようございます         │
│ #general  │ 2024-01 ▾ │  [Bob   09:05] おはようございます！       │
│ #random ◄ │   01/13   │  [Alice 09:10] 今日の件ですが...          │
│ #dev      │   01/14   │  [Carol 09:15] 了解です                  │
│ #design   │  >01/15<  │  ...                                     │
│           │   01/16   │                                          │
│           │ 2024-02 ▾ │                                          │
│           │   02/01   │                                          │
└──────────┴──────────┴──────────────────────────────────────────┘
```

#### 日付選択の設計

- 年月のまとまり（グループヘッダー）が分かるリスト形式を基本とする
- TreeView 的な階層展開ではなく、list ベースで扱いやすい構成を優先
- 年月グループは折りたたみ可能（セクションヘッダーとして機能）
- 日単位の項目をタップ/クリックで即座にコンテンツが切り替わる
- ログ件数が多い（例: 数年分・数百日分）場合でもスクロール・切り替えが重くなりにくいよう、仮想化リストを使用
- 各日付項目にはメッセージ件数などの補助情報を表示可能とする

#### アダプティブ構成

画面幅に応じて同じ情報設計を保ったまま見せ方を変える:

**広い画面（≧ 900px 程度）:**
- チャンネル選択・日付選択・コンテンツ表示を 3 カラムで同時表示
- 一覧とコンテンツを行き来しながら読む体験を重視
- 選択状態が常に視認可能
- チャンネルや日付を変えるたびにページ遷移せず、コンテンツ領域のみ更新

**中程度の画面（600〜900px 程度）:**
- チャンネル選択を折りたたみ可能なペインに変更、または日付選択とコンテンツの 2 カラム構成
- チャンネル選択は NavigationView のペインのように開閉可能

**狭い画面（＜ 600px 程度）:**
- 一度に 1 つの領域を中心に表示するスタック型レイアウト
- チャンネル一覧 → 日付一覧 → コンテンツの順に drill-down し、戻り操作で上位へ戻る
- BreadcrumbBar で現在位置と戻り先が常に分かる
- 「別アプリの別画面」ではなく、同じ閲覧体験を画面幅に応じて最適化する

> **補足**: VisualStateManager の AdaptiveTrigger で画面幅に応じた VisualState を切り替え、同一ページ内でレイアウトをアダプティブに変更する設計を基本とする。狭い画面時の drill-down は内部状態の切り替えであり、重いページ遷移の積み重ねは避ける。

#### BreadcrumbBar

- 現在の選択位置を常に表示: 「アーカイブ名 > チャンネル名 > 年月日」
- 各セグメントをクリックして上位階層へ直接戻れる
- 狭い画面時に折り返しやオーバーフローが起きにくいよう、文言を省略可能にする
- アーカイブ未選択時は非表示または「アーカイブを開いてください」を表示

#### ナビゲーション方針

- 項目を選ぶたびに重いページ遷移を積み重ねない
- 閲覧コンテキストを保ったまま、別チャンネル・別日へ軽快に切り替える
- Back 操作が必要な場合、ユーザーが今どの階層にいて何に戻るのかが BreadcrumbBar で分かる
- 「今どのチャンネルの、どの日を見ているか」が常に分かること
- 直前に見ていた文脈（スクロール位置等）を見失いにくいこと
- 検索中心ではなく、読む・たどる・切り替えるのしやすさを優先

#### ページ遷移フロー

list/details ベースの設計では、アーカイブ読み込み後のチャンネル→日付→メッセージの閲覧はページ遷移ではなく、同一画面内での選択状態の切り替えで行う。「ページ遷移」が発生するのは以下の場面のみ:

1. アプリ起動 → `WelcomePage`（「アーカイブを開く」ボタン表示）
2. アーカイブを開く → 読み込み → `ArchiveBrowsePage`（list/details 閲覧画面）
3. NavigationView フッターから About / Settings への遷移

```
MainWindow
├── [NavigationView]
│   ├── [コンテンツ]
│   │   ├── WelcomePage            ← 初期状態: アーカイブ未読み込み
│   │   ├── ArchiveBrowsePage      ← list/details 閲覧画面（メイン）
│   │   │   ├── BreadcrumbBar
│   │   │   ├── チャンネル選択ペイン
│   │   │   ├── 日付選択ペイン
│   │   │   └── コンテンツ表示領域
│   │   └── SearchPage             ← キーワード検索
│   ├── [フッター]
│   │   ├── About                  ← About 画面
│   │   └── Settings               ← テーマ切り替え等
```

#### ウィザード型より list/details が適している理由

| 観点 | ウィザード型 | list/details 型（採用） |
|---|---|---|
| 閲覧の自由度 | 一方向。戻って別選択は手間 | 任意の一覧項目を即座に選択可能 |
| 日・チャンネル切り替え | ステップを戻る必要がある | 一覧をクリックするだけ |
| 文脈の保持 | ステップ移動で失われやすい | 選択状態として常に可視 |
| 大量データ対応 | 各ステップの一覧が肥大化 | 仮想化リスト + グループ化で対応 |
| アダプティブ対応 | ステップの並び替えが困難 | カラム構成の変更で自然に対応 |

#### パフォーマンス設計要件

- チャンネル一覧・日付一覧は仮想化リスト（`ItemsRepeater` + `ScrollView` または `ListView` の仮想化）で表示し、大量項目でもスクロールが軽快
- メッセージ表示領域も仮想化を適用し、1 日数千件のメッセージでも描画負荷を抑制
- 日付選択時のメッセージ読み込みは非同期で行い、読み込み中はプレースホルダーまたはプログレス表示
- チャンネル・日付の切り替え時に前回の読み込み結果をキャッシュし、再選択時の再読み込みを回避（将来拡張）

#### 検索画面との関係

キーワード検索は list/details 閲覧画面とは別の `SearchPage` として提供する。検索結果から特定メッセージを選択した場合は、`ArchiveBrowsePage` の該当チャンネル・日付・メッセージ位置へ遷移する導線を将来的に提供可能とする。初版では検索結果の表示のみでもよい。

### ログ戦略

- **出力先**: MSIX パッケージのローカルデータフォルダ (`ApplicationData.Current.LocalFolder/Logs/`)
- **ローテーション**: 日単位、最大 10 ファイル保持
- **レベル**: Debug 以上（Release ビルドでは Information 以上）
- **フォーマット**: `[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}`
- **禁止事項**: ユーザーのチャットログデータ（本文・発言者名・チャンネル名等）をログに出力しない。出力してよいのはファイル数・メッセージ数・チャンネル数等のメタデータのみ
- **ユーザーアクセス**: About 画面から「ログフォルダを開く」機能を提供

### エラーハンドリング戦略

- 原則としてフォールバックは行わず、例外を返す（copilot-instructions.md 準拠）
- ただし、アーカイブ読み込み中の個別ファイル破損については、`LoadDiagnostic` に記録して読み込みを継続する（これは「フォールバック」ではなく「部分読み込み」設計）
- すべての例外は `ILogger` で `Exception.ToString()` をトレースログに出力
- UI スレッドで未処理例外が発生した場合は、ユーザーに通知ダイアログを表示
- ZIP スリップ対策: 展開先パスがルートディレクトリ外に出ないことを検証

### ZIP 安全性

- `ZipArchiveSource` は ZIP をストリーム処理するか、一時ディレクトリに安全に展開
- 展開時に各エントリのフルパスを検証し、`..` を含むパスやルート外への展開を拒否
- 一時ディレクトリは `IAsyncDisposable.DisposeAsync()` でクリーンアップ
- 一時ディレクトリは OS の temp フォルダ内に作成

---

## Coarse interaction scenarios

主要シナリオの詳細は [Runtime Evidence](./v1-initial-release-runtime-evidence.md) を参照。

### シナリオ概要

| ID | シナリオ | 分類 |
|---|---|---|
| S-010 | アプリ起動（初期状態） | 正常系 |
| S-020 | ZIP アーカイブを開いて閲覧 | 正常系 |
| S-030 | フォルダアーカイブを開いて閲覧 | 正常系 |
| S-040 | チャンネル選択と日付選択（list/details） | 正常系 |
| S-050 | メッセージ一覧のチャット風表示 | 正常系 |
| S-055 | アダプティブレイアウト切り替え | 正常系 |
| S-060 | キーワード検索 | 正常系 |
| S-070 | About 画面表示 | 正常系 |
| S-080 | プライバシーポリシー URL を開く | 正常系 |
| S-085 | テーマ設定の変更 | 正常系 |
| S-110 | 破損ファイルを含むアーカイブの読み込み | 異常系 |
| S-120 | 読み込みキャンセル | 代替系 |
| S-130 | 未対応形式のアーカイブを開く | 異常系 |
| S-140 | 空チャンネル・空日付の取り扱い | 境界系 |

---

## Impacted code / files / modules

すべて新規作成。以下にファイル一覧を示す。

### ソリューション・プロジェクトファイル

| ファイル | 説明 |
|---|---|
| `ChatArchiveViewer.sln` | ソリューションファイル |
| `src/ChatArchiveViewer.App/ChatArchiveViewer.App.csproj` | WinUI 3 MSIX アプリ |
| `src/ChatArchiveViewer.Core/ChatArchiveViewer.Core.csproj` | Core ライブラリ |
| `src/ChatArchiveViewer.Formats.Slack/ChatArchiveViewer.Formats.Slack.csproj` | Slack 形式 |
| `tests/ChatArchiveViewer.Core.Tests/ChatArchiveViewer.Core.Tests.csproj` | Core テスト |
| `tests/ChatArchiveViewer.Formats.Slack.Tests/ChatArchiveViewer.Formats.Slack.Tests.csproj` | Slack テスト |

### Core プロジェクト

| ファイル | 説明 |
|---|---|
| `Core/Models/ChatArchive.cs` | アーカイブ全体モデル |
| `Core/Models/ArchiveMetadata.cs` | メタデータ |
| `Core/Models/Conversation.cs` | 会話・チャンネル |
| `Core/Models/Participant.cs` | 参加者 |
| `Core/Models/ChatMessage.cs` | メッセージ |
| `Core/Models/MessageAttachment.cs` | 添付 |
| `Core/Models/MessageReaction.cs` | リアクション |
| `Core/Models/LoadDiagnostic.cs` | 読み込み診断 |
| `Core/Models/ArchiveLoadProgress.cs` | 読み込み進捗 |
| `Core/Models/FormatDetectionResult.cs` | 形式検出結果 |
| `Core/Abstractions/IArchiveSource.cs` | 入力ソース抽象 |
| `Core/Abstractions/IArchiveFormatProvider.cs` | 形式プロバイダ抽象 |
| `Core/Abstractions/IArchiveFormatDetector.cs` | 形式検出抽象 |
| `Core/Abstractions/IArchiveParser.cs` | パーサ抽象 |
| `Core/Abstractions/IArchiveFormatRegistry.cs` | レジストリ抽象 |
| `Core/Abstractions/IArchiveLoadService.cs` | 読み込みサービス抽象 |
| `Core/Services/ArchiveFormatRegistry.cs` | レジストリ実装 |
| `Core/Services/ArchiveLoadService.cs` | 読み込みオーケストレーション |
| `Core/Services/ZipArchiveSource.cs` | ZIP 入力ソース |
| `Core/Services/FolderArchiveSource.cs` | フォルダ入力ソース |
| `Core/Services/SearchService.cs` | 検索サービス |

### Slack 形式プロジェクト

| ファイル | 説明 |
|---|---|
| `Formats.Slack/SlackFormatProvider.cs` | Slack プロバイダ |
| `Formats.Slack/SlackFormatDetector.cs` | Slack 形式検出 |
| `Formats.Slack/SlackArchiveParser.cs` | Slack パーサ |
| `Formats.Slack/SlackModels/SlackChannel.cs` | Slack チャンネル JSON モデル |
| `Formats.Slack/SlackModels/SlackMessage.cs` | Slack メッセージ JSON モデル |
| `Formats.Slack/SlackModels/SlackUser.cs` | Slack ユーザー JSON モデル |
| `Formats.Slack/SlackToGenericMapper.cs` | Slack→汎用モデル変換 |

### App プロジェクト

| ファイル | 説明 |
|---|---|
| `App/App.xaml` / `App.xaml.cs` | アプリエントリポイント・DI 構成・テーマ初期化 |
| `App/MainWindow.xaml` / `.cs` | メインウインドウ・NavigationView |
| `App/Views/WelcomePage.xaml` / `.cs` | 初期画面 |
| `App/Views/ArchiveBrowsePage.xaml` / `.cs` | list/details 閲覧メイン画面 |
| `App/Views/SearchPage.xaml` / `.cs` | 検索 |
| `App/Views/AboutPage.xaml` / `.cs` | About |
| `App/Views/SettingsPage.xaml` / `.cs` | 設定（テーマ切り替え等） |
| `App/ViewModels/ArchiveOverviewViewModel.cs` | 概要情報 VM |
| `App/ViewModels/ArchiveBrowseViewModel.cs` | list/details 閲覧 VM（チャンネル・日付・メッセージ統合） |
| `App/ViewModels/MessageListViewModel.cs` | メッセージ VM |
| `App/ViewModels/SearchViewModel.cs` | 検索 VM |
| `App/ViewModels/AboutViewModel.cs` | About VM |
| `App/ViewModels/SettingsViewModel.cs` | 設定 VM（テーマ等） |
| `App/Services/NavigationService.cs` | ナビゲーション |
| `App/Services/INavigationService.cs` | ナビゲーション抽象 |
| `App/Services/DialogService.cs` | ダイアログ表示 |
| `App/Services/IDialogService.cs` | ダイアログ抽象 |
| `App/Services/IFilePicker.cs` | ファイルピッカー抽象 |
| `App/Services/FilePicker.cs` | ファイルピッカー実装 |
| `App/Helpers/AppInfo.cs` | バージョン等の静的情報 |
| `App/Package.appxmanifest` | MSIX マニフェスト |
| `App/Assets/` | アイコン等 |
| `App/Strings/en-us/Resources.resw` | 英語リソース |
| `App/Strings/ja-jp/Resources.resw` | 日本語リソース |

### テストプロジェクト

| ファイル | 説明 |
|---|---|
| `Core.Tests/Services/ArchiveFormatRegistryTests.cs` | レジストリテスト |
| `Core.Tests/Services/ZipArchiveSourceTests.cs` | ZIP ソーステスト |
| `Core.Tests/Services/FolderArchiveSourceTests.cs` | フォルダソーステスト |
| `Core.Tests/Services/SearchServiceTests.cs` | 検索テスト |
| `Core.Tests/Services/ArchiveLoadServiceTests.cs` | 読み込みサービステスト |
| `Slack.Tests/SlackFormatDetectorTests.cs` | Slack 検出テスト |
| `Slack.Tests/SlackArchiveParserTests.cs` | Slack パーサテスト |
| `Slack.Tests/SlackToGenericMapperTests.cs` | 変換テスト |
| `tests/TestData/` | テスト用サンプルデータ |

---

## Verification design

詳細は [Integration Test Points](./v1-initial-release-integration-test-points.md) を参照（19 観点、4 分類: 正常系 9 / 異常系 7 / 負荷系 1 / 連続系 2）。

テスト観点は全 30 入力パターン（P-SRC-01〜18, P-ZIP-01〜04, P-KW-01〜05, P-DT-01〜03）を網羅し、Plan のトレーサビリティマトリックス全要件をカバー済み。

特筆事項: TP-170（ログにチャットデータが含まれない検証）は `ILogger` キャプチャによる自動テスト観点として設計。Plan 段階では Code review のみだった箇所を自動化。

### テスト戦略概要

| レイヤー | テスト種別 | 対象 |
|---|---|---|
| Core Models | Unit | モデルの生成・バリデーション |
| Core Services | Unit | FormatRegistry, ZipArchiveSource, FolderArchiveSource, SearchService, ArchiveLoadService |
| Slack Format | Unit | FormatDetector, ArchiveParser, Mapper |
| ViewModel | Unit | 各 ViewModel のコマンド・状態遷移 |
| App UI | Manual | ナビゲーション、表示、About 画面 |

### 主要テスト観点

| 観点 ID | 観点 | 対象コンポーネント |
|---|---|---|
| T-010 | Slack 形式ディレクトリ判定（channels.json の有無） | M-SlackFmt |
| T-020 | ZIP / フォルダ入力の両対応 | M-ZipSrc, M-FolderSrc |
| T-030 | 日付単位のメッセージ抽出 | M-SlackFmt, M-DateFilter |
| T-040 | 破損 JSON 混入時の継続読み込み | M-SlackFmt |
| T-050 | キーワード検索結果の妥当性 | M-Search |
| T-060 | 空チャンネル・空日付ファイルの扱い | M-SlackFmt |
| T-070 | About 情報の生成（非公式表記含む） | M-AboutVM |
| T-080 | プライバシーポリシー URL 設定 | M-AboutVM |
| T-090 | ZIP スリップ防御 | M-ZipSrc |
| T-100 | 読み込みキャンセル | M-LoadSvc |
| T-110 | 未対応形式の検出拒否 | M-FormatReg |
| T-120 | 大量メッセージの読み込み | M-SlackFmt |
| T-130 | メタデータ不足時の継続 | M-SlackFmt |
| T-140 | 非公式表記が About に含まれること | M-AboutVM |
| T-150 | Slack 補助アーティファクトの安全な識別 | M-SlackFmt |
| T-160 | 編集・削除メッセージの読み取り | M-SlackFmt |
| T-170 | Slack URL を自動取得しないこと | M-SlackFmt |
| T-180 | ログにチャットデータを含めないこと | M-SlackFmt, M-Search, M-LoadSvc |
| T-190 | テーマ設定の適用と永続化 | M-SettingsVM, M-Shell |

---

## Traceability matrix

| 要件 / 振る舞い | シナリオ | Plan テスト観点 | Integration Test Point |
|---|---|---|---|
| ローカル ZIP を開ける | S-020 | T-020, T-090 | TP-050, TP-140 |
| ローカルフォルダを開ける | S-030 | T-020 | TP-060 |
| ログ形式を選択できる | S-020, S-030 | T-010, T-110 | TP-010, TP-040, TP-110 |
| Slack 形式に対応 | S-020, S-030 | T-010, T-030, T-040, T-060, T-130, T-150, T-160, T-170 | TP-010, TP-020, TP-030, TP-120, TP-130, TP-160 |
| チャンネル一覧表示 | S-040 | T-030 | TP-020, TP-080 + Manual |
| 日付で絞り込み | S-040 | T-030 | TP-080 + Manual |
| メッセージ本文と投稿情報を表示 | S-050 | T-030 | TP-030 + Manual |
| About 表示 | S-070 | T-070, T-140 | TP-090 |
| プライバシーポリシー導線 | S-080 | T-080 | TP-090 + Manual |
| 非公式・ローカル完結・外部送信なしの説明 | S-070 | T-070, T-140 | TP-090 |
| エラー耐性 | S-110, S-140 | T-040, T-060, T-130 | TP-120, TP-130, TP-160 |
| Slack 補助アーティファクトを本文会話と混同しない | S-020, S-030 | T-150 | TP-020, TP-130 |
| 編集・削除メッセージを読み取れる | S-050 | T-160 | TP-020 |
| Slack 上の URL を自動取得しない | S-020, S-030 | T-170 | TP-020, TP-130 |
| 将来の形式追加に耐えるアーキテクチャ | — | T-110 | TP-040 + Code review |
| キーワード検索 | S-060 | T-050 | TP-070 |
| 読み込みキャンセル | S-120 | T-100 | TP-310 |
| ZIP スリップ防御 | S-020 | T-090 | TP-140 |
| ログにチャットデータを含めない | — | T-180 | TP-170（ILogger キャプチャ自動テスト）|
| 大量データ耐性 | — | T-120 | TP-210 |
| アーカイブ連続切り替え | S-020, S-030 | — | TP-320 |
| ライト/ダークテーマ切り替え | S-085 | T-190 | Manual（テーマ 3 モードの視覚確認） |
| アダプティブレイアウト | S-055 | — | Manual（画面幅変更での 3→2→1 カラム遷移確認） |
| BreadcrumbBar による現在地表示 | S-040 | — | Manual（階層移動と戻り操作の確認） |

---

## Definition of Done

以下のすべてを満たしたとき完了とする:

1. **ビルド成功**: `dotnet build` がエラーなく完了する
2. **MSIX パッケージ生成**: Release 構成で MSIX パッケージが生成される
3. **ユニットテスト全件パス**: `dotnet test` ですべてのテストが成功する
4. **必須機能動作確認**:
   - Slack エクスポート ZIP とフォルダの両方を開ける
   - チャンネル一覧 → 日付選択 → メッセージ表示の list/details 操作が動作する
   - 広い画面での 3 カラム表示と狭い画面での drill-down が動作する
   - キーワード検索が動作する
   - About 画面に非公式説明・プライバシーポリシー導線が表示される
   - ライト / ダーク / システム追従テーマが正しく切り替わる
5. **エラー耐性確認**: 破損 JSON を含むアーカイブで、クラッシュせず部分読み込みが動作する
6. **ログ確認**: Serilog によるファイルログが出力され、チャットデータが含まれていないことを確認
7. **外部通信なし確認**: ネットワーク切断状態でアプリが正常動作する

---

## Risks / rollout / rollback

| リスク | 影響 | 緩和策 |
|---|---|---|
| .NET 10 SDK の WinUI 3 対応状況 | ビルド不可 | プレビュー SDK で早期検証。最悪 .NET 9 にフォールバック |
| 巨大 ZIP アーカイブ（数 GB） | メモリ不足・UI フリーズ | 遅延読み込み + キャンセル対応で緩和。初版は警告表示で対応 |
| Slack エクスポート形式の変更 | パース失敗 | 破損耐性設計により部分読み込み継続。診断メッセージでユーザーに通知 |
| Microsoft Store 審査での Slack 商標問題 | リジェクト | アプリ名・説明から Slack ブランドを排除。「サポート対象形式の一つ」として説明 |
| MSIX サンドボックスでのファイルアクセス制限 | ファイル読み込み失敗 | FileOpenPicker / FolderPicker 経由のブローカーアクセスを使用 |

---

## Open questions / assumptions

### Assumptions

| # | 仮定 |
|---|---|
| A-1 | .NET 10 SDK が WinUI 3 (Windows App SDK) で安定利用可能 |
| A-2 | テストでの NSubstitute 使用（リフレクション）は、プロダクションコードではないため許容 |
| A-3 | 初版の UI ローカライズは日本語・英語の 2 言語 |
| A-4 | アプリアイコンはプレースホルダーを使用し、正式版は別途デザイン |
| A-5 | プライバシーポリシー URL は設定可能なプレースホルダーとし、公開時に決定 |
| A-6 | Windows 10 バージョン 2004 (Build 19041) 以上を最低動作環境とする |

### Open Questions

| # | 質問 |
|---|---|


### Resolved Questions

| # | 質問と回答 |
|---|---|
| Q-1 | キーワード検索で空文字列は UI 上で検索実行不可とする（検索ボタン無効化）。これを Plan の仕様とする。 |

| # | 質問 | 回答 |
|---|---|---|
| Q-3 | テーマ（ライト/ダーク）対応は初版で必要か | **対応する**。ライト / ダーク / システム追従の 3 モードを初版でサポート |

### Future Considerations

| # | 項目 |
|---|---|
| F-1 | ログ出力先のユーザーアクセスについて、将来的に「ログフォルダを開く」以外のエクスポート機能を追加するか |
| F-2 | スレッド関係は v1 で保持・表示するが、専用スレッドビューや返信ペインを追加するか |
