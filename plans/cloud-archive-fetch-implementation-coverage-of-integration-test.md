# クラウド配信アーカイブ取得機能 — 実装カバレッジ（統合テスト観点）

> Plan: [cloud-archive-fetch.md](./cloud-archive-fetch.md)  
> 観点: [cloud-archive-fetch-integration-test-points.md](./cloud-archive-fetch-integration-test-points.md)  
> 更新日: 2026年4月4日

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

| ID | 状態 | 対応テスト | 判定理由 |
|---|---|---|---|
| TP-C001 | `Automated` | `UT_IT_TP_C001__GetConfigAsync_ValidJson_ReturnsConfig` | bootstrap.json 正常取得と必須項目・scopes を検証した |
| TP-C002 | `Automated` | `UT_IT_TP_C002__GetConfigAsync_MissingTenantId_ThrowsException` | bootstrap 不正入力時に例外を返す挙動を検証した |
| TP-C003 | `RecordedButSkipped` | `UT_IT_TP_C003__CloudManifestProvider_Success_RequiresAuthenticatedBlobEnvironment` | `CloudManifestProvider` の具象実装と DI 配線は存在するが、通常 CI では認証付き Blob 環境が必要なため Explicit に記録した |
| TP-C004 | `RecordedButSkipped` | `UT_IT_TP_C004__CloudManifestProvider_Failure_RequiresAuthenticatedBlobEnvironment` | 失敗パスも具象実装は存在するが、Blob 応答を本物で観測するには専用 Azure 環境が必要なため Explicit に記録した |
| TP-C005 | `RecordedButSkipped` | `UT_IT_TP_C005__MsalAuthService_RequiresInteractiveTenantEnvironment` | `MsalAuthService` の具象実装と DI 配線は存在するが、silent/interactive/cancel の本物確認には対話的 Entra ID 環境が必要なため Explicit に記録した |
| TP-C006 | `RecordedButSkipped` | `UT_IT_TP_C006__CloudArchiveDownloader_RequiresAuthenticatedBlobEnvironment` | `CloudArchiveDownloader` の具象実装と DI 配線は存在するが、Blob ダウンロード成否の本物確認は通常 CI 外と判断した |
| TP-C007 | `Automated` | `UT_IT_TP_C007a__VerifyAsync_HashMatches_ReturnsTrue`, `UT_IT_TP_C007b__VerifyAsync_HashMismatch_ReturnsFalse`, `UT_IT_TP_C007c__VerifyAsync_MissingFile_ThrowsFileNotFoundException`, `UT_IT_TP_C007d__VerifyAsync_EmptyFileHash_ReturnsTrue` | SHA-256 一致/不一致・ファイル欠損・空ファイルを実ファイルで検証した |
| TP-C008 | `Automated` | `UT_IT_TP_C008__GetCurrentStateAndZipPath_NoCache_ReturnsNull`, `UT_IT_TP_C008__GetCurrentStateAndZipPath_ValidCacheAndMissingZip_AreHandled`, `UT_IT_TP_C008__GetCurrentStateAsync_BrokenJson_ReturnsNull`, `UT_IT_TP_C008__GetCurrentStateAsync_EmptyStateFile_ReturnsNull` | キャッシュ未存在・正常 state・zip 不整合・破損/空 state を検証した |
| TP-C009 | `Automated` | `UT_IT_TP_C009__CommitDownloadAsync_CreatesZipAndStateAndRemovesTemp`, `UT_IT_TP_C009__CommitDownloadAsync_OverwritesExistingCacheAndCleansResidualTemp` | temp path 生成、コミット、既存キャッシュ上書き、残存 temp 除去を検証した |
| TP-C010 | `Automated` | `UT_IT_TP_C010__FetchLatestAsync_NoCacheAndSuccess_ReturnsFreshDownload` | 初回正常フローの呼び出し順と結果を検証した |
| TP-C011 | `Automated` | `UT_IT_TP_C011__FetchLatestAsync_CacheHit_ReturnsAlreadyUpToDate` | キャッシュ一致時のダウンロードスキップを検証した |
| TP-C012 | `Automated` | `UT_IT_TP_C012__FetchLatestAsync_VersionChanged_ReturnsFreshDownloadAndCommits` | version 不一致時に再ダウンロードとコミットが行われることを検証した |
| TP-C013 | `Automated` | `UT_IT_TP_C013_TP_C027a__FetchLatestAsync_BootstrapFailsWithCache_ReturnsStaleAndLogsException` | bootstrap 失敗時に StaleCache へ分岐することを検証した |
| TP-C014 | `Automated` | `UT_IT_TP_C014__FetchLatestAsync_BootstrapFailsWithoutCache_ReturnsNoCacheError` | キャッシュなし時に NoCacheError を返すことを検証した |
| TP-C015 | `Automated` | `UT_IT_TP_C015_TP_C016__FetchLatestAsync_HashMismatch_DeletesTempAndFallsBackByCacheState` | hash 不一致 + キャッシュあり時のフォールバックと temp 削除を検証した |
| TP-C016 | `Automated` | `UT_IT_TP_C015_TP_C016__FetchLatestAsync_HashMismatch_DeletesTempAndFallsBackByCacheState` | hash 不一致 + キャッシュなし時の NoCacheError を検証した |
| TP-C017 | `Automated` | `UT_IT_TP_C017_TP_C027b__FetchLatestAsync_AuthenticationCanceledWithCache_ReturnsStaleCache`, `UT_IT_TP_C017_TP_C018__FetchLatestAsync_AuthenticationFailure_FallsBackByCacheState` | 認証キャンセル/認証例外でキャッシュありなら StaleCache にフォールバックすることを検証した。実装も user cancel をフォールバック対象に補修した |
| TP-C018 | `Automated` | `UT_IT_TP_C018__FetchLatestAsync_AuthenticationFailureWithoutCache_ReturnsNoCacheError`, `UT_IT_TP_C017_TP_C018__FetchLatestAsync_AuthenticationFailure_FallsBackByCacheState` | 認証キャンセル/認証例外でキャッシュなしなら NoCacheError になることを検証した |
| TP-C019 | `Automated` | `UT_IT_TP_C019_TP_C027c__FetchLatestAsync_ManifestFailure_FallsBackByCacheState` | manifest 取得失敗でキャッシュ有無に応じて StaleCache / NoCacheError が返ることを検証した |
| TP-C020 | `Automated` | `UT_IT_TP_C020_TP_C027d__FetchLatestAsync_DownloadFailure_DeletesTempAndFallsBackByCacheState` | ダウンロード失敗時の temp 削除とキャッシュ有無による分岐を検証した |
| TP-C021 | `Automated` | `UT_IT_TP_C021__FetchLatestAsync_Canceled_ThrowsOperationCanceledException`, `UT_IT_TP_C021__FetchLatestAsync_DownloadCanceled_DeletesTempAndRethrows` | 呼び出し元トークンによるキャンセル再送出と temp クリーンアップを検証した |
| TP-C022 | `Automated` | `UT_IT_TP_C022__FetchLatestAsync_ReportsProgressStages` | 主要フェーズの進捗通知を検証した |
| TP-C023 | `Automated` | `UT_IT_TP_C023__OpenCloudArchiveAsync_FreshDownload_LoadsZipAndSetsSession`, `UT_IT_TP_C023__OpenCloudArchiveAsync_StaleCache_LoadsZipAndKeepsWarningStatus` | `ArchiveWorkflowService.OpenCloudArchiveAsync` が `ZipArchiveSource` 経由で表示し、警告状態を MainPageViewModel へ伝搬することを検証した |
| TP-C024 | `Automated` | `UT_IT_TP_C024__OpenArchiveAsync_LocalArchiveClearsCloudWarning` | ローカルアーカイブ読込時にクラウド由来の警告状態がクリアされることを検証した |
| TP-C025 | `Automated` | `UT_IT_TP_C025__OpenCloudArchiveAsync_NoCacheError_DoesNotLoadArchive` | `NoCacheError` 時にアーカイブを開かず、エラー状態だけを反映することを検証した |
| TP-C026 | `Automated` | `UT_IT_TP_C013_TP_C027a__FetchLatestAsync_BootstrapFailsWithCache_ReturnsStaleAndLogsException`, `UT_IT_TP_C014__FetchLatestAsync_BootstrapFailsWithoutCache_ReturnsNoCacheError`, `UT_IT_TP_C015_TP_C016__FetchLatestAsync_HashMismatch_DeletesTempAndFallsBackByCacheState`, `UT_IT_TP_C017_TP_C027b__FetchLatestAsync_AuthenticationCanceledWithCache_ReturnsStaleCache`, `UT_IT_TP_C017_TP_C018__FetchLatestAsync_AuthenticationFailure_FallsBackByCacheState`, `UT_IT_TP_C018__FetchLatestAsync_AuthenticationFailureWithoutCache_ReturnsNoCacheError`, `UT_IT_TP_C019_TP_C027c__FetchLatestAsync_ManifestFailure_FallsBackByCacheState`, `UT_IT_TP_C020_TP_C027d__FetchLatestAsync_DownloadFailure_DeletesTempAndFallsBackByCacheState` | bootstrap/auth/manifest/download/hash の各障害位置でキャッシュ有無に応じたフォールバック分岐を網羅した |
| TP-C027 | `NotImplementedOrMismatch` | `UT_IT_TP_C013_TP_C027a__FetchLatestAsync_BootstrapFailsWithCache_ReturnsStaleAndLogsException`, `UT_IT_TP_C017_TP_C027b__FetchLatestAsync_AuthenticationCanceledWithCache_ReturnsStaleCache`, `UT_IT_TP_C019_TP_C027c__FetchLatestAsync_ManifestFailure_FallsBackByCacheState`, `UT_IT_TP_C020_TP_C027d__FetchLatestAsync_DownloadFailure_DeletesTempAndFallsBackByCacheState` | 例外 catch 時の `Exception.ToString()` ログは bootstrap/auth/manifest/download で検証済み。ただし hash 不一致では実装が期待値しか出力しておらず、観点 e の「期待値/実値」両方の詳細ログは未実装 |
| TP-C028 | `RecordedButSkipped` | `UT_IT_TP_C028__LargeArchive_DownloadAndVerify_RequiresLoadEnvironment` | 大容量 ZIP は CI 負荷が高く、通常テストからは除外した |
| TP-C029 | `RecordedButSkipped` | `UT_IT_TP_C029__RepeatedFetch_RequiresSoakEnvironment` | 連続実行安定性は soak テスト向けで通常 CI から除外した |
| TP-C030 | `RecordedButSkipped` | `UT_IT_TP_C030__InterruptedDownloadRecovery_RequiresIntegrationEnvironment` | 中断痕跡の復旧確認は統合/耐障害試験向けで通常 CI から除外した |
| TP-C031 | `Automated` | `CloudFetchFeatureOptions_WhenBootstrapConfigUrlIsNull_IsDisabled`, `CloudFetchFeatureOptions_WhenBootstrapConfigUrlIsWhitespace_IsDisabled`, `CloudFetchFeatureOptions_WhenBootstrapConfigUrlIsAbsoluteUri_IsEnabled` | `GetBootstrapConfigUrl` の結果を受けた `CloudFetchFeatureOptions` が有効/無効を正しく切り替えることを検証し、ボタン表示制御の基礎を担保した |

---

## 補足メモ

- `CloudManifestProvider` / `CloudArchiveDownloader` / `MsalAuthService` の本物の実装は `src/ChatArchiveViewer.CloudFetch/Services/` に存在し、`src/ChatArchiveViewer.App/App.xaml.cs` で既定 DI に配線されていることを浅く確認した。
- `テストはあるが本物の実装が見つからない` 項目は今回確認範囲では見つからなかった。
- 未解消の主な差分は TP-C027e（hash 不一致ログに actual hash が含まれない）で、これは `IHashVerifier` の戻り値/ロギング設計を広げるかどうかの判断が次に必要となる。
