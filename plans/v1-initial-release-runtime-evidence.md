# v1 初版リリース — Runtime Evidence

> 本ドキュメントは [v1-initial-release.md](./v1-initial-release.md) の Runtime Evidence として、主要シナリオの実行時シーケンスとシナリオ台帳を定義する。

---

## C4 Vocabulary（再掲・抜粋）

| ID | Kind | Formal Name | 役割 |
|---|---|---|---|
| M-Shell | Component | AppShell | メインウインドウ・ナビゲーション |
| M-NavSvc | Component | NavigationService | ページ遷移 |
| M-OpenSvc | Component | ArchiveOpenService | アーカイブオープン |
| M-FormatReg | Component | FormatRegistry | 形式登録・検出 |
| M-LoadSvc | Component | ArchiveLoadService | 読み込みオーケストレーション |
| M-ZipSrc | Component | ZipArchiveSource | ZIP 入力ソース |
| M-FolderSrc | Component | FolderArchiveSource | フォルダ入力ソース |
| M-SlackFmt | Component | SlackFormatProvider | Slack 形式検出・パース |
| M-DateFilter | Component | DateFilterService | 日付フィルタ |
| M-Search | Component | SearchService | キーワード検索 |
| M-OverviewVM | Component | ArchiveOverviewVM | 概要情報 VM |
| M-BrowseVM | Component | ArchiveBrowseVM | list/details 閲覧 VM |
| M-MsgListVM | Component | MessageListVM | メッセージ一覧 VM |
| M-SearchVM | Component | SearchVM | 検索 VM |
| M-AboutVM | Component | AboutVM | About 画面 VM |
| M-SettingsVM | Component | SettingsVM | 設定画面 VM |
| X-Picker | External | WindowsFilePicker | OS ファイルピッカー |
| X-FS | External | FileSystem | ファイルシステム |
| X-Browser | External | DefaultBrowser | ブラウザ |
| X-TempDir | External | TempDirectory | 一時ディレクトリ |

---

## Scenario Ledger（シナリオ台帳）

| Scenario ID | Purpose | Given | When | Then | Participants | Input/Output | Exception/Timeout/Retry | Observation Points |
|---|---|---|---|---|---|---|---|---|
| S-010 | アプリ起動（初期状態） | アプリ未起動 | ユーザーがアプリを起動 | WelcomePage が表示される | M-Shell, M-NavSvc | -/WelcomePage | - | ログ: アプリ起動 |
| S-020 | ZIP アーカイブを開いて閲覧 | アプリ起動済み・アーカイブ未読み込み | ユーザーが ZIP ファイルを選択 | アーカイブが読み込まれ ArchiveBrowsePage に遷移 | M-Shell, M-OpenSvc, M-ZipSrc, M-FormatReg, M-SlackFmt, M-LoadSvc, M-OverviewVM, X-Picker, X-TempDir | ZIP パス / ChatArchive | ZIP 展開失敗→ErrorDialog, 形式検出失敗→ErrorDialog | ログ: ファイル数, チャンネル数, メッセージ数 |
| S-030 | フォルダアーカイブを開いて閲覧 | アプリ起動済み | ユーザーがフォルダを選択 | アーカイブが読み込まれ ArchiveBrowsePage に遷移 | M-Shell, M-OpenSvc, M-FolderSrc, M-FormatReg, M-SlackFmt, M-LoadSvc, M-OverviewVM, X-Picker | フォルダパス / ChatArchive | 形式検出失敗→ErrorDialog | ログ: ファイル数, チャンネル数, メッセージ数 |
| S-040 | チャンネル選択と日付選択（list/details） | アーカイブ読み込み済み | ユーザーがチャンネル一覧でチャンネルを選び、日付を選択 | 選択チャンネル・日付のメッセージがコンテンツ領域に表示される。BreadcrumbBar が更新される | M-BrowseVM, M-DateFilter, M-NavSvc | チャンネル+日付選択 / メッセージ一覧 | - | - |
| S-050 | メッセージ一覧のチャット風表示 | チャンネル・日付選択済み | コンテンツ領域にメッセージが表示される | メッセージがチャット風に時系列表示される | M-MsgListVM, M-SlackFmt, M-BrowseVM | チャンネルID+日付 / メッセージ一覧 | メッセージ読み込み失敗→診断表示 | ログ: メッセージ数 |
| S-055 | アダプティブレイアウト切り替え | ArchiveBrowsePage 表示中 | ウインドウ幅を変更 | 広い画面では 3 カラム同時表示、狭い画面では drill-down 型に切り替わる | M-BrowseVM, M-Shell | 画面幅変更 / レイアウト変更 | - | - |
| S-060 | キーワード検索 | アーカイブ読み込み済み | ユーザーがキーワードを入力し検索 | マッチするメッセージが一覧表示される | M-SearchVM, M-Search, M-SlackFmt | 検索キーワード / 検索結果 | 検索対象なし→空結果表示。空文字列は UI 上で検索実行不可（操作無効化） | ログ: 検索ヒット数 |
| S-070 | About 画面表示 | アプリ起動済み | ユーザーが About を選択 | About 情報が表示される | M-AboutVM, M-NavSvc | - / About 情報 | - | - |
| S-080 | プライバシーポリシー URL を開く | About 画面表示中 | ユーザーがプライバシーポリシーリンクをクリック | デフォルトブラウザで URL が開く | M-AboutVM, X-Browser | - / URL 起動 | URL 未設定→リンク非活性 | - |
| S-085 | テーマ設定の変更 | Settings 画面表示中 | ユーザーがテーマを変更する | テーマが即時反映され、再起動後も保持される | M-SettingsVM, M-Shell | テーマ選択 / テーマ反映 | 保存失敗→ErrorDialog | ログ: テーマ変更 |
| S-110 | 破損ファイルを含むアーカイブの読み込み | アプリ起動済み | 破損 JSON を含む Slack アーカイブを開く | 読み込み可能な部分が表示され、診断に破損情報が記録される | M-LoadSvc, M-SlackFmt, M-OverviewVM | 破損アーカイブ / 部分 ChatArchive + LoadDiagnostic | JSON パース失敗→診断記録・継続 | ログ: 破損ファイルパス（データ内容除く） |
| S-120 | 読み込みキャンセル | 読み込み中 | ユーザーがキャンセルを押下 | 読み込みが中止され WelcomePage に戻る | M-LoadSvc, M-Shell | CancellationToken | OperationCanceledException→正常中止 | ログ: キャンセル記録 |
| S-130 | 未対応形式のアーカイブを開く | アプリ起動済み | 未対応形式のフォルダを開く | 形式不明エラーが表示される | M-FormatReg, M-OpenSvc | フォルダパス / エラーメッセージ | 全形式検出失敗→ErrorDialog | ログ: 検出試行結果 |
| S-140 | 空チャンネル・空日付の取り扱い | アーカイブ読み込み済み | 空チャンネルや空日付を含むアーカイブ | 空チャンネルも一覧に表示。空日付は選択肢に出ない | M-SlackFmt, M-BrowseVM | 空データアーカイブ / 一覧（空含む） | - | - |

---

## Scenario Details

### Scenario S-010: アプリ起動（初期状態）

#### Sequence (PlantUML)

```plantuml
@startuml S-010
actor User
participant "M_Shell\n(AppShell)" as M_Shell
participant "M_NavSvc\n(NavigationService)" as M_NavSvc

User -> M_Shell : アプリ起動
activate M_Shell
M_Shell -> M_Shell : DI コンテナ初期化\nSerilog 初期化
M_Shell -> M_NavSvc : NavigateTo(WelcomePage)
activate M_NavSvc
M_NavSvc --> M_Shell : WelcomePage 表示
deactivate M_NavSvc
M_Shell --> User : WelcomePage\n（「アーカイブを開く」ボタン）
deactivate M_Shell
@enduml
```

#### Component–Step Map

| Step | M-Shell | M-NavSvc |
|---|---|---|
| 1. アプリ起動 | ● | |
| 2. DI・ログ初期化 | ● | |
| 3. WelcomePage へ遷移 | | ● |

---

### Scenario S-020: ZIP アーカイブを開いて閲覧

#### Sequence (PlantUML)

```plantuml
@startuml S-020
actor User
participant "M_Shell\n(AppShell)" as M_Shell
participant "M_OpenSvc\n(ArchiveOpenService)" as M_OpenSvc
participant "X_Picker\n(FilePicker)" as X_Picker
participant "M_ZipSrc\n(ZipArchiveSource)" as M_ZipSrc
participant "X_TempDir\n(TempDirectory)" as X_TempDir
participant "M_FormatReg\n(FormatRegistry)" as M_FormatReg
participant "M_SlackFmt\n(SlackFormatProvider)" as M_SlackFmt
participant "M_LoadSvc\n(ArchiveLoadService)" as M_LoadSvc
participant "M_OverviewVM\n(ArchiveOverviewVM)" as M_OverviewVM
participant "M_NavSvc\n(NavigationService)" as M_NavSvc

User -> M_Shell : 「アーカイブを開く」クリック
activate M_Shell
M_Shell -> M_OpenSvc : OpenArchiveAsync()
activate M_OpenSvc
M_OpenSvc -> X_Picker : PickFileAsync(filter: *.zip)
X_Picker --> M_OpenSvc : filePath

alt ZIP ファイル選択
    M_OpenSvc -> M_ZipSrc : Create(filePath)
    activate M_ZipSrc
    M_ZipSrc -> X_TempDir : 一時ディレクトリ作成
    M_ZipSrc -> M_ZipSrc : ZIP エントリ検証\n（zip slip 防御）
    M_ZipSrc -> X_TempDir : 安全に展開
    M_ZipSrc --> M_OpenSvc : IArchiveSource
    deactivate M_ZipSrc
end

M_OpenSvc -> M_FormatReg : DetectAllAsync(source)
activate M_FormatReg
M_FormatReg -> M_SlackFmt : DetectAsync(source)
activate M_SlackFmt
M_SlackFmt -> M_SlackFmt : ルート構成確認\nchannels.json / users.json 構造検証\n日次 JSON 形式ファイルの有無確認
M_SlackFmt --> M_FormatReg : FormatDetectionResult\n(Slack, confidence=0.9, root metadata + daily json detected)
deactivate M_SlackFmt
M_FormatReg --> M_OpenSvc : 検出結果リスト
deactivate M_FormatReg

M_OpenSvc -> M_LoadSvc : LoadAsync(source, formatId, progress, ct)
activate M_LoadSvc
M_LoadSvc -> M_SlackFmt : ParseAsync(source, progress, ct)
activate M_SlackFmt
M_SlackFmt -> M_SlackFmt : Root Inventory\nusers.json → Participant\nchannels.json → Conversation\n日次 JSON を列挙して AvailableDates / MessageCount 集計\n補助アーティファクトは件数のみ診断へ集約
M_SlackFmt --> M_LoadSvc : ChatArchive
deactivate M_SlackFmt
M_LoadSvc --> M_OpenSvc : ChatArchive
deactivate M_LoadSvc

M_OpenSvc --> M_Shell : ChatArchive
deactivate M_OpenSvc

M_Shell -> M_OverviewVM : SetArchive(archive)
M_Shell -> M_NavSvc : NavigateTo(ArchiveBrowsePage)
M_NavSvc --> User : ArchiveBrowsePage 表示（list/details 閲覧画面）
deactivate M_Shell
@enduml
```

#### Component–Step Map

| Step | M-Shell | M-OpenSvc | X-Picker | M-ZipSrc | X-TempDir | M-FormatReg | M-SlackFmt | M-LoadSvc | M-OverviewVM | M-NavSvc |
|---|---|---|---|---|---|---|---|---|---|---|
| 1. ユーザー操作 | ● | | | | | | | | | |
| 2. ファイルピッカー表示 | | ● | ● | | | | | | | |
| 3. ZIP 展開（安全検証） | | | | ● | ● | | | | | |
| 4. 形式自動検出 | | | | | | ● | ● | | | |
| 5. アーカイブパース | | | | | | | ● | ● | | |
| 6. 閲覧画面へ遷移 | ● | | | | | | | | ● | ● |

---

### Scenario S-030: フォルダアーカイブを開いて閲覧

#### Sequence (PlantUML)

```plantuml
@startuml S-030
actor User
participant "M_Shell\n(AppShell)" as M_Shell
participant "M_OpenSvc\n(ArchiveOpenService)" as M_OpenSvc
participant "X_Picker\n(FolderPicker)" as X_Picker
participant "M_FolderSrc\n(FolderArchiveSource)" as M_FolderSrc
participant "M_FormatReg\n(FormatRegistry)" as M_FormatReg
participant "M_SlackFmt\n(SlackFormatProvider)" as M_SlackFmt
participant "M_LoadSvc\n(ArchiveLoadService)" as M_LoadSvc
participant "M_OverviewVM\n(ArchiveOverviewVM)" as M_OverviewVM
participant "M_NavSvc\n(NavigationService)" as M_NavSvc

User -> M_Shell : 「フォルダを開く」クリック
activate M_Shell
M_Shell -> M_OpenSvc : OpenFolderArchiveAsync()
activate M_OpenSvc
M_OpenSvc -> X_Picker : PickFolderAsync()
X_Picker --> M_OpenSvc : folderPath
M_OpenSvc -> M_FolderSrc : Create(folderPath)
M_FolderSrc --> M_OpenSvc : IArchiveSource

M_OpenSvc -> M_FormatReg : DetectAllAsync(source)
M_FormatReg -> M_SlackFmt : DetectAsync(source)
M_SlackFmt --> M_FormatReg : FormatDetectionResult(Slack)\n(root metadata + daily json detected)
M_FormatReg --> M_OpenSvc : 検出結果リスト

M_OpenSvc -> M_LoadSvc : LoadAsync(source, formatId, progress, ct)
M_LoadSvc -> M_SlackFmt : ParseAsync(source, progress, ct)
M_SlackFmt -> M_SlackFmt : Root Inventory\nusers.json / channels.json 読込\n日次 JSON インデックス構築\n補助アーティファクトを診断へ記録
M_SlackFmt --> M_LoadSvc : ChatArchive
M_LoadSvc --> M_OpenSvc : ChatArchive
M_OpenSvc --> M_Shell : ChatArchive
deactivate M_OpenSvc

M_Shell -> M_OverviewVM : SetArchive(archive)
M_Shell -> M_NavSvc : NavigateTo(ArchiveBrowsePage)
M_NavSvc --> User : ArchiveBrowsePage 表示（list/details 閲覧画面）
deactivate M_Shell
@enduml
```

#### Component–Step Map

| Step | M-Shell | M-OpenSvc | X-Picker | M-FolderSrc | M-FormatReg | M-SlackFmt | M-LoadSvc | M-OverviewVM | M-NavSvc |
|---|---|---|---|---|---|---|---|---|---|
| 1. ユーザー操作 | ● | | | | | | | | |
| 2. フォルダピッカー表示 | | ● | ● | | | | | | |
| 3. フォルダソース生成 | | | | ● | | | | | |
| 4. 形式自動検出 | | | | | ● | ● | | | |
| 5. アーカイブパース | | | | | | ● | ● | | |
| 6. 閲覧画面へ遷移 | ● | | | | | | | ● | ● |

---

### Scenario S-040: チャンネル選択と日付選択（list/details）

#### Sequence (PlantUML)

```plantuml
@startuml S-040
actor User
participant "M_BrowseVM\n(ArchiveBrowseVM)" as M_BrowseVM
participant "M_DateFilter\n(DateFilterService)" as M_DateFilter
participant "M_MsgListVM\n(MessageListVM)" as M_MsgListVM

User -> M_BrowseVM : チャンネル一覧からチャンネルを選択
activate M_BrowseVM
M_BrowseVM -> M_BrowseVM : SelectedConversation を更新\nAvailableDates を年月グループ化して日付一覧に反映\nBreadcrumbBar を「Archive > #channel」に更新
M_BrowseVM --> User : 日付一覧が表示される

User -> M_BrowseVM : 日付を選択
M_BrowseVM -> M_BrowseVM : SelectedDate を更新\nBreadcrumbBar を「Archive > #channel > 2024-01-15」に更新
M_BrowseVM -> M_MsgListVM : LoadMessagesAsync(conversationId, date, ct)
activate M_MsgListVM
M_MsgListVM --> M_BrowseVM : メッセージ一覧
deactivate M_MsgListVM
M_BrowseVM --> User : コンテンツ領域にメッセージ表示

note right of M_BrowseVM
  広い画面: チャンネル・日付・コンテンツが同時表示
  狭い画面: 日付選択で drill-down し
            コンテンツ領域が前面に来る
end note
deactivate M_BrowseVM
@enduml
```

#### Component–Step Map

| Step | M-BrowseVM | M-DateFilter | M-MsgListVM |
|---|---|---|---|
| 1. チャンネル選択 | ● | | |
| 2. 日付一覧更新 | ● | ● | |
| 3. 日付選択 | ● | | |
| 4. メッセージ読み込み | | | ● |
| 5. BreadcrumbBar 更新 | ● | | |

---

### Scenario S-050: メッセージ一覧のチャット風表示

#### Sequence (PlantUML)

```plantuml
@startuml S-050
actor User
participant "M_BrowseVM\n(ArchiveBrowseVM)" as M_BrowseVM
participant "M_MsgListVM\n(MessageListVM)" as M_MsgListVM
participant "M_LoadSvc\n(ArchiveLoadService)" as M_LoadSvc
participant "M_SlackFmt\n(SlackArchiveParser)" as M_SlackFmt

User -> M_BrowseVM : チャンネル+日付が選択済み状態
activate M_BrowseVM
M_BrowseVM -> M_MsgListVM : LoadMessagesAsync(conversationId, date, ct)
activate M_MsgListVM
M_MsgListVM -> M_LoadSvc : LoadMessagesAsync(source, conversationId, date, ct)
activate M_LoadSvc
M_LoadSvc -> M_SlackFmt : LoadMessagesAsync(source, conversationId, date, ct)
activate M_SlackFmt
M_SlackFmt -> M_SlackFmt : <channel>/YYYY-MM-DD.json 読み込み\nJSON デシリアライズ\nthread_ts / edited / reactions / files / subtype を解釈\n汎用 ChatMessage へ変換
M_SlackFmt --> M_LoadSvc : IReadOnlyList<ChatMessage>
deactivate M_SlackFmt
M_LoadSvc --> M_MsgListVM : IReadOnlyList<ChatMessage>
deactivate M_LoadSvc

M_MsgListVM -> M_MsgListVM : Participant 情報とマージ\nタイムスタンプ順にソート\nスレッド関係の表示用グルーピング
M_MsgListVM --> M_BrowseVM : 表示用メッセージリスト
deactivate M_MsgListVM
M_BrowseVM --> User : コンテンツ領域にチャット風メッセージ一覧\n（発言者 | 日時 | 本文）
deactivate M_BrowseVM
@enduml
```

#### Component–Step Map

| Step | M-BrowseVM | M-MsgListVM | M-LoadSvc | M-SlackFmt |
|---|---|---|---|---|
| 1. メッセージ読み込み要求 | ● | ● | | |
| 2. Slack JSON パース | | | ● | ● |
| 3. 表示用データ構成 | | ● | | |
| 4. コンテンツ領域更新 | ● | | | |

---

### Scenario S-055: アダプティブレイアウト切り替え

#### Sequence (PlantUML)

```plantuml
@startuml S-055
actor User
participant "M_Shell\n(MainWindow)" as M_Shell
participant "M_BrowseVM\n(ArchiveBrowseVM)" as M_BrowseVM

User -> M_Shell : ウインドウ幅を変更
activate M_Shell
M_Shell -> M_Shell : VisualStateManager.AdaptiveTrigger 発火

alt 広い画面（≧900px）
    M_Shell -> M_BrowseVM : LayoutMode = ThreeColumn
    M_BrowseVM --> User : チャンネル・日付・コンテンツ同時表示
else 中程度（600〜900px）
    M_Shell -> M_BrowseVM : LayoutMode = TwoColumn
    M_BrowseVM --> User : 日付+コンテンツ表示。チャンネルは折りたたみペイン
else 狭い画面（＜600px）
    M_Shell -> M_BrowseVM : LayoutMode = SingleColumn
    M_BrowseVM --> User : 現在の選択階層を中心に表示。BreadcrumbBar で戻り操作
end
deactivate M_Shell

note right of M_BrowseVM
  レイアウト変更は VisualState 切り替えであり
  ページ遷移ではない。選択状態・スクロール位置は保持される。
end note
@enduml
```

#### Component–Step Map

| Step | M-Shell | M-BrowseVM |
|---|---|---|
| 1. ウインドウ幅変更検出 | ● | |
| 2. VisualState 切り替え | ● | |
| 3. レイアウトモード適用 | | ● |

---

### Scenario S-060: キーワード検索

#### Sequence (PlantUML)

```plantuml
@startuml S-060
actor User
participant "M_SearchVM\n(SearchVM)" as M_SearchVM
participant "M_Search\n(SearchService)" as M_Search
participant "M_SlackFmt\n(SlackArchiveParser)" as M_SlackFmt

User -> M_SearchVM : キーワード入力 + 検索実行
activate M_SearchVM
M_SearchVM -> M_Search : SearchAsync(archive, source, keyword, ct)
activate M_Search
M_Search -> M_SlackFmt : 各チャンネル・各日付のメッセージを順次読み込み
M_SlackFmt --> M_Search : メッセージリスト
M_Search -> M_Search : キーワードマッチング\n（大文字小文字非区別）
M_Search --> M_SearchVM : IReadOnlyList<SearchResult>\n（メッセージ+チャンネル+日付）
deactivate M_Search
M_SearchVM --> User : 検索結果一覧\n（チャンネル名・日付・該当メッセージ）
deactivate M_SearchVM
@enduml
```

#### Component–Step Map

| Step | M-SearchVM | M-Search | M-SlackFmt |
|---|---|---|---|
| 1. 検索開始 | ● | | |
| 2. メッセージ走査 | | ● | ● |
| 3. マッチング | | ● | |
| 4. 結果表示 | ● | | |

---

### Scenario S-070: About 画面表示

#### Sequence (PlantUML)

```plantuml
@startuml S-070
actor User
participant "M_NavSvc\n(NavigationService)" as M_NavSvc
participant "M_AboutVM\n(AboutVM)" as M_AboutVM

User -> M_NavSvc : About メニュー選択
M_NavSvc -> M_AboutVM : ページ表示
activate M_AboutVM
M_AboutVM -> M_AboutVM : AppInfo から情報取得:\n- アプリ名\n- バージョン\n- 非公式ビューア説明\n- 外部送信なし説明\n- 対応形式一覧\n- プライバシーポリシー URL\n- OSS URL\n- ライブラリ・ライセンス情報
M_AboutVM --> User : About 画面表示
deactivate M_AboutVM
@enduml
```

#### Component–Step Map

| Step | M-NavSvc | M-AboutVM |
|---|---|---|
| 1. ナビゲーション | ● | |
| 2. 情報取得・表示 | | ● |

---

### Scenario S-080: プライバシーポリシー URL を開く

#### Sequence (PlantUML)

```plantuml
@startuml S-080
actor User
participant "M_AboutVM\n(AboutVM)" as M_AboutVM
participant "X_Browser\n(DefaultBrowser)" as X_Browser

User -> M_AboutVM : プライバシーポリシーリンククリック
activate M_AboutVM
alt URL 設定済み
    M_AboutVM -> X_Browser : Launcher.LaunchUriAsync(privacyPolicyUrl)
    X_Browser --> User : ブラウザで URL を表示
else URL 未設定
    M_AboutVM --> User : リンク非活性（操作不可）
end
deactivate M_AboutVM
@enduml
```

#### Component–Step Map

| Step | M-AboutVM | X-Browser |
|---|---|---|
| 1. リンク押下判定 | ● | |
| 2. URL 起動 | | ● |

---

### Scenario S-085: テーマ設定の変更

#### Sequence (PlantUML)

```plantuml
@startuml S-085
actor User
participant "M_SettingsVM\n(SettingsVM)" as M_SettingsVM
participant "M_Shell\n(AppShell)" as M_Shell

User -> M_SettingsVM : テーマを選択（Light / Dark / Default）
activate M_SettingsVM
M_SettingsVM -> M_SettingsVM : 選択値を LocalSettings に保存
M_SettingsVM -> M_Shell : ApplyTheme(selectedTheme)
activate M_Shell
M_Shell --> User : アプリ全体のテーマを即時反映
deactivate M_Shell
M_SettingsVM --> User : 次回起動後も同じテーマを復元
deactivate M_SettingsVM
@enduml
```

#### Component–Step Map

| Step | M-SettingsVM | M-Shell |
|---|---|---|
| 1. テーマ選択 | ● | |
| 2. 設定保存 | ● | |
| 3. 即時反映 | | ● |
| 4. 次回起動への永続化 | ● | |

---

### Scenario S-110: 破損ファイルを含むアーカイブの読み込み

#### Sequence (PlantUML)

```plantuml
@startuml S-110
actor User
participant "M_LoadSvc\n(ArchiveLoadService)" as M_LoadSvc
participant "M_SlackFmt\n(SlackArchiveParser)" as M_SlackFmt
participant "M_OverviewVM\n(ArchiveOverviewVM)" as M_OverviewVM

User -> M_LoadSvc : LoadAsync(source, formatId, progress, ct)
activate M_LoadSvc
M_LoadSvc -> M_SlackFmt : ParseAsync(source, progress, ct)
activate M_SlackFmt

loop 各 JSON ファイル
    M_SlackFmt -> M_SlackFmt : JSON デシリアライズ試行
    alt 正常 JSON
        M_SlackFmt -> M_SlackFmt : ChatMessage へ変換
    else 破損 JSON
        M_SlackFmt -> M_SlackFmt : LoadDiagnostic(Warning) 記録\n※ファイル名のみ。データ内容はログ不可
        note right: 例外を catch し\nException.ToString() を\nトレースログ出力
    end
end

M_SlackFmt --> M_LoadSvc : ChatArchive\n（部分データ + Diagnostics）
deactivate M_SlackFmt
M_LoadSvc --> M_OverviewVM : ChatArchive
deactivate M_LoadSvc

M_OverviewVM --> User : 概要画面\n+ 診断メッセージ表示\n「N 件のファイルの読み込みに失敗しました」
@enduml
```

#### Component–Step Map

| Step | M-LoadSvc | M-SlackFmt | M-OverviewVM |
|---|---|---|---|
| 1. 読み込み開始 | ● | | |
| 2. 各ファイルパース | | ● | |
| 3. 破損検出・診断記録 | | ● | |
| 4. 部分結果返却 | ● | | |
| 5. 診断表示 | | | ● |

---

### Scenario S-120: 読み込みキャンセル

#### Sequence (PlantUML)

```plantuml
@startuml S-120
actor User
participant "M_Shell\n(AppShell)" as M_Shell
participant "M_LoadSvc\n(ArchiveLoadService)" as M_LoadSvc
participant "M_SlackFmt\n(SlackArchiveParser)" as M_SlackFmt
participant "M_ZipSrc\n(ZipArchiveSource)" as M_ZipSrc

User -> M_Shell : 読み込み中にキャンセル押下
M_Shell -> M_LoadSvc : CancellationTokenSource.Cancel()
activate M_LoadSvc
M_LoadSvc -> M_SlackFmt : ct.ThrowIfCancellationRequested()
M_SlackFmt --> M_LoadSvc : OperationCanceledException
M_LoadSvc -> M_ZipSrc : DisposeAsync()
activate M_ZipSrc
M_ZipSrc -> M_ZipSrc : 一時ディレクトリ削除
deactivate M_ZipSrc
M_LoadSvc --> M_Shell : OperationCanceledException
deactivate M_LoadSvc

M_Shell -> M_Shell : キャンセル完了処理\nWelcomePage へ復帰
M_Shell --> User : WelcomePage
@enduml
```

#### Component–Step Map

| Step | M-Shell | M-LoadSvc | M-SlackFmt | M-ZipSrc |
|---|---|---|---|---|
| 1. キャンセル要求 | ● | | | |
| 2. トークン確認・例外 | | ● | ● | |
| 3. リソースクリーンアップ | | | | ● |
| 4. 画面復帰 | ● | | | |

---

### Scenario S-130: 未対応形式のアーカイブを開く

#### Sequence (PlantUML)

```plantuml
@startuml S-130
actor User
participant "M_OpenSvc\n(ArchiveOpenService)" as M_OpenSvc
participant "M_FolderSrc\n(FolderArchiveSource)" as M_FolderSrc
participant "M_FormatReg\n(FormatRegistry)" as M_FormatReg
participant "M_SlackFmt\n(SlackFormatProvider)" as M_SlackFmt
participant "M_Shell\n(AppShell)" as M_Shell

User -> M_OpenSvc : OpenFolderArchiveAsync()
activate M_OpenSvc
M_OpenSvc -> M_FolderSrc : Create(folderPath)
M_FolderSrc --> M_OpenSvc : IArchiveSource

M_OpenSvc -> M_FormatReg : DetectAllAsync(source)
activate M_FormatReg
M_FormatReg -> M_SlackFmt : DetectAsync(source)
M_SlackFmt --> M_FormatReg : FormatDetectionResult(IsDetected=false)
M_FormatReg --> M_OpenSvc : 空の検出結果リスト
deactivate M_FormatReg

M_OpenSvc --> M_Shell : 形式検出失敗
deactivate M_OpenSvc
M_Shell --> User : エラーダイアログ\n「対応する形式が見つかりませんでした」
@enduml
```

#### Component–Step Map

| Step | M-OpenSvc | M-FolderSrc | M-FormatReg | M-SlackFmt | M-Shell |
|---|---|---|---|---|---|
| 1. フォルダオープン | ● | ● | | | |
| 2. 形式検出（全プロバイダ） | | | ● | ● | |
| 3. 検出失敗判定 | ● | | | | |
| 4. エラー表示 | | | | | ● |

---

### Scenario S-140: 空チャンネル・空日付の取り扱い

#### Sequence (PlantUML)

```plantuml
@startuml S-140
actor User
participant "M_SlackFmt\n(SlackArchiveParser)" as M_SlackFmt
participant "M_BrowseVM\n(ArchiveBrowseVM)" as M_BrowseVM
participant "M_MsgListVM\n(MessageListVM)" as M_MsgListVM

User -> M_SlackFmt : ParseAsync（空チャンネルを含むアーカイブ）
activate M_SlackFmt
M_SlackFmt -> M_SlackFmt : channels.json のチャンネル定義を読み込み\n空ディレクトリのチャンネルも Conversation に含める\nAvailableDates は空リスト\nMessageCount = 0
M_SlackFmt --> User : ChatArchive（空チャンネル含む）
deactivate M_SlackFmt

User -> M_BrowseVM : チャンネル一覧表示
activate M_BrowseVM
M_BrowseVM -> M_BrowseVM : 空チャンネルも一覧に表示\nMessageCount=0 を表示
M_BrowseVM --> User : チャンネル一覧（空含む）
deactivate M_BrowseVM

User -> M_MsgListVM : 空チャンネルを選択
activate M_MsgListVM
M_MsgListVM -> M_MsgListVM : メッセージ 0 件\n「このチャンネルにはメッセージがありません」表示
M_MsgListVM --> User : 空メッセージ画面
deactivate M_MsgListVM
@enduml
```

#### Component–Step Map

| Step | M-SlackFmt | M-BrowseVM | M-MsgListVM |
|---|---|---|---|
| 1. 空チャンネルのパース | ● | | |
| 2. 一覧での空表示 | | ● | |
| 3. 空メッセージ表示 | | | ● |
