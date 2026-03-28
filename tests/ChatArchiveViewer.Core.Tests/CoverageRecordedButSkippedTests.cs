namespace ChatArchiveViewer.Core.Tests;

/// <summary>
/// 通常実行対象外として記録する観点のプレースホルダーテスト。
/// 負荷・統合・アプリレベル確認が必要な観点だけを対象にする。
/// </summary>
[TestFixture]
public sealed class CoverageRecordedButSkippedTests
{
    // TP-210a/b/c: 大量データの読み込み・日付ファイル・検索は通常CIでは負荷が高すぎる
    [Test]
    [Explicit("Load/performance scenario. Use dedicated load or soak environment.")]
    public void UT_IT_210abc__LargeArchive_LoadAndSearch_RequiresLoadEnvironment()
    {
        Assert.Inconclusive("Use a dedicated load/performance environment for this scenario.");
    }

    // TP-210d/e: 大量データ下の進捗報告とキャンセル応答性は負荷環境で測定する
    [Test]
    [Explicit("Load/performance scenario. Use dedicated load or soak environment.")]
    public void UT_IT_210de__LargeArchive_ProgressAndCancellation_RequiresLoadEnvironment()
    {
        Assert.Inconclusive("Use a dedicated load/performance environment for this scenario.");
    }

    // TP-310b/c: キャンセル後の解放確認はタイミング依存が強く統合検証向き
    [Test]
    [Explicit("Integration scenario. Needs timing-sensitive resource verification.")]
    public void UT_IT_310bc__CancellationCleanup_RequiresIntegrationVerification()
    {
        Assert.Inconclusive("Verify resource cleanup in an integration-level scenario.");
    }

    // TP-320b/c/d: アーカイブ切り替えはアプリ状態を含むためアプリレベルの検証向き
    [Test]
    [Explicit("Application-level scenario. Needs archive switching state verification.")]
    public void UT_IT_320bcd__ArchiveSwitching_RequiresApplicationLevelVerification()
    {
        Assert.Inconclusive("Verify repeated archive switching in an application-level test.");
    }
}
