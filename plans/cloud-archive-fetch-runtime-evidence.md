# クラウド配信アーカイブ取得機能 — Runtime Evidence

> 本ドキュメントは [cloud-archive-fetch.md](./cloud-archive-fetch.md) の Runtime Evidence として、主要シナリオの実行時シーケンスとシナリオ台帳を定義する。

---

## C4 Vocabulary（再掲・抜粋）

| ID | Kind | Formal Name | 役割 |
|---|---|---|---|
| Cmp-BrowsePage | Component | BrowsePage | 閲覧画面のエントリアクション（ログインして開く / Open Archive） |
| Cmp-Orchestrator | Component | CloudFetchOrchestrator | クラウド取得のメインオーケストレーション |
| Cmp-Bootstrap | Component | BootstrapConfigProvider | bootstrap.json 取得 |
| Cmp-MsalAuth | Component | MsalAuthService (ICloudAuthService) | MSAL 対話的認証・トークン取得 |
| Cmp-Manifest | Component | CloudManifestProvider | manifest.json 取得 |
| Cmp-Cache | Component | LocalCacheManager (ICacheManager) | ローカルキャッシュ管理 |
| Cmp-Downloader | Component | CloudArchiveDownloader | ZIP ダウンロード |
| Cmp-Hash | Component | Sha256Verifier (IHashVerifier) | SHA-256 検証 |
| Cmp-Workflow | Component | ArchiveWorkflowService | ソース取得→フォーマット検出→ロード→セッション設定→VM 更新 |
| Cmp-ZipSrc | Component | ZipArchiveSource | ZIP 展開・FolderArchiveSource 委譲 |
| Cmp-Session | Component | ArchiveSessionService | アーカイブセッション管理 |

---

## Scenario Details

### Scenario S-001: 初回ログイン実行（キャッシュなし・正常取得）

**Summary:** キャッシュが存在しない状態で「ログインして開く」を実行し、クラウドから ZIP を取得して表示する正常系フロー。
**Participants (C4 IDs):** Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Manifest, Cmp-Cache, Cmp-Downloader, Cmp-Hash, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session

#### Sequence (PlantUML)

```plantuml
@startuml S-001

title S-001: 初回ログイン実行（キャッシュなし・正常取得）

actor "User" as User
participant "Cmp_BrowsePage\n(BrowsePage)" as Cmp_BrowsePage
participant "Cmp_Orchestrator\n(CloudFetchOrchestrator)" as Cmp_Orchestrator
participant "Cmp_Bootstrap\n(BootstrapConfigProvider)" as Cmp_Bootstrap
participant "Cmp_MsalAuth\n(MsalAuthService)" as Cmp_MsalAuth
participant "Cmp_Manifest\n(CloudManifestProvider)" as Cmp_Manifest
participant "Cmp_Cache\n(LocalCacheManager)" as Cmp_Cache
participant "Cmp_Downloader\n(CloudArchiveDownloader)" as Cmp_Downloader
participant "Cmp_Hash\n(Sha256Verifier)" as Cmp_Hash
participant "Cmp_Workflow\n(ArchiveWorkflowService)" as Cmp_Workflow
participant "Cmp_ZipSrc\n(ZipArchiveSource)" as Cmp_ZipSrc
participant "Cmp_Session\n(ArchiveSessionService)" as Cmp_Session

== Main ==
User -> Cmp_BrowsePage : [E1] 「ログインして開く」押下
Cmp_BrowsePage -> Cmp_Orchestrator : [E2] FetchLatestAsync(progress, ct)
Cmp_Orchestrator -> Cmp_Bootstrap : [E3] GetConfigAsync(ct)
Cmp_Bootstrap --> Cmp_Orchestrator : [E4] BootstrapConfig
Cmp_Orchestrator -> Cmp_MsalAuth : [E5] AuthenticateAsync(config, ct)
Cmp_MsalAuth -> User : [E6] ブラウザ対話認証ダイアログ表示
User -> Cmp_MsalAuth : [E7] サインイン完了
Cmp_MsalAuth --> Cmp_Orchestrator : [E8] TokenCredential
Cmp_Orchestrator -> Cmp_Manifest : [E9] GetManifestAsync(config, credential, ct)
Cmp_Manifest --> Cmp_Orchestrator : [E10] CloudManifest
Cmp_Orchestrator -> Cmp_Cache : [E11] GetCurrentStateAsync(ct)
Cmp_Cache --> Cmp_Orchestrator : [E12] null（キャッシュなし）
Cmp_Orchestrator -> Cmp_Cache : [E13] GetTempDownloadPathAsync(ct)
Cmp_Cache --> Cmp_Orchestrator : [E14] tempPath
Cmp_Orchestrator -> Cmp_Downloader : [E15] DownloadAsync(manifest, credential, tempPath, ct)
Cmp_Downloader --> Cmp_Orchestrator : [E16] ダウンロード完了
Cmp_Orchestrator -> Cmp_Hash : [E17] VerifyAsync(tempPath, manifest.sha256, ct)
Cmp_Hash --> Cmp_Orchestrator : [E18] true（検証成功）
Cmp_Orchestrator -> Cmp_Cache : [E19] CommitDownloadAsync(tempPath, version, sha256, ct)
Cmp_Cache --> Cmp_Orchestrator : [E20] コミット完了
Cmp_Orchestrator --> Cmp_BrowsePage : [E21] CloudFetchResult(FreshDownload, zipPath)
Cmp_BrowsePage -> Cmp_Workflow : [E22] OpenCloudArchiveAsync(result)
Cmp_Workflow -> Cmp_ZipSrc : [E23] OpenAsync(zipPath)
Cmp_ZipSrc --> Cmp_Workflow : [E24] IArchiveSource
Cmp_Workflow -> Cmp_Workflow : [E25] フォーマット検出→ロード
Cmp_Workflow -> Cmp_Session : [E26] SetCurrentAsync(archive)
Cmp_Session --> Cmp_Workflow : [E27] セッション設定完了
Cmp_Workflow --> Cmp_BrowsePage : [E28] 完了
Cmp_BrowsePage --> User : [E29] アーカイブ表示

@enduml
```

#### Component–Step Map

| Component | Steps |
|---|---|
| Cmp-BrowsePage | E1, E22, E29 |
| Cmp-Orchestrator | E2, E4, E8, E10, E12, E14, E16, E18, E20, E21 |
| Cmp-Bootstrap | E3, E4 |
| Cmp-MsalAuth | E5, E6, E7, E8 |
| Cmp-Manifest | E9, E10 |
| Cmp-Cache | E11, E12, E13, E14, E19, E20 |
| Cmp-Downloader | E15, E16 |
| Cmp-Hash | E17, E18 |
| Cmp-Workflow | E22, E23, E25, E26, E28 |
| Cmp-ZipSrc | E23, E24 |
| Cmp-Session | E26, E27 |

---

### Scenario S-002: 2回目以降のログイン実行（キャッシュあり・最新版と同一）

**Summary:** キャッシュ済み ZIP が最新版と同一であり、ダウンロードをスキップしてキャッシュ ZIP を表示する。
**Participants (C4 IDs):** Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Manifest, Cmp-Cache, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session

#### Sequence (PlantUML)

```plantuml
@startuml S-002

title S-002: 2回目以降のログイン実行（キャッシュあり・最新版と同一）

actor "User" as User
participant "Cmp_BrowsePage\n(BrowsePage)" as Cmp_BrowsePage
participant "Cmp_Orchestrator\n(CloudFetchOrchestrator)" as Cmp_Orchestrator
participant "Cmp_Bootstrap\n(BootstrapConfigProvider)" as Cmp_Bootstrap
participant "Cmp_MsalAuth\n(MsalAuthService)" as Cmp_MsalAuth
participant "Cmp_Manifest\n(CloudManifestProvider)" as Cmp_Manifest
participant "Cmp_Cache\n(LocalCacheManager)" as Cmp_Cache
participant "Cmp_Workflow\n(ArchiveWorkflowService)" as Cmp_Workflow
participant "Cmp_ZipSrc\n(ZipArchiveSource)" as Cmp_ZipSrc
participant "Cmp_Session\n(ArchiveSessionService)" as Cmp_Session

== Main ==
User -> Cmp_BrowsePage : [E1] 「ログインして開く」押下
Cmp_BrowsePage -> Cmp_Orchestrator : [E2] FetchLatestAsync(progress, ct)
Cmp_Orchestrator -> Cmp_Bootstrap : [E3] GetConfigAsync(ct)
Cmp_Bootstrap --> Cmp_Orchestrator : [E4] BootstrapConfig
Cmp_Orchestrator -> Cmp_MsalAuth : [E5] AuthenticateAsync(config, ct)
note right : AcquireTokenSilent 成功\n（トークンキャッシュヒット）
Cmp_MsalAuth --> Cmp_Orchestrator : [E6] TokenCredential
Cmp_Orchestrator -> Cmp_Manifest : [E7] GetManifestAsync(config, credential, ct)
Cmp_Manifest --> Cmp_Orchestrator : [E8] CloudManifest
Cmp_Orchestrator -> Cmp_Cache : [E9] GetCurrentStateAsync(ct)
Cmp_Cache --> Cmp_Orchestrator : [E10] CacheState(version=同一)
Cmp_Orchestrator -> Cmp_Orchestrator : [E11] version 比較 → 同一、ダウンロードスキップ
Cmp_Orchestrator -> Cmp_Cache : [E12] GetCurrentZipPath()
Cmp_Cache --> Cmp_Orchestrator : [E13] current.zip パス
Cmp_Orchestrator --> Cmp_BrowsePage : [E14] CloudFetchResult(AlreadyUpToDate, zipPath)
Cmp_BrowsePage -> Cmp_Workflow : [E15] OpenCloudArchiveAsync(result)
Cmp_Workflow -> Cmp_ZipSrc : [E16] OpenAsync(zipPath)
Cmp_ZipSrc --> Cmp_Workflow : [E17] IArchiveSource
Cmp_Workflow -> Cmp_Workflow : [E18] フォーマット検出→ロード
Cmp_Workflow -> Cmp_Session : [E19] SetCurrentAsync(archive)
Cmp_Session --> Cmp_Workflow : [E20] セッション設定完了
Cmp_Workflow --> Cmp_BrowsePage : [E21] 完了
Cmp_BrowsePage --> User : [E22] アーカイブ表示

@enduml
```

#### Component–Step Map

| Component | Steps |
|---|---|
| Cmp-BrowsePage | E1, E15, E22 |
| Cmp-Orchestrator | E2, E4, E6, E8, E10, E11, E13, E14 |
| Cmp-Bootstrap | E3, E4 |
| Cmp-MsalAuth | E5, E6 |
| Cmp-Manifest | E7, E8 |
| Cmp-Cache | E9, E10, E12, E13 |
| Cmp-Workflow | E15, E16, E18, E19, E21 |
| Cmp-ZipSrc | E16, E17 |
| Cmp-Session | E19, E20 |

---

### Scenario S-003: ログイン実行時に新バージョン検出

**Summary:** キャッシュ済み版とは異なる新バージョンが manifest に存在し、新 ZIP をダウンロードして更新する。
**Participants (C4 IDs):** Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Manifest, Cmp-Cache, Cmp-Downloader, Cmp-Hash, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session

#### Sequence (PlantUML)

```plantuml
@startuml S-003

title S-003: ログイン実行時に新バージョン検出

actor "User" as User
participant "Cmp_BrowsePage\n(BrowsePage)" as Cmp_BrowsePage
participant "Cmp_Orchestrator\n(CloudFetchOrchestrator)" as Cmp_Orchestrator
participant "Cmp_Bootstrap\n(BootstrapConfigProvider)" as Cmp_Bootstrap
participant "Cmp_MsalAuth\n(MsalAuthService)" as Cmp_MsalAuth
participant "Cmp_Manifest\n(CloudManifestProvider)" as Cmp_Manifest
participant "Cmp_Cache\n(LocalCacheManager)" as Cmp_Cache
participant "Cmp_Downloader\n(CloudArchiveDownloader)" as Cmp_Downloader
participant "Cmp_Hash\n(Sha256Verifier)" as Cmp_Hash
participant "Cmp_Workflow\n(ArchiveWorkflowService)" as Cmp_Workflow
participant "Cmp_ZipSrc\n(ZipArchiveSource)" as Cmp_ZipSrc
participant "Cmp_Session\n(ArchiveSessionService)" as Cmp_Session

== Main ==
User -> Cmp_BrowsePage : [E1] 「ログインして開く」押下
Cmp_BrowsePage -> Cmp_Orchestrator : [E2] FetchLatestAsync(progress, ct)
Cmp_Orchestrator -> Cmp_Bootstrap : [E3] GetConfigAsync(ct)
Cmp_Bootstrap --> Cmp_Orchestrator : [E4] BootstrapConfig
Cmp_Orchestrator -> Cmp_MsalAuth : [E5] AuthenticateAsync(config, ct)
Cmp_MsalAuth --> Cmp_Orchestrator : [E6] TokenCredential
Cmp_Orchestrator -> Cmp_Manifest : [E7] GetManifestAsync(config, credential, ct)
Cmp_Manifest --> Cmp_Orchestrator : [E8] CloudManifest(version=新)
Cmp_Orchestrator -> Cmp_Cache : [E9] GetCurrentStateAsync(ct)
Cmp_Cache --> Cmp_Orchestrator : [E10] CacheState(version=旧)
Cmp_Orchestrator -> Cmp_Orchestrator : [E11] version 比較 → 不一致、ダウンロード要
Cmp_Orchestrator -> Cmp_Cache : [E12] GetTempDownloadPathAsync(ct)
Cmp_Cache --> Cmp_Orchestrator : [E13] tempPath
Cmp_Orchestrator -> Cmp_Downloader : [E14] DownloadAsync(manifest, credential, tempPath, ct)
Cmp_Downloader --> Cmp_Orchestrator : [E15] ダウンロード完了
Cmp_Orchestrator -> Cmp_Hash : [E16] VerifyAsync(tempPath, manifest.sha256, ct)
Cmp_Hash --> Cmp_Orchestrator : [E17] true（検証成功）
Cmp_Orchestrator -> Cmp_Cache : [E18] CommitDownloadAsync(tempPath, version, sha256, ct)
Cmp_Cache --> Cmp_Orchestrator : [E19] コミット完了
Cmp_Orchestrator --> Cmp_BrowsePage : [E20] CloudFetchResult(FreshDownload, zipPath)
Cmp_BrowsePage -> Cmp_Workflow : [E21] OpenCloudArchiveAsync(result)
Cmp_Workflow -> Cmp_ZipSrc : [E22] OpenAsync(zipPath)
Cmp_ZipSrc --> Cmp_Workflow : [E23] IArchiveSource
Cmp_Workflow -> Cmp_Workflow : [E24] フォーマット検出→ロード
Cmp_Workflow -> Cmp_Session : [E25] SetCurrentAsync(archive)
Cmp_Session --> Cmp_Workflow : [E26] セッション設定完了
Cmp_Workflow --> Cmp_BrowsePage : [E27] 完了
Cmp_BrowsePage --> User : [E28] アーカイブ表示（新バージョン）

@enduml
```

#### Component–Step Map

| Component | Steps |
|---|---|
| Cmp-BrowsePage | E1, E21, E28 |
| Cmp-Orchestrator | E2, E4, E6, E8, E10, E11, E13, E15, E17, E19, E20 |
| Cmp-Bootstrap | E3, E4 |
| Cmp-MsalAuth | E5, E6 |
| Cmp-Manifest | E7, E8 |
| Cmp-Cache | E9, E10, E12, E13, E18, E19 |
| Cmp-Downloader | E14, E15 |
| Cmp-Hash | E16, E17 |
| Cmp-Workflow | E21, E22, E24, E25, E27 |
| Cmp-ZipSrc | E22, E23 |
| Cmp-Session | E25, E26 |

---

### Scenario S-004: ネットワーク障害（キャッシュあり）

**Summary:** bootstrap.json 取得時にネットワークエラーが発生するが、前回取得済みキャッシュで表示を継続する。ステール警告を表示。
**Participants (C4 IDs):** Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-Cache, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session

#### Sequence (PlantUML)

```plantuml
@startuml S-004

title S-004: ネットワーク障害（キャッシュあり）

actor "User" as User
participant "Cmp_BrowsePage\n(BrowsePage)" as Cmp_BrowsePage
participant "Cmp_Orchestrator\n(CloudFetchOrchestrator)" as Cmp_Orchestrator
participant "Cmp_Bootstrap\n(BootstrapConfigProvider)" as Cmp_Bootstrap
participant "Cmp_Cache\n(LocalCacheManager)" as Cmp_Cache
participant "Cmp_Workflow\n(ArchiveWorkflowService)" as Cmp_Workflow
participant "Cmp_ZipSrc\n(ZipArchiveSource)" as Cmp_ZipSrc
participant "Cmp_Session\n(ArchiveSessionService)" as Cmp_Session

== Main ==
User -> Cmp_BrowsePage : [E1] 「ログインして開く」押下
Cmp_BrowsePage -> Cmp_Orchestrator : [E2] FetchLatestAsync(progress, ct)
Cmp_Orchestrator -> Cmp_Bootstrap : [E3] GetConfigAsync(ct)
Cmp_Bootstrap --> Cmp_Orchestrator : [E4] 例外（HttpRequestException）

== Fallback: キャッシュあり ==
Cmp_Orchestrator -> Cmp_Orchestrator : [E5] catch → トレースログに Exception.ToString() 出力
Cmp_Orchestrator -> Cmp_Cache : [E6] GetCurrentZipPath()
Cmp_Cache --> Cmp_Orchestrator : [E7] current.zip パス（存在）
Cmp_Orchestrator --> Cmp_BrowsePage : [E8] CloudFetchResult(StaleCache, zipPath)
Cmp_BrowsePage -> Cmp_Workflow : [E9] OpenCloudArchiveAsync(result)
Cmp_Workflow -> Cmp_ZipSrc : [E10] OpenAsync(zipPath)
Cmp_ZipSrc --> Cmp_Workflow : [E11] IArchiveSource
Cmp_Workflow -> Cmp_Workflow : [E12] フォーマット検出→ロード
Cmp_Workflow -> Cmp_Session : [E13] SetCurrentAsync(archive)
Cmp_Session --> Cmp_Workflow : [E14] セッション設定完了
Cmp_Workflow --> Cmp_BrowsePage : [E15] 完了
Cmp_BrowsePage --> User : [E16] アーカイブ表示 + ステール警告\n「最新取得に失敗したため前回取得済みを表示中」

@enduml
```

#### Component–Step Map

| Component | Steps |
|---|---|
| Cmp-BrowsePage | E1, E9, E16 |
| Cmp-Orchestrator | E2, E4, E5, E7, E8 |
| Cmp-Bootstrap | E3, E4 |
| Cmp-Cache | E6, E7 |
| Cmp-Workflow | E9, E10, E12, E13, E15 |
| Cmp-ZipSrc | E10, E11 |
| Cmp-Session | E13, E14 |

---

### Scenario S-005: ネットワーク障害（キャッシュなし）

**Summary:** bootstrap.json 取得時にネットワークエラーが発生し、キャッシュも存在しないためエラー表示のみ。
**Participants (C4 IDs):** Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-Cache

#### Sequence (PlantUML)

```plantuml
@startuml S-005

title S-005: ネットワーク障害（キャッシュなし）

actor "User" as User
participant "Cmp_BrowsePage\n(BrowsePage)" as Cmp_BrowsePage
participant "Cmp_Orchestrator\n(CloudFetchOrchestrator)" as Cmp_Orchestrator
participant "Cmp_Bootstrap\n(BootstrapConfigProvider)" as Cmp_Bootstrap
participant "Cmp_Cache\n(LocalCacheManager)" as Cmp_Cache

== Main ==
User -> Cmp_BrowsePage : [E1] 「ログインして開く」押下
Cmp_BrowsePage -> Cmp_Orchestrator : [E2] FetchLatestAsync(progress, ct)
Cmp_Orchestrator -> Cmp_Bootstrap : [E3] GetConfigAsync(ct)
Cmp_Bootstrap --> Cmp_Orchestrator : [E4] 例外（HttpRequestException）

== Fallback: キャッシュなし ==
Cmp_Orchestrator -> Cmp_Orchestrator : [E5] catch → トレースログに Exception.ToString() 出力
Cmp_Orchestrator -> Cmp_Cache : [E6] GetCurrentZipPath()
Cmp_Cache --> Cmp_Orchestrator : [E7] null（キャッシュなし）
Cmp_Orchestrator --> Cmp_BrowsePage : [E8] CloudFetchResult(NoCacheError, errorMessage)
Cmp_BrowsePage --> User : [E9] エラー表示\n「クラウドアーカイブの取得に失敗しました」

@enduml
```

#### Component–Step Map

| Component | Steps |
|---|---|
| Cmp-BrowsePage | E1, E9 |
| Cmp-Orchestrator | E2, E4, E5, E7, E8 |
| Cmp-Bootstrap | E3, E4 |
| Cmp-Cache | E6, E7 |

---

### Scenario S-006: SHA-256 不一致

**Summary:** ZIP ダウンロード完了後に SHA-256 検証が失敗する。キャッシュ有無によりフォールバックまたはエラーに分岐する。
**Participants (C4 IDs):** Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Manifest, Cmp-Cache, Cmp-Downloader, Cmp-Hash, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session

#### Sequence (PlantUML)

```plantuml
@startuml S-006

title S-006: SHA-256 不一致

actor "User" as User
participant "Cmp_BrowsePage\n(BrowsePage)" as Cmp_BrowsePage
participant "Cmp_Orchestrator\n(CloudFetchOrchestrator)" as Cmp_Orchestrator
participant "Cmp_Bootstrap\n(BootstrapConfigProvider)" as Cmp_Bootstrap
participant "Cmp_MsalAuth\n(MsalAuthService)" as Cmp_MsalAuth
participant "Cmp_Manifest\n(CloudManifestProvider)" as Cmp_Manifest
participant "Cmp_Cache\n(LocalCacheManager)" as Cmp_Cache
participant "Cmp_Downloader\n(CloudArchiveDownloader)" as Cmp_Downloader
participant "Cmp_Hash\n(Sha256Verifier)" as Cmp_Hash
participant "Cmp_Workflow\n(ArchiveWorkflowService)" as Cmp_Workflow
participant "Cmp_ZipSrc\n(ZipArchiveSource)" as Cmp_ZipSrc
participant "Cmp_Session\n(ArchiveSessionService)" as Cmp_Session

== Main（取得→hash 検証失敗） ==
User -> Cmp_BrowsePage : [E1] 「ログインして開く」押下
Cmp_BrowsePage -> Cmp_Orchestrator : [E2] FetchLatestAsync(progress, ct)
Cmp_Orchestrator -> Cmp_Bootstrap : [E3] GetConfigAsync(ct)
Cmp_Bootstrap --> Cmp_Orchestrator : [E4] BootstrapConfig
Cmp_Orchestrator -> Cmp_MsalAuth : [E5] AuthenticateAsync(config, ct)
Cmp_MsalAuth --> Cmp_Orchestrator : [E6] TokenCredential
Cmp_Orchestrator -> Cmp_Manifest : [E7] GetManifestAsync(config, credential, ct)
Cmp_Manifest --> Cmp_Orchestrator : [E8] CloudManifest
Cmp_Orchestrator -> Cmp_Cache : [E9] GetCurrentStateAsync(ct)
Cmp_Cache --> Cmp_Orchestrator : [E10] CacheState or null
Cmp_Orchestrator -> Cmp_Cache : [E11] GetTempDownloadPathAsync(ct)
Cmp_Cache --> Cmp_Orchestrator : [E12] tempPath
Cmp_Orchestrator -> Cmp_Downloader : [E13] DownloadAsync(manifest, credential, tempPath, ct)
Cmp_Downloader --> Cmp_Orchestrator : [E14] ダウンロード完了
Cmp_Orchestrator -> Cmp_Hash : [E15] VerifyAsync(tempPath, manifest.sha256, ct)
Cmp_Hash --> Cmp_Orchestrator : [E16] false（hash 不一致）
Cmp_Orchestrator -> Cmp_Orchestrator : [E17] 一時ファイル削除・トレースログ出力

== Variations ==
alt キャッシュに前回版あり
  Cmp_Orchestrator -> Cmp_Cache : [E18] GetCurrentZipPath()
  Cmp_Cache --> Cmp_Orchestrator : [E19] current.zip パス
  Cmp_Orchestrator --> Cmp_BrowsePage : [E20] CloudFetchResult(StaleCache, zipPath)
  Cmp_BrowsePage -> Cmp_Workflow : [E21] OpenCloudArchiveAsync(result)
  Cmp_Workflow -> Cmp_ZipSrc : [E22] OpenAsync(zipPath)
  Cmp_ZipSrc --> Cmp_Workflow : [E23] IArchiveSource
  Cmp_Workflow -> Cmp_Workflow : [E24] フォーマット検出→ロード
  Cmp_Workflow -> Cmp_Session : [E25] SetCurrentAsync(archive)
  Cmp_Session --> Cmp_Workflow : [E26] セッション設定完了
  Cmp_Workflow --> Cmp_BrowsePage : [E27] 完了
  Cmp_BrowsePage --> User : [E28] アーカイブ表示 + ステール警告
else キャッシュなし
  Cmp_Orchestrator -> Cmp_Cache : [E29] GetCurrentZipPath()
  Cmp_Cache --> Cmp_Orchestrator : [E30] null
  Cmp_Orchestrator --> Cmp_BrowsePage : [E31] CloudFetchResult(NoCacheError, errorMessage)
  Cmp_BrowsePage --> User : [E32] エラー表示
end

@enduml
```

#### Component–Step Map

| Component | Steps |
|---|---|
| Cmp-BrowsePage | E1, E21/E28/E32 |
| Cmp-Orchestrator | E2, E4, E6, E8, E10, E12, E14, E16, E17, E18–E20/E29–E31 |
| Cmp-Bootstrap | E3, E4 |
| Cmp-MsalAuth | E5, E6 |
| Cmp-Manifest | E7, E8 |
| Cmp-Cache | E9, E10, E11, E12, E18, E19/E29, E30 |
| Cmp-Downloader | E13, E14 |
| Cmp-Hash | E15, E16 |
| Cmp-Workflow | E21, E22, E24, E25, E27 |
| Cmp-ZipSrc | E22, E23 |
| Cmp-Session | E25, E26 |

---

### Scenario S-007: 認証失敗 / キャンセル

**Summary:** MSAL 対話認証でユーザーがキャンセルまたは認証エラーが発生する。キャッシュ有無によりフォールバックまたはエラーに分岐する。
**Participants (C4 IDs):** Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Cache, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session

#### Sequence (PlantUML)

```plantuml
@startuml S-007

title S-007: 認証失敗 / キャンセル

actor "User" as User
participant "Cmp_BrowsePage\n(BrowsePage)" as Cmp_BrowsePage
participant "Cmp_Orchestrator\n(CloudFetchOrchestrator)" as Cmp_Orchestrator
participant "Cmp_Bootstrap\n(BootstrapConfigProvider)" as Cmp_Bootstrap
participant "Cmp_MsalAuth\n(MsalAuthService)" as Cmp_MsalAuth
participant "Cmp_Cache\n(LocalCacheManager)" as Cmp_Cache
participant "Cmp_Workflow\n(ArchiveWorkflowService)" as Cmp_Workflow
participant "Cmp_ZipSrc\n(ZipArchiveSource)" as Cmp_ZipSrc
participant "Cmp_Session\n(ArchiveSessionService)" as Cmp_Session

== Main ==
User -> Cmp_BrowsePage : [E1] 「ログインして開く」押下
Cmp_BrowsePage -> Cmp_Orchestrator : [E2] FetchLatestAsync(progress, ct)
Cmp_Orchestrator -> Cmp_Bootstrap : [E3] GetConfigAsync(ct)
Cmp_Bootstrap --> Cmp_Orchestrator : [E4] BootstrapConfig
Cmp_Orchestrator -> Cmp_MsalAuth : [E5] AuthenticateAsync(config, ct)
Cmp_MsalAuth -> User : [E6] ブラウザ対話認証ダイアログ表示
User -> Cmp_MsalAuth : [E7] キャンセル / 認証エラー
Cmp_MsalAuth --> Cmp_Orchestrator : [E8] 例外（MsalUiRequiredException / OperationCanceledException）
Cmp_Orchestrator -> Cmp_Orchestrator : [E9] catch → トレースログに Exception.ToString() 出力

== Variations ==
alt キャッシュあり
  Cmp_Orchestrator -> Cmp_Cache : [E10] GetCurrentZipPath()
  Cmp_Cache --> Cmp_Orchestrator : [E11] current.zip パス
  Cmp_Orchestrator --> Cmp_BrowsePage : [E12] CloudFetchResult(StaleCache, zipPath)
  Cmp_BrowsePage -> Cmp_Workflow : [E13] OpenCloudArchiveAsync(result)
  Cmp_Workflow -> Cmp_ZipSrc : [E14] OpenAsync(zipPath)
  Cmp_ZipSrc --> Cmp_Workflow : [E15] IArchiveSource
  Cmp_Workflow -> Cmp_Workflow : [E16] フォーマット検出→ロード
  Cmp_Workflow -> Cmp_Session : [E17] SetCurrentAsync(archive)
  Cmp_Session --> Cmp_Workflow : [E18] セッション設定完了
  Cmp_Workflow --> Cmp_BrowsePage : [E19] 完了
  Cmp_BrowsePage --> User : [E20] アーカイブ表示 + ステール警告
else キャッシュなし
  Cmp_Orchestrator -> Cmp_Cache : [E21] GetCurrentZipPath()
  Cmp_Cache --> Cmp_Orchestrator : [E22] null
  Cmp_Orchestrator --> Cmp_BrowsePage : [E23] CloudFetchResult(NoCacheError, errorMessage)
  Cmp_BrowsePage --> User : [E24] エラー表示
end

@enduml
```

#### Component–Step Map

| Component | Steps |
|---|---|
| Cmp-BrowsePage | E1, E13/E20/E24 |
| Cmp-Orchestrator | E2, E4, E8, E9, E10–E12/E21–E23 |
| Cmp-Bootstrap | E3, E4 |
| Cmp-MsalAuth | E5, E6, E7, E8 |
| Cmp-Cache | E10, E11/E21, E22 |
| Cmp-Workflow | E13, E14, E16, E17, E19 |
| Cmp-ZipSrc | E14, E15 |
| Cmp-Session | E17, E18 |

---

### Scenario S-008: ローカルアーカイブとの併用

**Summary:** クラウドアーカイブ表示中にユーザーが手動で「Open Archive」を実行し、ローカルアーカイブに切り替える。
**Participants (C4 IDs):** Cmp-BrowsePage, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session

#### Sequence (PlantUML)

```plantuml
@startuml S-008

title S-008: ローカルアーカイブとの併用

actor "User" as User
participant "Cmp_BrowsePage\n(BrowsePage)" as Cmp_BrowsePage
participant "Cmp_Workflow\n(ArchiveWorkflowService)" as Cmp_Workflow
participant "Cmp_ZipSrc\n(ZipArchiveSource)" as Cmp_ZipSrc
participant "Cmp_Session\n(ArchiveSessionService)" as Cmp_Session

== 前提: クラウドアーカイブ表示中 ==
note over User, Cmp_Session : クラウドアーカイブが正常に読み込まれた状態（S-001/S-002/S-003 完了後）

== ローカルアーカイブ切り替え ==
User -> Cmp_BrowsePage : [E1] 「Open Archive」実行
Cmp_BrowsePage -> Cmp_BrowsePage : [E2] ファイルピッカーでローカル ZIP / フォルダ選択
User -> Cmp_BrowsePage : [E3] パス確定
Cmp_BrowsePage -> Cmp_Workflow : [E4] OpenLocalArchiveAsync(path)
Cmp_Workflow -> Cmp_ZipSrc : [E5] OpenAsync(localPath)
Cmp_ZipSrc --> Cmp_Workflow : [E6] IArchiveSource
Cmp_Workflow -> Cmp_Workflow : [E7] フォーマット検出→ロード
Cmp_Workflow -> Cmp_Session : [E8] SetCurrentAsync(archive)
Cmp_Session --> Cmp_Workflow : [E9] セッション設定完了（クラウドからローカルに切替）
Cmp_Workflow --> Cmp_BrowsePage : [E10] 完了
Cmp_BrowsePage --> User : [E11] ローカルアーカイブ表示\n（ステール警告は消去）

@enduml
```

#### Component–Step Map

| Component | Steps |
|---|---|
| Cmp-BrowsePage | E1, E2, E3, E11 |
| Cmp-Workflow | E4, E5, E7, E8, E10 |
| Cmp-ZipSrc | E5, E6 |
| Cmp-Session | E8, E9 |

---

## Scenario Ledger（シナリオ台帳）

| Scenario ID | 目的/価値（1行） | Given（前提） | When（トリガ） | Then（結果） | 参加者（Vocabulary ID） | 入出力/メッセージ | 例外・タイムアウト・リトライ | 観測点（ログ/メトリクス） |
|---|---|---|---|---|---|---|---|---|
| S-001 | 初回ログイン実行で ZIP を取得し表示 | キャッシュなし | 「ログインして開く」押下 | ZIP ダウンロード→検証→cache 保存→ビューア表示 | Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Manifest, Cmp-Cache, Cmp-Downloader, Cmp-Hash, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session | bootstrap.json→認証→manifest.json→ZIP / アーカイブ表示 | 各段階で例外→S-004/S-005/S-006/S-007 に分岐 | ログ: bootstrap 取得, 認証成功, manifest 取得, ダウンロード完了, hash 検証成功, cache コミット |
| S-002 | キャッシュヒットでスキップ表示 | キャッシュあり・version 同一 | 「ログインして開く」押下 | ダウンロードスキップ→キャッシュ ZIP でビューア表示 | Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Manifest, Cmp-Cache, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session | manifest.version == cache.version / AlreadyUpToDate | - | ログ: version 一致・スキップ |
| S-003 | 新 version の ZIP を更新取得 | キャッシュあり・version 不一致 | 「ログインして開く」押下 | 新 ZIP ダウンロード→検証→cache 更新→ビューア表示 | Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Manifest, Cmp-Cache, Cmp-Downloader, Cmp-Hash, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session | manifest.version != cache.version / FreshDownload | hash 不一致→S-006 に分岐 | ログ: version 不一致, ダウンロード開始, 更新完了 |
| S-004 | ネットワーク障害時のキャッシュフォールバック | キャッシュあり | 「ログインして開く」押下・bootstrap 取得失敗 | 前回キャッシュで表示 + ステール警告 | Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-Cache, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session | HttpRequestException / StaleCache | 例外 catch→トレースログ出力→キャッシュ使用 | ログ: bootstrap 取得失敗(Exception.ToString()), フォールバック実行, ステール警告 |
| S-005 | ネットワーク障害＋キャッシュなし→エラー | キャッシュなし | 「ログインして開く」押下・bootstrap 取得失敗 | エラー表示 | Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-Cache | HttpRequestException / NoCacheError | 例外 catch→トレースログ出力→エラー返却 | ログ: bootstrap 取得失敗(Exception.ToString()), NoCacheError |
| S-006 | SHA-256 不一致でダウンロード破棄 | ZIP ダウンロード完了 | hash 検証失敗 | 一時ファイル削除→キャッシュ有無で分岐 | Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Manifest, Cmp-Cache, Cmp-Downloader, Cmp-Hash, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session | hash mismatch / StaleCache or NoCacheError | 一時ファイル削除→トレースログ出力 | ログ: hash 不一致(期待値/実値), 一時ファイル削除 |
| S-007 | 認証失敗/キャンセルでの分岐 | bootstrap 取得成功 | 「ログインして開く」押下後の MSAL 認証キャンセル/エラー | キャッシュ有無で分岐 | Cmp-BrowsePage, Cmp-Orchestrator, Cmp-Bootstrap, Cmp-MsalAuth, Cmp-Cache, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session | MsalUiRequiredException or OperationCanceledException / StaleCache or NoCacheError | 例外 catch→トレースログ出力→キャッシュ有無で分岐 | ログ: 認証失敗(Exception.ToString()), フォールバック or NoCacheError |
| S-008 | ローカルアーカイブへの手動切り替え | クラウドアーカイブ表示中 | 「Open Archive」実行 | ローカルアーカイブに切り替わる | Cmp-BrowsePage, Cmp-Workflow, Cmp-ZipSrc, Cmp-Session | ローカルパス / アーカイブ切替表示 | ファイルピッカーキャンセル→操作中止 | ログ: ローカルアーカイブ切替 |
