# クラウド配信アーカイブ取得機能

## Background / Goal

特定の Slack workspace 用のチャットログアーカイブを Azure Blob Storage からクラウド配信し、画面上の「ログインして開く」ボタン押下時に最新 ZIP をローカル cache に取得して既存ビューアで表示する機能を追加する。

既存のローカル Slack ログ ZIP / 展開フォルダを読むビューア機能はそのまま活かし、クラウドから直接読むのではなく、**ユーザー操作で最新 ZIP をローカル cache に取得してから既存ビューアに渡す**構成にする。

### 関連ドキュメント

- Runtime Evidence: [cloud-archive-fetch-runtime-evidence.md](./cloud-archive-fetch-runtime-evidence.md)
- Integration Test Points: [cloud-archive-fetch-integration-test-points.md](./cloud-archive-fetch-integration-test-points.md)

---

## Non-goals

| # | 明示的な非目標 |
|---|---|
| NG-1 | 複数 workspace 対応 — 単一 workspace 専用 |
| NG-2 | 汎用プラグイン基盤 — この用途に絞った専用実装 |
| NG-3 | サーバー側の動的認可ロジック |
| NG-4 | 書き込みやアップロード機能 |
| NG-5 | ストア配布前提の実装 — Unpackaged desktop app |
| NG-6 | Azure Functions やサーバーサイド処理 — Blob 直接アクセス |
| NG-7 | キャッシュの複数世代管理（初版は最新 1 世代のみ） |
| NG-8 | Client Secret / Storage Key / 長寿命 SAS の使用 |
| NG-9 | 既存ローカルビューア機能の変更 |

---

## Current state summary

- 既存アプリは完全ローカル動作の WinUI 3 デスクトップアプリケーション（.NET 10 / Unpackaged 対応済み）
- Slack エクスポート形式の ZIP / フォルダを `IArchiveSource` → `IArchiveParser` で読み込み、汎用モデル `ChatArchive` に変換して表示
- サービス層は DI コンテナ（`Microsoft.Extensions.DependencyInjection`）で構成
- `ArchiveWorkflowService` が「ソース取得 → フォーマット検出 → ロード → セッション設定 → ViewModel 更新」の一連のフローをオーケストレーション
- `ZipArchiveSource` は ZIP を temp フォルダに展開し `FolderArchiveSource` に委譲する設計
- 外部通信は一切行っていない（v1 の NG-3 でクラウド連携を非目標にしていた）
- ログは `%LOCALAPPDATA%/ChatArchiveViewer/Logs/` に Serilog で書き出し

---

## Proposed design / architecture delta

### 新規プロジェクト

```
src/
  ChatArchiveViewer.CloudFetch/           ← クラウドアーカイブ取得サービス層（新規）
tests/
  ChatArchiveViewer.CloudFetch.Tests/     ← ユニットテスト（新規）
```

**`ChatArchiveViewer.CloudFetch`** は `ChatArchiveViewer.Core` を参照するが、`ChatArchiveViewer.App` や `ChatArchiveViewer.Formats.Slack` は参照しない。App 層から DI 経由で利用される。

### NuGet 依存（CloudFetch プロジェクト）

| パッケージ | 目的 |
|---|---|
| `Azure.Core` | `TokenCredential` 基底型と Azure SDK 認証連携 |
| `Azure.Storage.Blobs` | Blob Storage からの bootstrap.json / manifest.json / ZIP ダウンロード |
| `Microsoft.Identity.Client` | MSAL による対話的認証・トークンキャッシュ |
| `Microsoft.Extensions.Logging.Abstractions` | ロガーインターフェース |

`Microsoft.Identity.Client.Extensions.Msal` は、プロセスを跨いだトークンキャッシュ永続化を採用する場合のみ `ChatArchiveViewer.CloudFetch` 側に追加する。初版の必須依存ではない。

### プロジェクト参照関係

```
App → CloudFetch → Core
App → Formats.Slack → Core
App → Core
```

### レイヤ構成

```
ChatArchiveViewer.CloudFetch/
├── Models/
│   ├── BootstrapConfig.cs          ← bootstrap.json のデシリアライズモデル
│   ├── CloudManifest.cs            ← manifest.json のデシリアライズモデル
│   ├── CloudFetchResult.cs         ← 取得結果の状態（成功 / キャッシュ使用 / 失敗）
│   ├── CloudFetchProgress.cs       ← クラウド取得の進捗状態
│   └── CacheState.cs               ← cache-state.json のデシリアライズモデル
├── Abstractions/
│   ├── IBootstrapConfigProvider.cs ← bootstrap.json 取得抽象
│   ├── ICloudManifestProvider.cs   ← manifest.json 取得抽象
│   ├── ICloudArchiveDownloader.cs  ← ZIP ダウンロード抽象
│   ├── ICloudAuthService.cs        ← MSAL 認証抽象
│   ├── ICloudFetchOrchestrator.cs  ← クラウド取得オーケストレーション抽象
│   ├── ICacheManager.cs            ← ローカルキャッシュ管理抽象
│   └── IHashVerifier.cs            ← SHA-256 検証抽象
├── Services/
│   ├── BootstrapConfigProvider.cs
│   ├── CloudManifestProvider.cs
│   ├── CloudArchiveDownloader.cs
│   ├── MsalAuthService.cs
│   ├── MsalTokenCredential.cs      ← MSAL のアクセストークンを Blob SDK に渡すラッパー
│   ├── LocalCacheManager.cs
│   ├── Sha256Verifier.cs
│   └── CloudFetchOrchestrator.cs   ← メインオーケストレーション
└── CloudFetchConstants.cs          ← 固定 bootstrap URL 等
```

### Azure Storage 配置構成

```
Storage Account
├── $web (公開コンテナ / Static Website)
│   └── bootstrap.json              ← 匿名アクセス可能
└── archives (非公開コンテナ)
    ├── manifest.json               ← 認証必須
    └── slack-export-2026-04-01-v1.zip  ← 認証必須・immutable 名
```

### 認証フロー

- **方式**: MSAL (`Microsoft.Identity.Client`) の `IPublicClientApplication` を使用した対話的ブラウザ認証
- **テナント**: 単一テナント（`AadAuthorityAudience.AzureAdMyOrg`）
- **スコープ**: `https://storage.azure.com/.default`（Azure Storage へのアクセス）
- **トークンキャッシュ**: 利用可能なキャッシュがあれば `AcquireTokenSilent` を優先し、取得できない場合のみ `AcquireTokenInteractive` にフォールバックする。初版ではプロセス内キャッシュのみでも可とする
- **認証情報の取得元**: `bootstrap.json` から `tenantId`, `clientId` を取得
- **Client Secret は使用しない**: PublicClientApplication（デスクトップアプリ向けの認可コードフロー + PKCE）

### Blob アクセス方式

- 認証成功で取得した Entra ID トークンを `MsalTokenCredential` で包み、`Azure.Storage.Blobs.BlobClient` に `TokenCredential` として渡す
- `bootstrap.json` の `manifestUrl` から `manifest.json` を取得し、`downloadUrl` が相対 URL の場合は `manifestUrl` を基準 URI として解決する
- Storage Account には RBAC で `Storage Blob Data Reader` ロールを対象テナントユーザーに付与

### bootstrap.json スキーマ

```json
{
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "authority": "https://login.microsoftonline.com/{tenantId}",
  "manifestUrl": "https://{account}.blob.core.windows.net/archives/manifest.json",
  "scopes": ["https://storage.azure.com/.default"]
}
```

### manifest.json スキーマ

```json
{
  "version": "2026-04-01-v1",
  "downloadUrl": "slack-export-2026-04-01-v1.zip",
  "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "publishedAt": "2026-04-01T10:00:00Z"
}
```

`downloadUrl` は絶対 URL または相対 URL を許容し、相対 URL の場合は `manifestUrl` を基準に解決する。

### ローカルキャッシュ構造

```
%LOCALAPPDATA%/ChatArchiveViewer/CloudCache/
├── cache-state.json                ← 現在採用中の version / sha256 / ダウンロード日時
├── current.zip                     ← 検証済み ZIP（最新1世代）
└── downloading.zip.tmp             ← ダウンロード中の一時ファイル
```

**`cache-state.json` スキーマ**:

```json
{
  "version": "2026-04-01-v1",
  "sha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "downloadedAt": "2026-04-01T12:34:56Z",
  "bootstrapUrl": "https://..."
}
```

- ZIP 更新は「一時ファイルにダウンロード → hash 検証 → `current.zip` にリネーム → `cache-state.json` 更新」のアトミック手順で行う
- hash 不一致の場合、一時ファイルを削除し既存 cache を維持する

---

## Coarse interaction scenarios

### S-001: 初回ログイン実行（キャッシュなし・正常取得）

1. ユーザーが「ログインして開く」ボタンを押下
2. `CloudFetchOrchestrator` が固定 bootstrap URL から `bootstrap.json` を取得
3. MSAL によるブラウザ対話認証（ユーザーがサインイン）
4. 認証トークンで `manifest.json` を取得
5. キャッシュなし → ZIP ダウンロード開始
6. ダウンロード完了 → SHA-256 検証成功
7. `current.zip` + `cache-state.json` を書き込み
8. `current.zip` を `ZipArchiveSource` で開き、既存ビューアに渡す

### S-002: 2回目以降のログイン実行（キャッシュあり・最新版と同一）

1. ユーザーが「ログインして開く」ボタンを押下
2. `bootstrap.json` を取得
3. 利用可能なトークンキャッシュがあれば `AcquireTokenSilent`、利用できなければ対話認証
4. `manifest.json` を取得
5. `cache-state.json` の version と比較 → 同一
6. ダウンロードスキップ
7. 既存 `current.zip` をビューアに渡す

### S-003: ログイン実行時に新バージョン検出

1. ユーザーが「ログインして開く」ボタンを押下 → bootstrap 取得 → 認証 → manifest 取得
2. version が cache と異なる → ZIP ダウンロード
3. hash 検証成功 → cache 更新
4. 新しい `current.zip` をビューアに渡す

### S-004: ネットワーク障害（キャッシュあり）

1. ユーザーが「ログインして開く」ボタンを押下
2. `bootstrap.json` 取得失敗（ネットワークエラー）
3. `cache-state.json` に前回版あり → フォールバック
4. 既存 `current.zip` をビューアに渡す
5. **UI にステール警告を表示**（「最新取得に失敗したため前回取得済みを表示中」）

### S-005: ネットワーク障害（キャッシュなし）

1. ユーザーが「ログインして開く」ボタンを押下
2. `bootstrap.json` 取得失敗
3. キャッシュなし → エラー表示
4. ビューアにはアーカイブを渡さない

### S-006: SHA-256 不一致

1. ZIP ダウンロード完了 → hash 検証失敗
2. 一時ファイル削除
3. キャッシュに前回版あり → フォールバック + ステール警告
4. キャッシュなし → エラー表示

### S-007: 認証失敗 / キャンセル

1. MSAL 対話認証でユーザーがキャンセルまたはエラー
2. キャッシュあり → フォールバック + ステール警告
3. キャッシュなし → エラー表示

### S-008: ローカルアーカイブとの併用

1. クラウド取得完了後、ユーザーが手動で「Open Archive」を実行
2. ローカル ZIP / フォルダを選択して既存ビューアで開く
3. クラウドアーカイブからローカルアーカイブに切り替わる（通常のビューア動作）
4. 「前回取得済みを表示中」などクラウド取得由来の警告状態はクリアされる

### S-009: UI テスト用サンプル自動ロードとの共存

1. アプリ起動時に `AppLaunchOptions.AutoLoadSample` が指定されている
2. `MainPage` の `Loaded` ハンドラは既存どおり `OpenBundledSampleAsync` を優先する
3. その起動ではクラウド取得は自動実行しない
4. 既存の UI テスト / サンプル起動フローは維持され、必要時のみユーザーが「ログインして開く」を実行する

---

## Impacted code / files / modules

### 新規作成

| ファイル | 説明 |
|---|---|
| `src/ChatArchiveViewer.CloudFetch/ChatArchiveViewer.CloudFetch.csproj` | プロジェクトファイル |
| `src/ChatArchiveViewer.CloudFetch/CloudFetchConstants.cs` | 固定 bootstrap URL |
| `src/ChatArchiveViewer.CloudFetch/Models/BootstrapConfig.cs` | bootstrap.json モデル |
| `src/ChatArchiveViewer.CloudFetch/Models/CloudManifest.cs` | manifest.json モデル |
| `src/ChatArchiveViewer.CloudFetch/Models/CloudFetchResult.cs` | オーケストレーション結果 |
| `src/ChatArchiveViewer.CloudFetch/Models/CloudFetchProgress.cs` | クラウド取得進捗モデル |
| `src/ChatArchiveViewer.CloudFetch/Models/CacheState.cs` | cache-state.json モデル |
| `src/ChatArchiveViewer.CloudFetch/Abstractions/IBootstrapConfigProvider.cs` | bootstrap 取得インターフェース |
| `src/ChatArchiveViewer.CloudFetch/Abstractions/ICloudManifestProvider.cs` | manifest 取得インターフェース |
| `src/ChatArchiveViewer.CloudFetch/Abstractions/ICloudArchiveDownloader.cs` | ZIP ダウンロードインターフェース |
| `src/ChatArchiveViewer.CloudFetch/Abstractions/ICloudAuthService.cs` | 認証インターフェース |
| `src/ChatArchiveViewer.CloudFetch/Abstractions/ICloudFetchOrchestrator.cs` | クラウド取得オーケストレーションインターフェース |
| `src/ChatArchiveViewer.CloudFetch/Abstractions/ICacheManager.cs` | キャッシュ管理インターフェース |
| `src/ChatArchiveViewer.CloudFetch/Abstractions/IHashVerifier.cs` | hash 検証インターフェース |
| `src/ChatArchiveViewer.CloudFetch/Services/BootstrapConfigProvider.cs` | HttpClient で匿名取得 |
| `src/ChatArchiveViewer.CloudFetch/Services/CloudManifestProvider.cs` | BlobClient で認証付き取得 |
| `src/ChatArchiveViewer.CloudFetch/Services/CloudArchiveDownloader.cs` | BlobClient で ZIP ダウンロード |
| `src/ChatArchiveViewer.CloudFetch/Services/MsalAuthService.cs` | MSAL PublicClientApplication |
| `src/ChatArchiveViewer.CloudFetch/Services/MsalTokenCredential.cs` | Blob SDK 用 `TokenCredential` ラッパー |
| `src/ChatArchiveViewer.CloudFetch/Services/LocalCacheManager.cs` | ファイルシステムキャッシュ管理 |
| `src/ChatArchiveViewer.CloudFetch/Services/Sha256Verifier.cs` | SHA-256 検証 |
| `src/ChatArchiveViewer.CloudFetch/Services/CloudFetchOrchestrator.cs` | メインフロー制御 |
| `tests/ChatArchiveViewer.CloudFetch.Tests/...` | ユニットテスト群 |

### 既存ファイルの変更

| ファイル | 変更内容 |
|---|---|
| `ChatArchiveViewer.slnx` | 新規プロジェクト参照追加 |
| `src/ChatArchiveViewer.App/ChatArchiveViewer.App.csproj` | `ChatArchiveViewer.CloudFetch` への ProjectReference 追加 |
| `src/ChatArchiveViewer.App/App.xaml.cs` | CloudFetch 関連サービスの DI 登録 |
| `src/ChatArchiveViewer.App/Services/IArchiveWorkflowService.cs` | `OpenCloudArchiveAsync` メソッド追加 |
| `src/ChatArchiveViewer.App/Services/ArchiveWorkflowService.cs` | クラウドアーカイブ読み込みフロー追加 |
| `src/ChatArchiveViewer.App/ViewModels/MainPageViewModel.cs` | クラウド取得ステータス / 警告表示状態の保持 |
| `src/ChatArchiveViewer.App/Views/MainPage.xaml` | クラウド取得ステータス表示 UI 追加 |
| `src/ChatArchiveViewer.App/Views/MainPage.xaml.cs` | `Loaded` ハンドラは `AutoLoadSample` の既存フローのみ維持し、クラウド取得を自動実行しない |
| `src/ChatArchiveViewer.App/Views/BrowsePage.xaml` | 「ログインして開く」ボタンを追加し、CloudFetch 設定有効時のみ表示 |
| `src/ChatArchiveViewer.App/Views/BrowsePage.xaml.cs` | 「ログインして開く」押下時に `OpenCloudArchiveAsync` を実行 |
| `src/ChatArchiveViewer.App/Strings/` | クラウド関連のローカライズ文字列追加 |

### 変更しないファイル

- `ChatArchiveViewer.Core/` — 変更なし（抽象・モデル・サービスはそのまま）
- `ChatArchiveViewer.Formats.Slack/` — 変更なし（パーサーはそのまま）
- 既存テストプロジェクト — 既存テストは変更しない

---

## 設計の詳細

### CloudFetchOrchestrator の責務

`CloudFetchOrchestrator` はクラウド取得の全フローを制御する唯一のエントリポイントである。

```
public interface ICloudFetchOrchestrator
{
    Task<CloudFetchResult> FetchLatestAsync(
        IProgress<CloudFetchProgress>? progress,
        CancellationToken ct);
}
```

**`CloudFetchResult`** は以下の情報を返す：

```csharp
public sealed class CloudFetchResult
{
    public required CloudFetchStatus Status { get; init; }
    public string? CachedZipPath { get; init; }
    public string? Version { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum CloudFetchStatus
{
    None,               // クラウド取得状態なし / ローカルアーカイブ表示中
    FreshDownload,       // 新規ダウンロード成功
    AlreadyUpToDate,     // キャッシュが最新
    StaleCache,          // 取得失敗だがキャッシュあり
    NoCacheError         // 取得失敗かつキャッシュなし
}
```

### ArchiveWorkflowService への統合

`ArchiveWorkflowService` に `OpenCloudArchiveAsync` メソッドを追加する。
このメソッドは：

1. `ICloudFetchOrchestrator.FetchLatestAsync()` を呼ぶ
2. 結果の `CachedZipPath` を `ZipArchiveSource` で開く
3. 既存の `LoadArchiveAsync` → `SetCurrentAsync` → ViewModel 更新のフローに合流する
4. `MainPageViewModel` に `CloudFetchStatus` / エラーメッセージを反映する
5. `OpenArchiveAsync` / `OpenBundledSampleAsync` でローカルソースを開いた場合は `CloudFetchStatus.None` に戻して警告状態をクリアする

これにより、**既存のアーカイブ読み込み・表示ロジックは一切変更しない**。

### ICloudAuthService の設計

```csharp
public interface ICloudAuthService
{
    Task<TokenCredential> AuthenticateAsync(BootstrapConfig config, CancellationToken ct);
}
```

- `MsalAuthService` は `IPublicClientApplication` を構築し、まず `AcquireTokenSilent` を試み、失敗時に `AcquireTokenInteractive` を実行
- 取得した `AccessToken` を `Azure.Core` の `TokenCredential` を継承した `MsalTokenCredential` でラップして返す
- テストでは `ICloudAuthService` をモックして認証をスキップ可能

### ICacheManager の設計

```csharp
public interface ICacheManager
{
    string CacheDirectory { get; }
    Task<CacheState?> GetCurrentStateAsync(CancellationToken ct);
    Task<string> GetTempDownloadPathAsync(CancellationToken ct);
    Task CommitDownloadAsync(string tempPath, string version, string sha256, CancellationToken ct);
    string? GetCurrentZipPath();
}
```

- `GetCurrentStateAsync` は `cache-state.json` を読んで現在の version を返す
- `CommitDownloadAsync` は一時ファイルを `current.zip` にアトミックリネームし、`cache-state.json` を更新する
- `GetCurrentZipPath` はキャッシュ済み ZIP のパスを返す（なければ null）

### IHashVerifier の設計

```csharp
public interface IHashVerifier
{
    Task<bool> VerifyAsync(string filePath, string expectedSha256, CancellationToken ct);
}
```

- `Sha256Verifier` は `System.Security.Cryptography.SHA256` を使用してファイルの hash を計算し比較する
- 組み込み API のみ使用、外部依存なし

### エントリ操作フロー統合

クラウド取得は `BrowsePage` の「ログインして開く」ボタン押下を起点に実行する。`MainPage.xaml.cs` の `OnMainPageLoaded` は既存の `AutoLoadSample` 処理のみを担当し、クラウド取得を自動実行しない。`MainPageViewModel` は状態保持に専念させる。

```
App.OnLaunched()
  → MainWindow.Activate()
    → MainPage.OnMainPageLoaded()
      → if launchOptions.AutoLoadSample != null
           → ArchiveWorkflowService.OpenBundledSampleAsync()
           → クラウド取得は自動実行しない

User.Click("ログインして開く")
  → BrowsePage.OnOpenCloudClick()
    → ArchiveWorkflowService.OpenCloudArchiveAsync()
      → CloudFetchOrchestrator.FetchLatestAsync()
      → UI 更新（ステール警告含む）
```

ユーザーはクラウド取得完了後も、手動で「Open Archive」からローカルアーカイブを開くことが可能であり、その際にはクラウド取得由来の警告状態をクリアする。

### エラーハンドリング方針

copilot-instructions.md のルール「原則として処理失敗時のフォールバックは行わず、処理が失敗したことを示すエラー・例外を返す」に対するこの機能での例外的扱い：

- **各サービス層のメソッド**は失敗時に例外を throw する（原則遵守）
- **`CloudFetchOrchestrator`** のみがキャッシュフォールバックのロジックを持つ。bootstrap 取得、認証、manifest 取得、ZIP ダウンロード、hash 検証の各ステップで発生した例外を catch してトレースログに `Exception.ToString()` を出力し、キャッシュ有無に応じて `StaleCache` または `NoCacheError` の `CloudFetchResult` を返す
- これはオーケストレータが「全体のエラー戦略」を決定する設計であり、個々のサービスがフォールバックするわけではない

### ステール警告の UI 表現

`MainPageViewModel` に以下のプロパティを追加：

```csharp
[ObservableProperty]
private CloudFetchStatus cloudFetchStatus;

[ObservableProperty]
private string? cloudFetchErrorMessage;

public bool IsStaleWarningVisible => CloudFetchStatus == CloudFetchStatus.StaleCache;
```

`MainPage.xaml` に InfoBar を追加し、ステール状態または初回取得失敗状態を表示する。ローカルアーカイブを手動で開いた場合は `CloudFetchStatus.None` に戻して InfoBar を閉じる。

---

## Verification design

> 詳細なブラックボックステスト観点（30件の TP-C001〜TP-C030）は [cloud-archive-fetch-integration-test-points.md](./cloud-archive-fetch-integration-test-points.md) を参照。
> ランタイムシーケンスは [cloud-archive-fetch-runtime-evidence.md](./cloud-archive-fetch-runtime-evidence.md) を参照。

### ユニットテスト

| テスト対象 | 検証内容 |
|---|---|
| `CloudFetchOrchestrator` | 各シナリオ（S-001〜S-008）のフロー制御。各依存をモックし、正しい順序で呼ばれること、正しい `CloudFetchResult` を返すことを検証 |
| `LocalCacheManager` | cache-state.json の読み書き、アトミックコミット、一時ファイル管理 |
| `Sha256Verifier` | 正しい hash で true / 不正 hash で false を返すこと |
| `BootstrapConfigProvider` | bootstrap.json のデシリアライズ、不正 JSON でのエラー |
| `CloudManifestProvider` | manifest.json のデシリアライズ、不正 JSON でのエラー |
| `MsalAuthService` | MSAL 呼び出しのモック検証（`IPublicClientApplication` をモック） |
| `CloudArchiveDownloader` | BlobClient のモック検証 |
| `ArchiveWorkflowService.OpenCloudArchiveAsync` | CloudFetch 結果をビューアに正しく渡すこと |

### インテグレーションテスト（CI で動作可能）

| テスト内容 | 方式 |
|---|---|
| キャッシュ読み書きの統合フロー | 実ファイルシステム（tempdir 使用） |
| SHA-256 検証の E2E | テスト用 ZIP ファイルを生成してハッシュ検証 |
| Orchestrator のフルフロー | 全依存をモック注入、各シナリオをステップ実行 |

### 手動検証

| 検証内容 | 手順 |
|---|---|
| Entra ID 対話認証 | 実テナント環境でブラウザ認証を実行 |
| Azure Blob からの実ダウンロード | 実 Storage Account にテスト用ファイルを配置 |
| ステール警告 UI | ネットワークを切断して起動し、警告表示を確認 |

---

## Traceability matrix

| 要件 / 振る舞い | シナリオ | 検証方法 |
|---|---|---|
| 「ログインして開く」実行時に bootstrap.json を取得 | S-001, S-002 | UT: BootstrapConfigProvider + Orchestrator |
| MSAL で Entra ID 認証を実行 | S-001, S-002 | UT: MsalAuthService モック / 手動: 実テナント |
| manifest.json を認証付きで取得 | S-001, S-002 | UT: CloudManifestProvider + Orchestrator |
| version 比較でダウンロードスキップ | S-002 | UT: Orchestrator (キャッシュ一致) |
| 新バージョンの ZIP をダウンロード | S-001, S-003 | UT: CloudArchiveDownloader + Orchestrator |
| SHA-256 でダウンロード検証 | S-001, S-003, S-006 | UT: Sha256Verifier + IT: E2E hash 検証 |
| hash 不一致時は ZIP を採用しない | S-006 | UT: Orchestrator (hash 不一致パス) |
| キャッシュにアトミック保存 | S-001, S-003 | UT+IT: LocalCacheManager |
| ネットワーク障害時にキャッシュで表示 | S-004 | UT: Orchestrator (例外→StaleCache) |
| manifest 取得失敗時にキャッシュで表示 / エラー化 | S-004, S-005 | UT: Orchestrator (manifest 例外パス) |
| ZIP ダウンロード失敗時にキャッシュで表示 / エラー化 | S-004, S-005 | UT: Orchestrator (download 例外パス) |
| キャッシュなし+障害でエラー表示 | S-005 | UT: Orchestrator (例外→NoCacheError) |
| 認証失敗時のフォールバック | S-007 | UT: Orchestrator (認証例外パス) |
| ステール警告 UI の表示 | S-004, S-006, S-007 | UT: MainPageViewModel 状態検証 |
| 既存ビューア機能の維持 | S-008 | 既存テスト suite が通ること |
| ローカルアーカイブとの併用 | S-008 | 手動 + UT: クラウド後にローカル Open し警告状態がクリアされること |
| UI テスト用サンプル自動ロードの維持 | S-009 | 既存 UI テスト起動 + `AutoLoadSample` 指定時にクラウド取得が自動実行されないこと |
| Client Secret を使用しない | 全体 | コードレビュー: 認証フローの設計確認 |
| bootstrap.json に秘密情報を含めない | 全体 | コードレビュー: スキーマ確認 |

---

## Definition of Done

1. `ChatArchiveViewer.CloudFetch` プロジェクトが新規作成され、ソリューションに追加されている
2. 固定 bootstrap URL → bootstrap.json 取得 → MSAL 認証 → manifest.json 取得 → ZIP ダウンロード → hash 検証 → キャッシュ保存の全フローが実装されている
3. 「ログインして開く」ボタン押下時にクラウドアーカイブ取得が実行され、既存ビューアで表示される
4. ネットワーク障害 / 認証失敗 / hash 不一致時に、キャッシュがあればフォールバック、なければエラー表示する
5. ステール警告（「前回取得済みを表示中」）が UI に表示される
6. 既存のローカルアーカイブ読み込み機能は変更されず、ローカルアーカイブを開いたときはクラウド取得由来の警告状態がクリアされ、既存テストがすべて通る
7. 各サービスの抽象化によりユニットテストでモック注入が可能で、全シナリオのテストが CI で通る
8. Client Secret / Storage Key / 長寿命 SAS が一切使用されていない
9. `AppLaunchOptions.AutoLoadSample` が指定された起動では既存のサンプル自動ロードが優先され、クラウド取得は自動実行されない
10. 例外は全てトレースログに `Exception.ToString()` が出力されている

---

## Risks / rollout / rollback

### リスク

| リスク | 影響 | 軽減策 |
|---|---|---|
| MSAL 対話認証がデスクトップ環境で安定しない | 認証不能 | `ICloudAuthService` 抽象化で差し替え可能。フォールバックキャッシュで緩和 |
| Azure Storage の匿名アクセス設定ミス | bootstrap.json 取得不能 | 起動前の設定検証ドキュメントを用意 |
| RBAC 設定不足で Blob アクセス拒否 | manifest/ZIP 取得不能 | デプロイチェックリストに RBAC 確認を含める |
| ZIP サイズが大きくダウンロードに時間がかかる | UX 低下 | プログレス表示 + キャンセル対応 |
| プロセスを跨いだ MSAL トークンキャッシュを初版で実装しない場合 | 起動ごとに対話認証が再度必要になり得る | 初版では許容し、UX 要件が上がった時点で `Microsoft.Identity.Client.Extensions.Msal` を追加する |

### ロールバック

- クラウド取得機能は既存ビューアから分離されたサービス層のため、DI 登録を外すだけで無効化可能
- 既存のローカルアーカイブ機能は変更しないため、クラウド機能に問題があっても既存機能に影響しない

---

## Assumptions

| # | 項目 | 内容 |
|---|---|---|
| A-1 | bootstrap URL の保持方法 | 配布物に埋め込む固定 URL とし、`CloudFetchConstants.cs` で保持する |
| A-2 | キャッシュ保存先 | `%LOCALAPPDATA%/ChatArchiveViewer/CloudCache/` を使用し、パス解決は既存ログ出力と同様に `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` を使う |
| A-3 | 起動時の優先順位 | `AppLaunchOptions.AutoLoadSample` が指定された起動では既存のサンプル自動ロードを優先し、クラウド取得は自動実行しない |
| A-4 | 初版の再試行 UI | 「ログインして開く」ボタン押下を対象とし、追加の手動再試行 UI は初版スコープ外とする |
