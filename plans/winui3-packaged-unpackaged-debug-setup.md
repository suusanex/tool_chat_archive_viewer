# WinUI 3 Packaged / Unpackaged 両対応デバッグ環境セットアップ — 問題と解決方法

## 概要

Single-project MSIX 構成の WinUI 3 アプリ（Windows App SDK 1.8 / .NET 10）において、  
Visual Studio から Packaged プロファイルと Unpackaged プロファイルの両方でデバッグ実行できるようにする際に発生した2つの問題と、それぞれの解決方法を記録する。

---

## 問題1: Visual Studio Rebuild で AppxManifest.xml が生成されず DEP1560 エラーが発生する

### 症状

- Visual Studio でリビルドすると `AppxManifest.xml` が生成されず、デプロイ時に `DEP1560` エラーが発生する。
- `dotnet msbuild` コマンドラインでは正常にビルド・デプロイできる。

### 原因

`ChatArchiveViewer.App.csproj` では以下の条件で `WindowsPackageType=None`（Unpackaged ビルド）を設定していた。

```xml
<WindowsPackageType Condition="...">None</WindowsPackageType>
```

コマンドラインでは `Platform=x64` 時に `PublishProfile=win-x64.pubxml` が解決されて `RuntimeIdentifier=win-x64` が確定し、その後の targets で `AppxPackage=true` などの Packaged 関連プロパティが設定されるため `WindowsPackageType=None` が適用されなかった。

一方、Visual Studio からのビルドでは `BuildingInsideVisualStudio=true` が設定されるが、`AppxPackage` や `Packaged` などの Packaged を示すプロパティが明示的に渡されなかった。  
その結果、Visual Studio ビルドでは常に `WindowsPackageType=None` が適用され、MSIX パッケージングに必要な `AppxManifest.xml` の生成処理がスキップされていた。

#### 失敗したアプローチ

`DeployOnBuild=true` を条件に `Packaged=true` / `AppxPackage=true` を設定することを試みたが、  
`WindowsPackageType=None` と `AppxPackage=true` が同時に評価されて矛盾が生じ、  
`Improper project configuration` エラーが発生したため却下した。

### 解決方法

`BuildingInsideVisualStudio=true` のときに `Packaged=true` を設定し、  
`WindowsPackageType=None` の適用条件に `Packaged != true` を追加した。

```xml
<!-- Visual Studio からのビルド時は Packaged モードとして扱う -->
<Packaged Condition="'$(Packaged)' == '' and '$(BuildingInsideVisualStudio)' == 'true'">true</Packaged>
<!-- Packaged / AppxPackage / GenerateAppxPackageOnBuild のいずれも指定されていない場合のみ Unpackaged にする -->
<WindowsPackageType Condition="'$(Packaged)' != 'true' and '$(GenerateAppxPackageOnBuild)' != 'true' and '$(AppxPackage)' != 'true'">None</WindowsPackageType>
```

これにより Visual Studio ビルド時は `WindowsPackageType` が `None` に設定されなくなり、  
MSIX パッケージング処理が正常に実行されて `AppxManifest.xml` が生成されるようになった。

---

## 問題2: 問題1の修正後、Unpackaged プロファイルでデバッグ起動すると COMException が発生する

### 症状

問題1の修正を適用した後、`launchSettings.json` の `commandName: "Project"` プロファイル（Unpackaged デバッグ）で起動すると、  
起動直後に以下の例外が発生してアプリが終了する。

```
COMException: 0x80040154 (REGDB_E_CLASSNOTREG)
```

スタックトレースは以下の経路だった。

```
WindowsAppRuntimeAutoInitializer.InitializeWindowsAppSDK  [ModuleInitializer]
  → DeploymentManagerCS.AutoInitialize.AccessWindowsAppSDK
    → DeploymentManagerCS.AutoInitialize.Options.get  ← 例外発生点
```

### 原因

Windows App SDK の自動初期化機能は、**ビルド時に決定された `WindowsPackageType` の値に基づいてコンパイル時 define constants が設定され**、実行時の初期化経路が固定される仕組みになっている。

- `WindowsPackageType=MSIX` → `MICROSOFT_WINDOWSAPPSDK_AUTOINITIALIZE_DEPLOYMENTMANAGER` が define される → `DeploymentManager.Initialize` を呼ぶ経路がコンパイルされる
- `WindowsPackageType=None` → `MICROSOFT_WINDOWSAPPSDK_AUTOINITIALIZE_BOOTSTRAP` が define される → `Bootstrap.TryInitialize` を呼ぶ経路がコンパイルされる

問題1の修正により、Visual Studio ビルドでは常に `Packaged=true` → `WindowsPackageType=MSIX` となるため、  
ビルド生成物には `DeploymentManager.Initialize` を呼ぶ自動初期化コードが組み込まれた。

一方、`launchSettings.json` の `commandName: "Project"`（Unpackaged プロファイル）は MSBuild には一切影響しない。  
そのため、Unpackaged として起動されたプロセスは package identity を持たないにもかかわらず、  
`DeploymentManager.Initialize` を呼ぼうとして `REGDB_E_CLASSNOTREG` で失敗した。

```
プロセスの package identity: なし（GetCurrentPackageFullName → APPMODEL_ERROR_NO_PACKAGE）
自動初期化が呼び出す経路: DeploymentManager（packaged 用）
結果: REGDB_E_CLASSNOTREG
```

### 解決方法

Windows App SDK の自動初期化を完全に無効化し、独自の `Main` エントリポイントで  
**実行時に** package identity の有無を判定して初期化経路を切り替える方式に変更した。

#### `.csproj` への追加設定

```xml
<!-- XAML が自動生成する Main を無効化し、独自 Program.cs の Main を有効にする -->
<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>
<!-- Windows App SDK の自動初期化を両方無効化する -->
<WindowsAppSdkBootstrapInitialize>false</WindowsAppSdkBootstrapInitialize>
<WindowsAppSdkDeploymentManagerInitialize>false</WindowsAppSdkDeploymentManagerInitialize>
```

#### `Program.cs` の新規作成

`kernel32.dll` の `GetCurrentPackageFullName` を呼び出し、戻り値で package identity の有無を判定する。

| 戻り値 | 意味 | 初期化経路 |
|--------|------|-----------|
| `ERROR_INSUFFICIENT_BUFFER` (122) | packaged（バッファが足りないだけで名前は取れる） | `DeploymentManager.Initialize` |
| `APPMODEL_ERROR_NO_PACKAGE` (15700) | unpackaged（package identity なし） | `Bootstrap.TryInitialize` |

```csharp
private static bool HasPackageIdentity()
{
    var packageFullNameLength = 0;
    var result = GetCurrentPackageFullName(ref packageFullNameLength, null);
    return result switch
    {
        ErrorInsufficientBuffer => true,   // packaged
        AppModelErrorNoPackage => false,    // unpackaged
        _ => throw new Win32Exception(result, $"GetCurrentPackageFullName failed with error {result}."),
    };
}
```

- packaged の場合: `DeploymentManager.Initialize` で Windows App Runtime のデプロイ状態を確認・修復する。
- unpackaged の場合: `Bootstrap.TryInitialize` で Windows App SDK の動的依存関係を解決する。

これにより、同一バイナリを Packaged / Unpackaged のどちらのプロファイルで起動しても、  
実行時に適切な初期化経路が選択されるようになった。

---

## まとめ

| # | 問題 | 原因 | 解決方法 |
|---|------|------|---------|
| 1 | VS Rebuild で `AppxManifest.xml` が生成されない（DEP1560） | VS ビルド時に Packaged を示すプロパティが渡されず、`WindowsPackageType=None` が適用されていた | `BuildingInsideVisualStudio=true` のとき `Packaged=true` を設定し、`WindowsPackageType=None` の条件を厳密化 |
| 2 | Unpackaged プロファイルで起動すると `REGDB_E_CLASSNOTREG` | 自動初期化経路がビルド時固定（`WindowsPackageType=MSIX` → DeploymentManager 経路）で、Unpackaged 起動時に不整合が発生 | 自動初期化を無効化し、独自 `Main` で実行時の package identity 判定により初期化経路を切り替える |

### 教訓

- Windows App SDK の自動初期化はビルド時の `WindowsPackageType` でコンパイル時に初期化経路が決定される。`launchSettings.json` の起動プロファイルはビルドプロセスには影響しない。
- Packaged / Unpackaged を同一バイナリで切り替えて使用するには、自動初期化を無効化した上で、実行時に package identity を判定して初期化を行う必要がある。
- Visual Studio の Configuration Manager の Deploy チェックは、MSBuild の `DeployOnBuild` プロパティとは別物であり、`DeployOnBuild` を条件に使うアプローチは機能しない。

### 関連ファイル

- `src/ChatArchiveViewer.App/ChatArchiveViewer.App.csproj` — `Packaged`、`WindowsPackageType`、自動初期化制御プロパティの設定
- `src/ChatArchiveViewer.App/Program.cs` — 独自 `Main`、実行時 package identity 判定、packaged/unpackaged 初期化分岐
- `src/ChatArchiveViewer.App/Properties/launchSettings.json` — `MsixPackage`（Packaged）/ `Project`（Unpackaged）プロファイル定義
