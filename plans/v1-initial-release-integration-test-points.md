# v1 初版リリース — ブラックボックステスト観点

> 本ドキュメントは [v1-initial-release.md](./v1-initial-release.md) のブラックボックステスト観点を定義する。
> 各観点はモック使用の自動テスト（NUnit）と、モック不使用の手動テストの両方のインプットとして使用する。

---

## テスト対象 I/F

| # | インターフェース | 実装例 | 役割 |
|---|---|---|---|
| IF-1 | `IArchiveFormatDetector.DetectAsync` | `SlackFormatDetector` | 形式検出 |
| IF-2 | `IArchiveParser.ParseAsync` | `SlackArchiveParser` | アーカイブ構造パース |
| IF-3 | `IArchiveParser.LoadMessagesAsync` | `SlackArchiveParser` | メッセージ読み込み |
| IF-4 | `IArchiveFormatRegistry.DetectAllAsync` | `ArchiveFormatRegistry` | 全形式自動検出 |
| IF-5 | `IArchiveSource` | `ZipArchiveSource` | ZIP 入力ソース |
| IF-6 | `IArchiveSource` | `FolderArchiveSource` | フォルダ入力ソース |
| IF-7 | `SearchService.SearchAsync` | `SearchService` | キーワード検索 |
| IF-8 | `DateFilterService` | `DateFilterService` | 日付フィルタ |
| IF-9 | `AboutViewModel` | `AboutViewModel` | About 情報生成 |

## 外部依存（テスト時のスタブ対象）

| ID | 外部 | テスト時の置換 |
|---|---|---|
| X-Picker | WindowsFilePicker | `IFilePicker` スタブ（パスを直接返す） |
| X-FS | FileSystem | `IArchiveSource` のメモリ内テストデータ実装 |
| X-Browser | DefaultBrowser | URI 起動の抽象インターフェーススタブ |
| X-TempDir | TempDirectory | テスト用一時ディレクトリ（テスト終了時クリーンアップ） |

---

## 入力パラメータパターン

### IArchiveSource 内コンテンツ（Slack 形式）

| パターン | 説明 |
|---|---|
| P-SRC-01 | 標準構造: `channels.json` + `users.json` + チャンネルディレクトリ + `YYYY-MM-DD.json` |
| P-SRC-02 | `channels.json` のみ存在、`users.json` なし |
| P-SRC-03 | `users.json` のみ存在、`channels.json` なし |
| P-SRC-04 | 両方不在（空ディレクトリ / 非 Slack 構造） |
| P-SRC-05 | `channels.json` が JSON として不正（構文エラー） |
| P-SRC-06 | `channels.json` は有効 JSON だが Slack 構造ではない |
| P-SRC-07 | チャンネルディレクトリ内に日付ファイルなし（空チャンネル） |
| P-SRC-08 | 日付 JSON が破損（一部ファイルのみ） |
| P-SRC-09 | 日付 JSON が全ファイル破損 |
| P-SRC-10 | 正常ファイルと破損ファイルが混在 |
| P-SRC-11 | 大量チャンネル（100+）・大量日付ファイル |
| P-SRC-12 | スレッド親子・リアクション・添付を含むメッセージ |
| P-SRC-13 | System メッセージ・Unknown タイプ混在 |
| P-SRC-14 | DM チャンネル・グループチャンネル混在 |
| P-SRC-15 | `canvases.json` / `file_conversations.json` / `huddle_transcripts.json` / `integration_logs.json` / `lists.json` を含む |
| P-SRC-16 | `edited` / `previous` / `original_ts` / `subtype=message_changed|message_deleted` を含む |
| P-SRC-17 | `content_flags` や `FC:*` フォルダ等、v1 対象外の補助アーティファクトを含む |
| P-SRC-18 | 非 ASCII / Unicode を含むチャンネルディレクトリ名を含む |

### ZIP 固有パターン

| パターン | 説明 |
|---|---|
| P-ZIP-01 | 有効な ZIP（標準 Slack エクスポート） |
| P-ZIP-02 | `../` を含むエントリパス（zip slip） |
| P-ZIP-03 | 空の ZIP ファイル |
| P-ZIP-04 | 破損した ZIP（展開不可） |

### 検索キーワードパターン

| パターン | 説明 |
|---|---|
| P-KW-01 | 通常キーワード（マッチあり） |
| P-KW-02 | マッチなしキーワード |
| P-KW-03 | 大文字/小文字混在キーワード（case-insensitive 検証） |
| P-KW-04 | 空文字列 |
| P-KW-05 | 複数チャンネルにまたがるマッチ |

### 日付フィルタパターン

| パターン | 説明 |
|---|---|
| P-DT-01 | 会話が存在する日付を指定 |
| P-DT-02 | 会話が存在しない日付を指定 |
| P-DT-03 | 日付範囲の境界（最古日・最新日） |

---

## テスト観点

### 正常系（機能）

---

#### TP-010: Slack 形式の自動検出

**対象 I/F**: IF-1 (`IArchiveFormatDetector.DetectAsync`)
**関連シナリオ**: S-020, S-030
**Plan トレース**: T-010（Slack 形式ディレクトリ判定）

| # | 条件 | 期待 |
|---|---|---|
| a | P-SRC-01: `channels.json` + `users.json` 存在、構造正常 | `IsDetected=true`, `Confidence` が高い値 |
| b | P-SRC-02: `channels.json` のみ存在 | `IsDetected=true`（部分検出として成立） |
| c | P-SRC-03: `users.json` のみ存在 | `IsDetected=true`（部分検出として成立） |
| d | P-SRC-14: DM・グループ等の複数チャンネルタイプ存在 | `IsDetected=true`、チャンネルタイプに依存しない |
| e | `FormatDetectionResult.FormatId` / `FormatDisplayName` が非空 | Slack のフォーマット識別子と表示名が返される |
| f | P-SRC-15: 補助アーティファクトを含む | 補助ファイルの有無に影響されず Slack 形式として検出される |
| g | P-SRC-18: Unicode を含むディレクトリ名 | ディレクトリ名の文字種に依存せず検出できる |

---

#### TP-020: Slack アーカイブの構造パース

**対象 I/F**: IF-2 (`IArchiveParser.ParseAsync`)
**関連シナリオ**: S-020, S-030
**Plan トレース**: T-030（日付単位メッセージ抽出）, T-150（Slack 補助アーティファクトの安全な識別）, T-160（編集・削除メッセージの読み取り）, T-170（Slack URL を自動取得しないこと）

| # | 条件 | 期待 |
|---|---|---|
| a | P-SRC-01: 標準構造（複数チャンネル・複数日付） | `ChatArchive` が返り、`Conversations`, `Participants`, `Metadata` が正しくマッピングされる |
| b | `users.json` の各ユーザー | `Participant.Id`, `DisplayName`, `RealName` に変換される |
| c | `channels.json` の各チャンネル | `Conversation.Id`, `DisplayName`, `Topic`, `Purpose`, `Type` に変換される |
| d | 各チャンネルの日付ファイル群 | `Conversation.AvailableDates` に集約、`MessageCount` がカウントされる |
| e | P-SRC-12: スレッド親子関係 | `ChatMessage.ThreadId`, `IsThreadParent`, `ReplyCount` が正しく設定される |
| f | P-SRC-12: リアクション | `ChatMessage.Reactions` に名前とカウントがマッピングされる |
| g | P-SRC-12: 添付 | `ChatMessage.Attachments` に名前・タイトル・URL がマッピングされる |
| h | P-SRC-13: System/Unknown メッセージタイプ | `ChatMessage.Type` が `System` / `Unknown` に正しく設定される |
| i | P-SRC-14: DM・グループチャンネル | `Conversation.Type` が `DirectMessage` / `Group` に正しく設定される |
| j | `Metadata.EarliestDate` / `LatestDate` | 全メッセージの日付範囲が正しく算出される |
| k | `Metadata.TotalMessageCount` | 全チャンネル・全日付の合計メッセージ数が正しい |
| l | `IProgress<ArchiveLoadProgress>` | パース中に進捗が報告される（Phase, Current, Total） |
| m | `Diagnostics` | 正常パース時は Diagnostics が空または Information のみ |
| n | P-SRC-15: 補助アーティファクトを含む | 補助ファイルは本文会話として `Conversations` に混入しない |
| o | P-SRC-15: 補助アーティファクトを含む | 補助ファイルの存在件数が `Metadata.ExtendedProperties` または `Diagnostics` に集約される |
| p | P-SRC-16: `edited` を含む | 更新後本文が `Text` に入り、編集済み状態が保持される |
| q | P-SRC-16: `message_deleted` を含む | 削除イベントとして保持され、パーサがクラッシュしない |
| r | `files` セクションを含む | 添付 / ファイル参照が `Attachments` に変換されるが、ダウンロードは行われない |
| s | `user_profile` があり `users.json` が不足 | 表示名解決が `user_profile` フォールバックで成立する |
| t | `url_private_download` 等の URL を含む | URL は保持されても、パース時に外部アクセスしない |
| u | P-SRC-18: Unicode を含むディレクトリ名 | 会話ディレクトリを正しく列挙し、`AvailableDates` と `MessageCount` を集計できる |

---

#### TP-030: メッセージの日付指定読み込み

**対象 I/F**: IF-3 (`IArchiveParser.LoadMessagesAsync`)
**関連シナリオ**: S-050
**Plan トレース**: T-030（日付単位メッセージ抽出）

| # | 条件 | 期待 |
|---|---|---|
| a | 有効な `conversationId` + 有効な `date` | 該当チャンネル・該当日付のメッセージリストが返される |
| b | メッセージの `Timestamp` 順 | タイムスタンプ昇順でソートされている |
| c | 各メッセージの必須フィールド | `Id`, `ConversationId`, `Timestamp`, `Text` が設定されている |
| d | `ParticipantId` の参照 | `ChatArchive.Participants` のいずれかの `Id` と一致する |
| e | 有効な `conversationId` + `date=null` | 該当チャンネルの全日付のメッセージが返される（設計による） |
| f | 複数メッセージタイプの混在 | Normal, System, Unknown が混在しても全件返される |

---

#### TP-040: 形式レジストリによる全形式自動検出

**対象 I/F**: IF-4 (`IArchiveFormatRegistry.DetectAllAsync`)
**関連シナリオ**: S-020, S-030, S-130
**Plan トレース**: T-110（未対応形式の検出拒否）

| # | 条件 | 期待 |
|---|---|---|
| a | Slack プロバイダ登録済み + Slack ソース | 検出結果リストに `IsDetected=true` のエントリを含む |
| b | Slack プロバイダ登録済み + 非 Slack ソース | 検出結果リストの全エントリが `IsDetected=false` |
| c | プロバイダ未登録（空レジストリ） | 検出結果リストが空 |
| d | `GetAllProviders()` | 登録済み全プロバイダを返す |
| e | `GetProvider(formatId)` 存在する ID | 対応する `IArchiveFormatProvider` を返す |
| f | `GetProvider(formatId)` 存在しない ID | `null` を返す |

---

#### TP-050: ZIP 入力ソースのファイルアクセスとリソース管理

**対象 I/F**: IF-5 (`IArchiveSource` — `ZipArchiveSource`)
**関連シナリオ**: S-020
**Plan トレース**: T-020（ZIP/フォルダ入力両対応）

| # | 条件 | 期待 |
|---|---|---|
| a | P-ZIP-01: 有効な ZIP | `GetFilesAsync` でファイル一覧を取得できる |
| b | P-ZIP-01: 有効な ZIP | `OpenFileAsync` で各ファイルのストリームを取得できる |
| c | P-ZIP-01: 有効な ZIP | `FileExistsAsync` が存在ファイルに `true` を返す |
| d | P-ZIP-01: 有効な ZIP | `DirectoryExistsAsync` が存在ディレクトリに `true` を返す |
| e | P-ZIP-01: 有効な ZIP | `GetDirectoriesAsync` でディレクトリ一覧を取得できる |
| f | `FileExistsAsync` 存在しないパス | `false` を返す |
| g | `DisplayPath` | ZIP ファイルパスに基づく表示パスが設定されている |
| h | `DisposeAsync()` | 一時ディレクトリが削除される |

---

#### TP-060: フォルダ入力ソースのファイルアクセス

**対象 I/F**: IF-6 (`IArchiveSource` — `FolderArchiveSource`)
**関連シナリオ**: S-030
**Plan トレース**: T-020（ZIP/フォルダ入力両対応）

| # | 条件 | 期待 |
|---|---|---|
| a | 有効なフォルダパス | `GetFilesAsync` でファイル一覧を取得できる |
| b | 有効なフォルダパス | `OpenFileAsync` でストリームを取得できる |
| c | 有効なフォルダパス | `FileExistsAsync` が存在ファイルに `true` を返す |
| d | 有効なフォルダパス | `DirectoryExistsAsync` が存在ディレクトリに `true` を返す |
| e | 有効なフォルダパス | `GetDirectoriesAsync` でディレクトリ一覧を取得できる |
| f | `FileExistsAsync` 存在しないパス | `false` を返す |
| g | `DisplayPath` | フォルダパスに基づく表示パスが設定されている |
| h | `GetFilesAsync` のパターン指定（例: `*.json`） | パターンに合致するファイルのみ返される |

---

#### TP-070: キーワード検索の結果精度

**対象 I/F**: IF-7 (`SearchService.SearchAsync`)
**関連シナリオ**: S-060
**Plan トレース**: T-050（キーワード検索結果の妥当性）

| # | 条件 | 期待 |
|---|---|---|
| a | P-KW-01: マッチあり（単一チャンネル） | マッチするメッセージが検索結果に含まれる |
| b | P-KW-05: マッチあり（複数チャンネル横断） | 複数チャンネルの結果がチャンネル・日付とともに返される |
| c | P-KW-02: マッチなし | 空の検索結果リストが返される |
| d | P-KW-03: 大文字/小文字の違い | 大文字小文字を区別せずマッチする |
| e | 検索結果の各エントリ | メッセージ本文・チャンネル情報・日付が含まれる |
| f | 検索結果の順序 | 結果が一定の順序（チャンネル順・日時順等）で返される |

---

#### TP-080: 日付フィルタによる会話絞り込み

**対象 I/F**: IF-8 (`DateFilterService`)
**関連シナリオ**: S-040
**Plan トレース**: T-030（日付単位メッセージ抽出）

| # | 条件 | 期待 |
|---|---|---|
| a | P-DT-01: 指定日付に該当する会話が複数存在 | 該当会話のみがフィルタ結果に含まれる |
| b | P-DT-01: 指定日付に該当する会話が 1 件 | 1 件のみ返される |
| c | P-DT-02: 指定日付に該当する会話が 0 件 | 空リストが返される |
| d | P-DT-03: 最古日を指定 | 最古日にメッセージがある会話が返される |
| e | P-DT-03: 最新日を指定 | 最新日にメッセージがある会話が返される |
| f | フィルタ対象が空チャンネルを含む | 空チャンネル（`AvailableDates` が空）は日付指定時に除外される |

---

#### TP-090: About 情報の生成と必須表示項目

**対象 I/F**: IF-9 (`AboutViewModel`)
**関連シナリオ**: S-070, S-080
**Plan トレース**: T-070（About 情報の生成）, T-080（プライバシーポリシー URL）, T-140（非公式表記）

| # | 条件 | 期待 |
|---|---|---|
| a | AboutViewModel 初期化 | アプリ名が取得できる |
| b | AboutViewModel 初期化 | バージョン番号が取得できる |
| c | 非公式表記 | 「非公式」「公式ではない」等の表現が含まれる（Slack 公式・認定・提携を示唆しない） |
| d | 外部通信なし表記 | 「外部送信なし」「ローカル完結」等の表現が含まれる |
| e | 対応形式一覧 | 登録済みフォーマットプロバイダの表示名が一覧できる |
| f | プライバシーポリシー URL 設定済み | URL が取得でき、リンクが活性状態 |
| g | プライバシーポリシー URL 未設定 | リンクが非活性または非表示 |
| h | OSS リポジトリ URL | URL が取得できる |
| i | ライブラリ・ライセンス情報 | 使用ライブラリとそのライセンスの一覧が取得できる |

---

### 異常系

---

#### TP-110: 未対応形式アーカイブの検出拒否

**対象 I/F**: IF-1 (`IArchiveFormatDetector.DetectAsync`), IF-4 (`IArchiveFormatRegistry.DetectAllAsync`)
**関連シナリオ**: S-130
**Plan トレース**: T-110（未対応形式の検出拒否）

| # | 条件 | 期待 |
|---|---|---|
| a | P-SRC-04: Slack 関連ファイルが一切存在しない | `IsDetected=false` |
| b | P-SRC-06: `channels.json` は有効 JSON だが Slack の構造ではない | `IsDetected=false` |
| c | P-SRC-05: `channels.json` が JSON 構文エラー | `IsDetected=false`（例外ではなく検出失敗） |
| d | レジストリ経由（DetectAllAsync） | 全プロバイダで `IsDetected=false` の結果リストが返される |

---

#### TP-120: 破損 JSON 混入時の部分読み込み継続

**対象 I/F**: IF-2 (`IArchiveParser.ParseAsync`)
**関連シナリオ**: S-110
**Plan トレース**: T-040（破損 JSON 混入時の継続読み込み）

| # | 条件 | 期待 |
|---|---|---|
| a | P-SRC-10: 正常ファイルと破損日付 JSON の混在 | 正常ファイルのメッセージは `ChatArchive` に含まれる |
| b | P-SRC-10: 正常ファイルと破損日付 JSON の混在 | `Diagnostics` に `Warning` 以上の診断が記録される |
| c | P-SRC-10: 診断の `SourceHint` | 破損ファイルのパス（ファイル名）が含まれる |
| d | P-SRC-10: 診断の `Message` | チャットデータ本文を含まない（ログ安全性） |
| e | P-SRC-08: 特定チャンネルの一部日付のみ破損 | 同チャンネルの正常日付メッセージは読み込まれる |
| f | P-SRC-09: 全日付 JSON が破損 | `ChatArchive` は返るが `TotalMessageCount=0`、`Diagnostics` に全件記録 |
| g | 例外がスローされない | `ParseAsync` は例外ではなく `ChatArchive`（部分データ + Diagnostics）を返す |

---

#### TP-130: メタデータファイル欠損への耐性

**対象 I/F**: IF-2 (`IArchiveParser.ParseAsync`)
**関連シナリオ**: S-110
**Plan トレース**: T-130（メタデータ不足時の継続）, T-150（Slack 補助アーティファクトの安全な識別）, T-170（Slack URL を自動取得しないこと）

| # | 条件 | 期待 |
|---|---|---|
| a | P-SRC-02: `users.json` が存在しない | パースは継続。`Participants` が空リストまたは ID ベースのフォールバック |
| b | P-SRC-02: `users.json` 欠損時 | `Diagnostics` に欠損情報が記録される |
| c | P-SRC-03: `channels.json` が存在しない | パースは継続。ディレクトリ構造からチャンネルを推定するか、`Diagnostics` に記録 |
| d | `users.json` の一部ユーザーにフィールド欠損（`real_name` なし等） | 利用可能なフィールドでマッピング、欠損フィールドは `null` |
| e | P-SRC-15: 補助アーティファクトが存在しない | 欠損として扱わず、正常系として継続する |
| f | P-SRC-17: `content_flags` や `FC:*` フォルダが存在 | 本文会話に混入せず、安全に無視または診断記録される |

---

#### TP-140: ZIP 不正エントリ（スリップ攻撃）の防御

**対象 I/F**: IF-5 (`IArchiveSource` — `ZipArchiveSource`)
**関連シナリオ**: S-020
**Plan トレース**: T-090（ZIP スリップ防御）

| # | 条件 | 期待 |
|---|---|---|
| a | P-ZIP-02: `../` を含むエントリパス | 展開が拒否される（例外スロー） |
| b | P-ZIP-02: ルートディレクトリ外を指すエントリ | 展開が拒否される |
| c | P-ZIP-04: 破損した ZIP ファイル（展開不可） | 適切な例外がスローされる（クラッシュしない） |
| d | P-ZIP-03: 空の ZIP ファイル | IArchiveSource として生成可能だが中身は空 |

---

#### TP-150: 存在しないチャンネル・日付の読み込み要求

**対象 I/F**: IF-3 (`IArchiveParser.LoadMessagesAsync`)
**関連シナリオ**: S-050

| # | 条件 | 期待 |
|---|---|---|
| a | 存在しない `conversationId` を指定 | 空リストが返される（例外ではない） |
| b | 存在する `conversationId` + 存在しない `date` を指定 | 空リストが返される（例外ではない） |
| c | `conversationId` が空文字列 | 空リストまたは引数例外 |

---

#### TP-160: 空アーカイブ・空チャンネルの取り扱い

**対象 I/F**: IF-2 (`IArchiveParser.ParseAsync`), IF-3 (`IArchiveParser.LoadMessagesAsync`)
**関連シナリオ**: S-140
**Plan トレース**: T-060（空チャンネル・空日付の扱い）

| # | 条件 | 期待 |
|---|---|---|
| a | P-SRC-07: チャンネルディレクトリが空（日付ファイルなし） | `Conversation` は一覧に含まれる（`AvailableDates` 空、`MessageCount=0`） |
| b | 空チャンネルのメッセージ読み込み（LoadMessagesAsync） | 空リストが返される |
| c | 全チャンネルが空のアーカイブ | `ChatArchive` は返る（`TotalMessageCount=0`、`Conversations` は全件含む） |
| d | `channels.json` にチャンネル定義があるが対応ディレクトリなし | `Conversation` は一覧に含まれる。`AvailableDates` 空 |

---

#### TP-170: ログ出力にチャットデータが含まれないこと

**対象 I/F**: 横断（全コンポーネントの `ILogger` 出力）
**関連シナリオ**: 全シナリオ
**Plan トレース**: T-180（ログにチャットデータを含めないこと）

| # | 条件 | 期待 |
|---|---|---|
| a | 正常パース実行中のログ出力 | メッセージ本文・発言者名・チャンネル名が含まれない |
| b | 破損ファイル処理中のログ出力 | ファイルパスは出力可。チャットデータ内容は含まれない |
| c | 検索実行中のログ出力 | 検索キーワード・マッチ本文が含まれない |
| d | ログに出力されるメタデータ | ファイル数・メッセージ数・チャンネル数等の数値情報のみ |

> **検証方法の注記**: 自動テストでは `ILogger` のモック/スタブでログ出力内容をキャプチャし、チャットデータのパターンが含まれないことをアサートする。手動テストではログファイルの内容を目視確認する。

---

### 負荷系

---

#### TP-210: 大量チャンネル・大量メッセージの処理

**対象 I/F**: IF-2 (`IArchiveParser.ParseAsync`), IF-7 (`SearchService.SearchAsync`)
**関連シナリオ**: S-020, S-060
**Plan トレース**: T-120（大量メッセージの読み込み）

| # | 条件 | 期待 |
|---|---|---|
| a | P-SRC-11: 100 チャンネル以上のアーカイブ | `ParseAsync` が正常完了する |
| b | P-SRC-11: チャンネルあたり 365 日付ファイル（1年分） | `ParseAsync` が正常完了する |
| c | P-SRC-11: 大量メッセージのキーワード検索 | `SearchAsync` が正常完了し、結果が返される |
| d | 進捗報告 | 大量データでも `IProgress<ArchiveLoadProgress>` が定期的に呼ばれる |
| e | `CancellationToken` 応答性 | 大量データ処理中でもキャンセルが受け付けられる（TP-310 と連動） |

---

### 連続系

---

#### TP-310: 読み込み中キャンセルとリソース解放

**対象 I/F**: IF-2 (`IArchiveParser.ParseAsync`), IF-5 (`IArchiveSource`)
**関連シナリオ**: S-120
**Plan トレース**: T-100（読み込みキャンセル）

| # | 条件 | 期待 |
|---|---|---|
| a | `ParseAsync` 実行中に `CancellationToken` をキャンセル | `OperationCanceledException` がスローされる |
| b | キャンセル後の `IArchiveSource` リソース | `DisposeAsync` で一時ディレクトリが削除される |
| c | キャンセル後の後続操作 | 新たなアーカイブオープンが正常に実行できる（前回のリソースリーク無し） |
| d | `LoadMessagesAsync` 実行中のキャンセル | `OperationCanceledException` がスローされる |
| e | `SearchAsync` 実行中のキャンセル | `OperationCanceledException` がスローされる |

---

#### TP-320: アーカイブ連続切り替え（再読み込み）

**対象 I/F**: IF-5 (`IArchiveSource`), IF-2 (`IArchiveParser.ParseAsync`)
**関連シナリオ**: S-020, S-030

| # | 条件 | 期待 |
|---|---|---|
| a | ZIP アーカイブ読み込み完了後、別の ZIP を開く | 前の `ZipArchiveSource` が Dispose され、新しいアーカイブが正常に読み込まれる |
| b | ZIP アーカイブ読み込み完了後、フォルダを開く | ソースタイプの切り替えが正常に動作する |
| c | フォルダ読み込み完了後、ZIP を開く | ソースタイプの切り替えが正常に動作する |
| d | 連続 3 回以上のアーカイブ切り替え | リソースリークなく全回正常に動作する |

---

## テスト観点一覧（サマリ）

| ID | 観点 | 分類 | 対象 I/F | Plan トレース |
|---|---|---|---|---|
| TP-010 | Slack 形式の自動検出 | 正常 | IF-1 | T-010 |
| TP-020 | Slack アーカイブの構造パース | 正常 | IF-2 | T-030, T-150, T-160, T-170 |
| TP-030 | メッセージの日付指定読み込み | 正常 | IF-3 | T-030 |
| TP-040 | 形式レジストリの全形式自動検出 | 正常 | IF-4 | T-110 |
| TP-050 | ZIP 入力ソースのファイルアクセスとリソース管理 | 正常 | IF-5 | T-020 |
| TP-060 | フォルダ入力ソースのファイルアクセス | 正常 | IF-6 | T-020 |
| TP-070 | キーワード検索の結果精度 | 正常 | IF-7 | T-050 |
| TP-080 | 日付フィルタによる会話絞り込み | 正常 | IF-8 | T-030 |
| TP-090 | About 情報の生成と必須表示項目 | 正常 | IF-9 | T-070, T-080, T-140 |
| TP-110 | 未対応形式の検出拒否 | 異常 | IF-1, IF-4 | T-110 |
| TP-120 | 破損 JSON の部分読み込み継続 | 異常 | IF-2 | T-040 |
| TP-130 | メタデータファイル欠損への耐性 | 異常 | IF-2 | T-130, T-150, T-170 |
| TP-140 | ZIP 不正エントリ（スリップ攻撃）防御 | 異常 | IF-5 | T-090 |
| TP-150 | 存在しないチャンネル・日付の読み込み要求 | 異常 | IF-3 | — |
| TP-160 | 空アーカイブ・空チャンネルの取り扱い | 異常 | IF-2, IF-3 | T-060 |
| TP-170 | ログにチャットデータが含まれないこと | 異常 | 横断 | T-180 |
| TP-210 | 大量チャンネル・大量メッセージの処理 | 負荷 | IF-2, IF-7 | T-120 |
| TP-310 | 読み込み中キャンセルとリソース解放 | 連続 | IF-2, IF-5 | T-100 |
| TP-320 | アーカイブ連続切り替え（再読み込み） | 連続 | IF-2, IF-5 | — |

---

## Plan トレーサビリティマトリクス

Plan の要件 / 振る舞いが、本テスト観点でカバーされていることを確認する。

| Plan 要件 / 振る舞い | Plan シナリオ | Plan テスト ID | 本観点 ID |
|---|---|---|---|
| ローカル ZIP を開ける | S-020 | T-020, T-090 | TP-050, TP-140 |
| ローカルフォルダを開ける | S-030 | T-020 | TP-060 |
| ログ形式を選択できる | S-020, S-030 | T-010, T-110 | TP-010, TP-040, TP-110 |
| Slack 形式に対応 | S-020, S-030 | T-010, T-030, T-040, T-060, T-130, T-150, T-160, T-170 | TP-010, TP-020, TP-030, TP-120, TP-130, TP-160 |
| チャンネル一覧表示 | S-040 | T-030 | TP-020, TP-080 |
| 日付で絞り込み | S-040 | T-030 | TP-080 |
| メッセージ本文と投稿情報を表示 | S-050 | T-030 | TP-030 |
| About 表示 | S-070 | T-070, T-140 | TP-090 |
| プライバシーポリシー導線 | S-080 | T-080 | TP-090 |
| 非公式・ローカル完結・外部送信なしの説明 | S-070 | T-070, T-140 | TP-090 |
| エラー耐性 | S-110, S-140 | T-040, T-060, T-130 | TP-120, TP-130, TP-160 |
| Slack 補助アーティファクトを本文会話と混同しない | S-020, S-030 | T-150 | TP-020, TP-130 |
| 編集・削除メッセージを読み取れる | S-050 | T-160 | TP-020 |
| Slack 上の URL を自動取得しない | S-020, S-030 | T-170 | TP-020, TP-130 |
| 将来の形式追加に耐えるアーキテクチャ | — | T-110 | TP-040 |
| キーワード検索 | S-060 | T-050 | TP-070 |
| 読み込みキャンセル | S-120 | T-100 | TP-310 |
| ZIP スリップ防御 | S-020 | T-090 | TP-140 |
| ログにチャットデータを含めない | — | T-180 | TP-170 |
| 大量メッセージの読み込み | — | T-120 | TP-210 |

---

## ブラックボックスパターン網羅確認

### 入力パラメータパターンの観点カバレッジ

| パターン | カバー観点 |
|---|---|
| P-SRC-01 (標準構造) | TP-010a, TP-020a |
| P-SRC-02 (channels.json のみ) | TP-010b, TP-130a |
| P-SRC-03 (users.json のみ) | TP-010c, TP-130c |
| P-SRC-04 (両方不在) | TP-110a |
| P-SRC-05 (JSON 構文エラー) | TP-110c |
| P-SRC-06 (非 Slack 構造) | TP-110b |
| P-SRC-07 (空チャンネル) | TP-160a |
| P-SRC-08 (一部破損) | TP-120e |
| P-SRC-09 (全破損) | TP-120f |
| P-SRC-10 (混在) | TP-120a,b,c,d |
| P-SRC-11 (大量データ) | TP-210a,b,c |
| P-SRC-12 (スレッド・リアクション・添付) | TP-020e,f,g |
| P-SRC-13 (System/Unknown タイプ) | TP-020h |
| P-SRC-14 (DM・グループ) | TP-010d, TP-020i |
| P-SRC-15 (補助アーティファクト群) | TP-010f, TP-020n,o, TP-130e |
| P-SRC-16 (編集・削除メッセージ) | TP-020p,q |
| P-SRC-17 (対象外補助フォルダ) | TP-130f |
| P-SRC-18 (Unicode ディレクトリ名) | TP-010g, TP-020u |
| P-ZIP-01 (有効 ZIP) | TP-050a,b,c,d,e |
| P-ZIP-02 (zip slip) | TP-140a,b |
| P-ZIP-03 (空 ZIP) | TP-140d |
| P-ZIP-04 (破損 ZIP) | TP-140c |
| P-KW-01 (マッチあり) | TP-070a |
| P-KW-02 (マッチなし) | TP-070c |
| P-KW-03 (case-insensitive) | TP-070d |
| P-KW-04 (空文字列) | UI 上で検索実行不可とする。TP-070 は検索無効状態を検証 |
| P-KW-05 (複数チャンネル横断) | TP-070b |
| P-DT-01 (存在する日付) | TP-080a,b |
| P-DT-02 (存在しない日付) | TP-080c |
| P-DT-03 (境界日付) | TP-080d,e |
