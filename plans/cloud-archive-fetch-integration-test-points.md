# クラウド配信アーカイブ取得機能 — ブラックボックステスト観点

> 本ドキュメントは [cloud-archive-fetch.md](./cloud-archive-fetch.md) のブラックボックステスト観点を定義する。
> 各観点はモック使用の自動テスト（NUnit / CI 実行可能）と、モック不使用の手動テストの両方のインプットとして使用する。
> ランタイムエビデンスは [cloud-archive-fetch-runtime-evidence.md](./cloud-archive-fetch-runtime-evidence.md) を参照。

---

## テスト対象 I/F

| # | インターフェース | 実装 | 役割 |
|---|---|---|---|
| IF-C1 | `IBootstrapConfigProvider.GetConfigAsync` | `BootstrapConfigProvider` | bootstrap.json 取得・デシリアライズ |
| IF-C2 | `ICloudManifestProvider.GetManifestAsync` | `CloudManifestProvider` | manifest.json 取得・デシリアライズ |
| IF-C3 | `ICloudAuthService.AuthenticateAsync` | `MsalAuthService` | MSAL 対話的認証・トークン取得 |
| IF-C4 | `ICloudArchiveDownloader.DownloadAsync` | `CloudArchiveDownloader` | ZIP ダウンロード |
| IF-C5 | `ICacheManager` | `LocalCacheManager` | ローカルキャッシュ管理（cache-state.json, current.zip） |
| IF-C6 | `IHashVerifier.VerifyAsync` | `Sha256Verifier` | SHA-256 ハッシュ検証 |
| IF-C7 | `ICloudFetchOrchestrator.FetchLatestAsync` | `CloudFetchOrchestrator` | クラウド取得メインフロー制御 |
| IF-C8 | `ArchiveWorkflowService.OpenCloudArchiveAsync` | `ArchiveWorkflowService` | クラウドアーカイブのビューア統合 |

## 外部依存（テスト時のスタブ／モック対象）

| ID | 外部 | テスト時の置換 |
|---|---|---|
| X-Bootstrap | Azure Blob Storage ($web/bootstrap.json) | `IBootstrapConfigProvider` モック |
| X-Manifest | Azure Blob Storage (archives/manifest.json) | `ICloudManifestProvider` モック |
| X-Auth | Entra ID / MSAL 対話認証 | `ICloudAuthService` モック |
| X-Download | Azure Blob Storage (archives/*.zip) | `ICloudArchiveDownloader` モック |
| X-FS | ファイルシステム（キャッシュ） | temp ディレクトリで実ファイルシステム使用 |

---

## 入力パラメータパターン

### bootstrap.json コンテンツ

| パターン | 説明 |
|---|---|
| P-BS-01 | 全フィールド正常（tenantId, clientId, authority, manifestUrl, scopes） |
| P-BS-02 | JSON 構文エラー（不正 JSON） |
| P-BS-03 | 必須フィールド欠損（tenantId なし） |
| P-BS-04 | 必須フィールド欠損（clientId なし） |
| P-BS-05 | 必須フィールド欠損（manifestUrl なし） |
| P-BS-06 | 空の JSON オブジェクト `{}` |
| P-BS-07 | 空文字列レスポンス |

### manifest.json コンテンツ

| パターン | 説明 |
|---|---|
| P-MF-01 | 全フィールド正常（version, downloadUrl, sha256, publishedAt） |
| P-MF-02 | JSON 構文エラー |
| P-MF-03 | 必須フィールド欠損（version なし） |
| P-MF-04 | 必須フィールド欠損（sha256 なし） |
| P-MF-05 | 必須フィールド欠損（downloadUrl なし） |
| P-MF-06 | sha256 が不正な形式（64文字16進でない） |

### 認証応答パターン

| パターン | 説明 |
|---|---|
| P-AUTH-01 | 認証成功（有効な TokenCredential 返却） |
| P-AUTH-02 | ユーザーキャンセル（OperationCanceledException） |
| P-AUTH-03 | 認証エラー（MsalUiRequiredException） |
| P-AUTH-04 | サイレントトークン取得成功（キャッシュヒット） |
| P-AUTH-05 | ネットワーク障害で認証サーバー未到達（MsalServiceException） |

### ダウンロード応答パターン

| パターン | 説明 |
|---|---|
| P-DL-01 | 正常ダウンロード完了（有効な ZIP ファイル） |
| P-DL-02 | ダウンロード中ネットワークエラー（IOException / HttpRequestException） |
| P-DL-03 | 403 Forbidden（権限不足） |
| P-DL-04 | 404 Not Found（ZIP が存在しない） |

### キャッシュ状態パターン

| パターン | 説明 |
|---|---|
| P-CACHE-01 | キャッシュなし（cache-state.json, current.zip ともに不在） |
| P-CACHE-02 | キャッシュあり・version 同一 |
| P-CACHE-03 | キャッシュあり・version 不一致（新バージョン存在） |
| P-CACHE-04 | cache-state.json 存在・current.zip 不在（不整合） |
| P-CACHE-05 | cache-state.json が破損 JSON |
| P-CACHE-06 | cache-state.json が空ファイル |
| P-CACHE-07 | downloading.zip.tmp が残存（前回ダウンロード中断） |

### SHA-256 検証パターン

| パターン | 説明 |
|---|---|
| P-HASH-01 | ハッシュ一致（正常） |
| P-HASH-02 | ハッシュ不一致（改ざんまたは破損） |
| P-HASH-03 | 対象ファイルが存在しない |
| P-HASH-04 | ファイルサイズ 0 バイト |

---

## テスト観点

### 1. bootstrap.json の取得と解析

---

#### TP-C001: bootstrap.json の正常取得とデシリアライズ

**対象 I/F**: IF-C1 (`IBootstrapConfigProvider.GetConfigAsync`)
**関連シナリオ**: S-001, S-002, S-003
**Plan トレース**: 「ログインして開く」実行時に bootstrap.json を取得

| # | 条件 | 期待 |
|---|---|---|
| a | P-BS-01: 全フィールド正常 | `BootstrapConfig` が返り、`TenantId`, `ClientId`, `Authority`, `ManifestUrl`, `Scopes` が正しくマッピングされる |
| b | P-BS-01: `scopes` 配列に複数要素 | `Scopes` が配列で全要素保持される |

---

#### TP-C002: bootstrap.json の取得・解析失敗

**対象 I/F**: IF-C1 (`IBootstrapConfigProvider.GetConfigAsync`)
**関連シナリオ**: S-004, S-005
**Plan トレース**: 「ログインして開く」実行時に bootstrap.json を取得

| # | 条件 | 期待 |
|---|---|---|
| a | ネットワーク障害（HttpRequestException） | 例外が throw される |
| b | P-BS-02: JSON 構文エラー | 例外が throw される（デシリアライズ失敗） |
| c | P-BS-03: tenantId 欠損 | 例外が throw される（バリデーション失敗） |
| d | P-BS-04: clientId 欠損 | 例外が throw される（バリデーション失敗） |
| e | P-BS-05: manifestUrl 欠損 | 例外が throw される（バリデーション失敗） |
| f | P-BS-06: 空オブジェクト `{}` | 例外が throw される |
| g | P-BS-07: 空文字列レスポンス | 例外が throw される |

---

### 2. manifest.json の取得と解析

---

#### TP-C003: manifest.json の正常取得とデシリアライズ

**対象 I/F**: IF-C2 (`ICloudManifestProvider.GetManifestAsync`)
**関連シナリオ**: S-001, S-002, S-003
**Plan トレース**: manifest.json を認証付きで取得

| # | 条件 | 期待 |
|---|---|---|
| a | P-MF-01: 全フィールド正常 | `CloudManifest` が返り、`Version`, `DownloadUrl`, `Sha256`, `PublishedAt` が正しくマッピングされる |
| b | P-MF-01: `downloadUrl` が相対パス | `manifestUrl` を基準 URI として解決される |

---

#### TP-C004: manifest.json の取得・解析失敗

**対象 I/F**: IF-C2 (`ICloudManifestProvider.GetManifestAsync`)
**関連シナリオ**: S-004, S-005（manifest 段階での障害）
**Plan トレース**: manifest.json を認証付きで取得

| # | 条件 | 期待 |
|---|---|---|
| a | ネットワーク障害（Azure.RequestFailedException） | 例外が throw される |
| b | P-MF-02: JSON 構文エラー | 例外が throw される |
| c | P-MF-03: version 欠損 | 例外が throw される |
| d | P-MF-04: sha256 欠損 | 例外が throw される |
| e | P-MF-05: downloadUrl 欠損 | 例外が throw される |
| f | P-MF-06: sha256 が不正形式 | 例外が throw される（バリデーション失敗） |

---

### 3. MSAL 認証フロー

---

#### TP-C005: 認証の成功と失敗

**対象 I/F**: IF-C3 (`ICloudAuthService.AuthenticateAsync`)
**関連シナリオ**: S-001, S-002, S-007
**Plan トレース**: MSAL で Entra ID 認証を実行

| # | 条件 | 期待 |
|---|---|---|
| a | P-AUTH-01: 対話認証成功 | 有効な `TokenCredential` が返される |
| b | P-AUTH-04: サイレント取得成功（2回目以降） | `TokenCredential` が返される（ダイアログなし） |
| c | P-AUTH-02: ユーザーキャンセル | `OperationCanceledException` が throw される |
| d | P-AUTH-03: 認証エラー | `MsalUiRequiredException` 相当の例外が throw される |
| e | P-AUTH-05: ネットワーク障害で認証サーバー未到達 | 例外が throw される |

---

### 4. ZIP ダウンロードとハッシュ検証

---

#### TP-C006: ZIP ダウンロードの成否

**対象 I/F**: IF-C4 (`ICloudArchiveDownloader.DownloadAsync`)
**関連シナリオ**: S-001, S-003
**Plan トレース**: 新バージョンの ZIP をダウンロード

| # | 条件 | 期待 |
|---|---|---|
| a | P-DL-01: 正常ダウンロード | 指定 `tempPath` にファイルが書き込まれる |
| b | P-DL-02: ダウンロード中ネットワークエラー | 例外が throw される |
| c | P-DL-03: 403 Forbidden | 例外が throw される |
| d | P-DL-04: 404 Not Found | 例外が throw される |

---

#### TP-C007: SHA-256 ハッシュ検証

**対象 I/F**: IF-C6 (`IHashVerifier.VerifyAsync`)
**関連シナリオ**: S-001, S-003, S-006
**Plan トレース**: SHA-256 でダウンロード検証
**テスト方式**: 実ファイルシステム（temp ディレクトリ）

| # | 条件 | 期待 |
|---|---|---|
| a | P-HASH-01: ファイルのハッシュが期待値と一致 | `true` が返される |
| b | P-HASH-02: ファイルのハッシュが期待値と不一致 | `false` が返される |
| c | P-HASH-03: 対象ファイルが存在しない | 例外が throw される（`FileNotFoundException`） |
| d | P-HASH-04: ファイルサイズ 0 バイト | 空ファイルのハッシュ値との比較で正しく判定される |

---

### 5. キャッシュ管理（LocalCacheManager）

---

#### TP-C008: キャッシュ状態の読み取り

**対象 I/F**: IF-C5 (`ICacheManager.GetCurrentStateAsync`, `ICacheManager.GetCurrentZipPath`)
**関連シナリオ**: S-001, S-002, S-003, S-004, S-005
**Plan トレース**: キャッシュにアトミック保存
**テスト方式**: 実ファイルシステム（temp ディレクトリ）

| # | 条件 | 期待 |
|---|---|---|
| a | P-CACHE-01: キャッシュなし（ディレクトリが空） | `GetCurrentStateAsync` が `null` を返す |
| b | P-CACHE-01: キャッシュなし | `GetCurrentZipPath` が `null` を返す |
| c | P-CACHE-02: 正常な cache-state.json + current.zip が存在 | `GetCurrentStateAsync` が `CacheState`（version, sha256, downloadedAt）を返す |
| d | P-CACHE-02: 正常なキャッシュ存在 | `GetCurrentZipPath` が current.zip の有効パスを返す |
| e | P-CACHE-04: cache-state.json あり・current.zip なし | `GetCurrentZipPath` が `null` を返す（不整合検出） |
| f | P-CACHE-05: cache-state.json が破損 JSON | `GetCurrentStateAsync` が `null` を返すか例外を throw する |
| g | P-CACHE-06: cache-state.json が空ファイル | `GetCurrentStateAsync` が `null` を返すか例外を throw する |

---

#### TP-C009: キャッシュへのコミット（アトミック更新）

**対象 I/F**: IF-C5 (`ICacheManager.GetTempDownloadPathAsync`, `ICacheManager.CommitDownloadAsync`)
**関連シナリオ**: S-001, S-003
**Plan トレース**: キャッシュにアトミック保存
**テスト方式**: 実ファイルシステム（temp ディレクトリ）

| # | 条件 | 期待 |
|---|---|---|
| a | `GetTempDownloadPathAsync` 呼び出し | 一時ダウンロードパス（`downloading.zip.tmp`）が返される |
| b | tempPath にファイルを書き込み → `CommitDownloadAsync` | `current.zip` が作成される |
| c | tempPath にファイルを書き込み → `CommitDownloadAsync` | `cache-state.json` が作成され、version, sha256, downloadedAt が正しい |
| d | tempPath にファイルを書き込み → `CommitDownloadAsync` | 一時ファイル（`downloading.zip.tmp`）が削除される |
| e | 既存キャッシュがある状態で `CommitDownloadAsync` | `current.zip` が新ファイルで上書きされる |
| f | 既存キャッシュがある状態で `CommitDownloadAsync` | `cache-state.json` が新バージョン情報で上書きされる |
| g | P-CACHE-07: `downloading.zip.tmp` が残存している状態で `GetTempDownloadPathAsync` | 残存ファイルが処理される（上書きまたは削除後に新パスを返す） |

---

### 6. オーケストレーション全体フロー（CloudFetchOrchestrator）

---

#### TP-C010: 初回ログイン実行・キャッシュなし・正常取得（S-001）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-001
**Plan トレース**: 初回ログイン実行で ZIP を取得し表示

| # | 条件 | 期待 |
|---|---|---|
| a | P-BS-01 + P-AUTH-01 + P-MF-01 + P-CACHE-01 + P-DL-01 + P-HASH-01 | `CloudFetchResult.Status == FreshDownload` |
| b | 同上 | `CloudFetchResult.CachedZipPath` が非 null で有効パス |
| c | 同上 | `CloudFetchResult.Version` が manifest の version と一致 |
| d | 同上 | 依存呼び出し順: Bootstrap → Auth → Manifest → Cache(状態確認) → Download → Hash → Cache(コミット) |

---

#### TP-C011: キャッシュヒット・最新版と同一（S-002）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-002
**Plan トレース**: キャッシュヒットでスキップ表示

| # | 条件 | 期待 |
|---|---|---|
| a | P-BS-01 + P-AUTH-04 + P-MF-01 + P-CACHE-02（version 同一） | `CloudFetchResult.Status == AlreadyUpToDate` |
| b | 同上 | `CloudFetchResult.CachedZipPath` が既存 current.zip のパス |
| c | 同上 | `ICloudArchiveDownloader.DownloadAsync` が呼ばれない |
| d | 同上 | `IHashVerifier.VerifyAsync` が呼ばれない |

---

#### TP-C012: 新バージョン検出・更新取得（S-003）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-003
**Plan トレース**: 新 version の ZIP を更新取得

| # | 条件 | 期待 |
|---|---|---|
| a | P-BS-01 + P-AUTH-01 + P-MF-01 + P-CACHE-03（version 不一致）+ P-DL-01 + P-HASH-01 | `CloudFetchResult.Status == FreshDownload` |
| b | 同上 | `CloudFetchResult.Version` が manifest の新 version と一致 |
| c | 同上 | `ICloudArchiveDownloader.DownloadAsync` が呼ばれる |
| d | 同上 | `ICacheManager.CommitDownloadAsync` が呼ばれる |

---

#### TP-C013: ネットワーク障害・キャッシュあり（S-004）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-004
**Plan トレース**: ネットワーク障害時のキャッシュフォールバック

| # | 条件 | 期待 |
|---|---|---|
| a | Bootstrap 取得で HttpRequestException + P-CACHE-02 | `CloudFetchResult.Status == StaleCache` |
| b | 同上 | `CloudFetchResult.CachedZipPath` が既存 current.zip のパス |
| c | 同上 | トレースログに `Exception.ToString()` が出力される |

---

#### TP-C014: ネットワーク障害・キャッシュなし（S-005）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-005
**Plan トレース**: ネットワーク障害＋キャッシュなし→エラー

| # | 条件 | 期待 |
|---|---|---|
| a | Bootstrap 取得で HttpRequestException + P-CACHE-01 | `CloudFetchResult.Status == NoCacheError` |
| b | 同上 | `CloudFetchResult.CachedZipPath` が `null` |
| c | 同上 | `CloudFetchResult.ErrorMessage` が非 null |
| d | 同上 | トレースログに `Exception.ToString()` が出力される |

---

#### TP-C015: SHA-256 不一致・キャッシュあり（S-006 分岐 1）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-006
**Plan トレース**: hash 不一致時は ZIP を採用しない

| # | 条件 | 期待 |
|---|---|---|
| a | P-HASH-02（ハッシュ不一致）+ P-CACHE-02（前回キャッシュあり） | `CloudFetchResult.Status == StaleCache` |
| b | 同上 | `CloudFetchResult.CachedZipPath` が既存 current.zip のパス |
| c | 同上 | 一時ファイル（downloading.zip.tmp）が削除される |
| d | 同上 | トレースログにハッシュ不一致の情報が出力される |

---

#### TP-C016: SHA-256 不一致・キャッシュなし（S-006 分岐 2）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-006
**Plan トレース**: hash 不一致時は ZIP を採用しない

| # | 条件 | 期待 |
|---|---|---|
| a | P-HASH-02（ハッシュ不一致）+ P-CACHE-01（キャッシュなし） | `CloudFetchResult.Status == NoCacheError` |
| b | 同上 | `CloudFetchResult.ErrorMessage` が非 null |
| c | 同上 | 一時ファイルが削除される |

---

#### TP-C017: 認証失敗・キャッシュあり（S-007 分岐 1）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-007
**Plan トレース**: 認証失敗時のフォールバック

| # | 条件 | 期待 |
|---|---|---|
| a | P-AUTH-02（ユーザーキャンセル）+ P-CACHE-02 | `CloudFetchResult.Status == StaleCache` |
| b | P-AUTH-03（認証エラー）+ P-CACHE-02 | `CloudFetchResult.Status == StaleCache` |
| c | 同上 | `CloudFetchResult.CachedZipPath` が既存 current.zip のパス |
| d | 同上 | トレースログに `Exception.ToString()` が出力される |

---

#### TP-C018: 認証失敗・キャッシュなし（S-007 分岐 2）

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-007
**Plan トレース**: 認証失敗時のフォールバック

| # | 条件 | 期待 |
|---|---|---|
| a | P-AUTH-02（ユーザーキャンセル）+ P-CACHE-01 | `CloudFetchResult.Status == NoCacheError` |
| b | P-AUTH-03（認証エラー）+ P-CACHE-01 | `CloudFetchResult.Status == NoCacheError` |
| c | 同上 | `CloudFetchResult.ErrorMessage` が非 null |

---

#### TP-C019: manifest 取得失敗時のフォールバック

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-004 の変形（bootstrap 成功→認証成功→manifest 失敗）
**Plan トレース**: ネットワーク障害時のキャッシュフォールバック

| # | 条件 | 期待 |
|---|---|---|
| a | Bootstrap 成功 + Auth 成功 + Manifest 取得で例外 + P-CACHE-02 | `CloudFetchResult.Status == StaleCache` |
| b | Bootstrap 成功 + Auth 成功 + Manifest 取得で例外 + P-CACHE-01 | `CloudFetchResult.Status == NoCacheError` |
| c | 同上 | トレースログに `Exception.ToString()` が出力される |

---

#### TP-C020: ダウンロード失敗時のフォールバック

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-004 の変形（bootstrap〜manifest 成功→ダウンロード失敗）
**Plan トレース**: ネットワーク障害時のキャッシュフォールバック

| # | 条件 | 期待 |
|---|---|---|
| a | フロー途中で P-DL-02（ダウンロード中ネットワークエラー）+ P-CACHE-02 | `CloudFetchResult.Status == StaleCache` |
| b | フロー途中で P-DL-03（403）+ P-CACHE-01 | `CloudFetchResult.Status == NoCacheError` |
| c | フロー途中で P-DL-04（404）+ P-CACHE-01 | `CloudFetchResult.Status == NoCacheError` |
| d | 同上 | トレースログに `Exception.ToString()` が出力される |

---

#### TP-C021: CancellationToken によるキャンセル

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: 全シナリオ共通
**Plan トレース**: 全体のキャンセル対応

| # | 条件 | 期待 |
|---|---|---|
| a | Bootstrap 取得中に CancellationToken がキャンセル | `OperationCanceledException` が throw される |
| b | ダウンロード中に CancellationToken がキャンセル | `OperationCanceledException` が throw される |
| c | キャンセル後 | 一時ファイルがあれば削除される（リソースリーク防止） |

---

#### TP-C022: IProgress による進捗通知

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-001, S-002, S-003
**Plan トレース**: プログレス表示 + キャンセル対応

| # | 条件 | 期待 |
|---|---|---|
| a | 正常フロー（S-001 相当）実行中に `IProgress<CloudFetchProgress>` をキャプチャ | 各フェーズ（Bootstrap / Auth / Manifest / Download / Verify / Commit 等）で進捗が報告される |
| b | `progress` が `null` で渡された場合 | 例外なく正常に処理が完了する |

---

### 7. ArchiveWorkflowService 統合

---

#### TP-C023: クラウドアーカイブの正常表示統合

**対象 I/F**: IF-C8 (`ArchiveWorkflowService.OpenCloudArchiveAsync`)
**関連シナリオ**: S-001, S-002, S-003
**Plan トレース**: 既存ビューア機能の維持

| # | 条件 | 期待 |
|---|---|---|
| a | `CloudFetchResult(FreshDownload, zipPath)` を渡す | `ZipArchiveSource` で ZIP が開かれる |
| b | 同上 | フォーマット検出→ロード→セッション設定が完了する |
| c | `CloudFetchResult(AlreadyUpToDate, zipPath)` を渡す | 同様にキャッシュ ZIP で表示される |
| d | `CloudFetchResult.Status` が `StaleCache` | ビューアが表示され、かつステール警告状態が伝搬される |

---

#### TP-C024: クラウドアーカイブ表示後のローカル切替（S-008）

**対象 I/F**: IF-C8 (`ArchiveWorkflowService`)
**関連シナリオ**: S-008
**Plan トレース**: ローカルアーカイブとの併用

| # | 条件 | 期待 |
|---|---|---|
| a | クラウドアーカイブ表示中 → ローカル ZIP を `OpenLocalArchiveAsync` で開く | ローカルアーカイブに切り替わる |
| b | 同上 | ステール警告が消去される |

---

#### TP-C025: NoCacheError 時のワークフロー

**対象 I/F**: IF-C8 (`ArchiveWorkflowService.OpenCloudArchiveAsync`)
**関連シナリオ**: S-005
**Plan トレース**: キャッシュなし+障害でエラー表示

| # | 条件 | 期待 |
|---|---|---|
| a | `CloudFetchResult(NoCacheError, errorMessage)` を渡す | アーカイブは開かれない（ビューアにデータを渡さない） |
| b | 同上 | エラーメッセージが伝搬される |

---

#### TP-C031: BootstrapConfigUrl 設定値に応じて「ログインして開く」ボタン表示を切り替える

**対象 I/F**: `CloudFetchConstants.GetBootstrapConfigUrl` / `CloudFetchFeatureOptions` / `BrowsePage`
**関連シナリオ**: S-009
**Plan トレース**: CloudFetch 設定値による UI 表示制御

| # | 条件 | 期待 |
|---|---|---|
| a | `CloudFetch:BootstrapConfigUrl` が未設定/空白（DefaultBootstrapConfigUrl） | `CloudFetchFeatureOptions.IsEnabled == false` となり、「ログインして開く」ボタンは `Collapsed` |
| b | `CloudFetch:BootstrapConfigUrl` が絶対 URI | `CloudFetchFeatureOptions.IsEnabled == true` となり、「ログインして開く」ボタンは `Visible` |
| c | 可視状態でボタン押下 | `OpenCloudArchiveAsync` が実行され、ログイン・ダウンロード・表示フローへ遷移する |

---

### 8. エラーハンドリングとフォールバック

---

#### TP-C026: エラー障害発生位置ごとのフォールバック判定

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-004, S-005, S-006, S-007
**Plan トレース**: 各段階で例外→キャッシュ有無で分岐

| # | 条件 | 期待 |
|---|---|---|
| a | Bootstrap 失敗 + キャッシュあり | `StaleCache`（TP-C013 と重複検証） |
| b | Auth 失敗 + キャッシュあり | `StaleCache`（TP-C017 と重複検証） |
| c | Manifest 失敗 + キャッシュあり | `StaleCache`（TP-C019 と重複検証） |
| d | Download 失敗 + キャッシュあり | `StaleCache`（TP-C020 と重複検証） |
| e | Hash 失敗 + キャッシュあり | `StaleCache`（TP-C015 と重複検証） |
| f | 上記すべてで、キャッシュなしの場合 | `NoCacheError` |

> TP-C026 はフォールバック判定の**網羅性確認**のための観点であり、個別シナリオテスト（TP-C013〜TP-C020）で各条件がカバーされていれば追加ケースは不要。

---

#### TP-C027: トレースログ出力の検証

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-004, S-005, S-006, S-007
**Plan トレース**: 例外は全てトレースログに Exception.ToString() 出力

| # | 条件 | 期待 |
|---|---|---|
| a | Bootstrap 失敗で例外 catch | ログに `Exception.ToString()` の内容が出力される |
| b | Auth 失敗で例外 catch | ログに `Exception.ToString()` の内容が出力される |
| c | Manifest 失敗で例外 catch | ログに `Exception.ToString()` の内容が出力される |
| d | Download 失敗で例外 catch | ログに `Exception.ToString()` の内容が出力される |
| e | Hash 不一致 | ログにハッシュ不一致の詳細（期待値/実値）が出力される |

---

### 9. 負荷・連続実行

---

#### TP-C028: 大容量 ZIP のダウンロードとハッシュ検証

**対象 I/F**: IF-C6 (`IHashVerifier.VerifyAsync`), IF-C5 (`ICacheManager`)
**関連シナリオ**: S-001
**テスト方式**: 実ファイルシステム（temp ディレクトリ）

| # | 条件 | 期待 |
|---|---|---|
| a | 大きめのテスト用ファイル（例: 10MB）に対するハッシュ検証 | 正しく計算・比較でき、タイムアウトしない |
| b | 大きめのファイルに対する `CommitDownloadAsync` | アトミックリネームが正常に完了する |

---

#### TP-C029: 連続実行時の安定性

**対象 I/F**: IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-001 → S-002 連続

| # | 条件 | 期待 |
|---|---|---|
| a | FetchLatestAsync を連続 2 回実行（1 回目: 初回取得、2 回目: キャッシュヒット） | 1 回目: `FreshDownload`、2 回目: `AlreadyUpToDate` |
| b | FetchLatestAsync 実行後にキャッシュファイルを手動削除 → 再実行 | 再度 `FreshDownload` |
| c | TP-C009 のコミット直後に `GetCurrentStateAsync` | コミットした version/sha256 が即座に読み取れる |

---

#### TP-C030: 前回中断痕跡がある状態でのログイン実行

**対象 I/F**: IF-C5 (`ICacheManager`), IF-C7 (`ICloudFetchOrchestrator.FetchLatestAsync`)
**関連シナリオ**: S-001（前回ダウンロードが中断した状態からの復旧）

| # | 条件 | 期待 |
|---|---|---|
| a | P-CACHE-07: `downloading.zip.tmp` が残存 + P-CACHE-01（キャッシュなし）→ FetchLatestAsync 正常実行 | 残存一時ファイルが削除されるか上書きされ、正常に `FreshDownload` が完了する |
| b | P-CACHE-07: `downloading.zip.tmp` が残存 + P-CACHE-02（キャッシュあり）→ FetchLatestAsync でキャッシュヒット | `AlreadyUpToDate` が返り、残存一時ファイルは処理される |

---

## Plan トレーサビリティ

| Plan 要件 / シナリオ | カバーする TP-ID |
|---|---|
| 「ログインして開く」実行時に bootstrap.json を取得 | TP-C001, TP-C002 |
| MSAL で Entra ID 認証を実行 | TP-C005 |
| manifest.json を認証付きで取得 | TP-C003, TP-C004 |
| version 比較でダウンロードスキップ | TP-C011 |
| 新バージョンの ZIP をダウンロード | TP-C006, TP-C012 |
| SHA-256 でダウンロード検証 | TP-C007, TP-C015, TP-C016 |
| hash 不一致時は ZIP を採用しない | TP-C015, TP-C016 |
| キャッシュにアトミック保存 | TP-C008, TP-C009 |
| ネットワーク障害時にキャッシュで表示 (S-004) | TP-C013, TP-C019, TP-C020 |
| キャッシュなし+障害でエラー表示 (S-005) | TP-C014, TP-C025 |
| 認証失敗時のフォールバック (S-007) | TP-C017, TP-C018 |
| ステール警告の伝搬 | TP-C023d |
| 既存ビューア機能の維持 | TP-C023, TP-C024 |
| ローカルアーカイブとの併用 (S-008) | TP-C024 |
| CloudFetch 設定値によるボタン表示制御 (S-009) | TP-C031 |
| トレースログに Exception.ToString() 出力 | TP-C027 |
| CancellationToken 対応 | TP-C021 |
| IProgress 進捗通知 | TP-C022 |
| Client Secret を使用しない | コードレビュー（テスト対象外） |
| bootstrap.json に秘密情報を含めない | コードレビュー（テスト対象外） |

---

## シナリオ → テスト観点マッピング

| シナリオ | TP-ID |
|---|---|
| S-001: 初回ログイン実行（キャッシュなし・正常取得） | TP-C001, TP-C003, TP-C005a, TP-C006a, TP-C007a, TP-C009, TP-C010, TP-C022, TP-C023 |
| S-002: 2回目以降のログイン実行（キャッシュあり・同一） | TP-C001, TP-C003, TP-C005b, TP-C008c/d, TP-C011, TP-C023c |
| S-003: 新バージョン検出 | TP-C001, TP-C003, TP-C005a, TP-C006a, TP-C007a, TP-C009, TP-C012, TP-C023 |
| S-004: ネットワーク障害（キャッシュあり） | TP-C002a, TP-C013, TP-C019, TP-C020, TP-C027 |
| S-005: ネットワーク障害（キャッシュなし） | TP-C002a, TP-C014, TP-C025, TP-C027 |
| S-006: SHA-256 不一致 | TP-C007b, TP-C015, TP-C016, TP-C027e |
| S-007: 認証失敗 / キャンセル | TP-C005c/d, TP-C017, TP-C018, TP-C027b |
| S-008: ローカルアーカイブとの併用 | TP-C024 |
| S-009: CloudFetch 設定値による「ログインして開く」表示制御 | TP-C031 |
