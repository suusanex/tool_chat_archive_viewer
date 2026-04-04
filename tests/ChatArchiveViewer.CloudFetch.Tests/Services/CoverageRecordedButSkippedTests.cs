namespace ChatArchiveViewer.CloudFetch.Tests.Services;

/// <summary>
/// このフェーズでは通常実行対象に含めない観点を、理由付きで記録する。
/// 外部 Azure/Entra 依存や負荷試験は Explicit として残す。
/// </summary>
[TestFixture]
public sealed class CoverageRecordedButSkippedTests
{
    [Test]
    [Explicit("Authenticated Azure Blob test environment is required for CloudManifestProvider success path.")]
    public void UT_IT_TP_C003__CloudManifestProvider_Success_RequiresAuthenticatedBlobEnvironment()
    {
        Assert.Inconclusive("Run against a dedicated authenticated Blob Storage test environment.");
    }

    [Test]
    [Explicit("Authenticated Azure Blob test environment is required for CloudManifestProvider failure-path verification.")]
    public void UT_IT_TP_C004__CloudManifestProvider_Failure_RequiresAuthenticatedBlobEnvironment()
    {
        Assert.Inconclusive("Run against a dedicated authenticated Blob Storage test environment.");
    }

    [Test]
    [Explicit("Interactive Entra ID sign-in and tenant configuration are required for MsalAuthService.")]
    public void UT_IT_TP_C005__MsalAuthService_RequiresInteractiveTenantEnvironment()
    {
        Assert.Inconclusive("Run in a tenant-enabled desktop environment with interactive sign-in.");
    }

    [Test]
    [Explicit("Authenticated Azure Blob test environment is required for CloudArchiveDownloader.")]
    public void UT_IT_TP_C006__CloudArchiveDownloader_RequiresAuthenticatedBlobEnvironment()
    {
        Assert.Inconclusive("Run against a dedicated authenticated Blob Storage test environment.");
    }

    [Test]
    [Explicit("Load/performance scenario. Use dedicated load or soak environment.")]
    public void UT_IT_TP_C028__LargeArchive_DownloadAndVerify_RequiresLoadEnvironment()
    {
        Assert.Inconclusive("Use a dedicated load/performance environment for this scenario.");
    }

    [Test]
    [Explicit("Soak scenario. Repeated execution should run outside normal CI.")]
    public void UT_IT_TP_C029__RepeatedFetch_RequiresSoakEnvironment()
    {
        Assert.Inconclusive("Use a dedicated soak environment for repeated fetch validation.");
    }

    [Test]
    [Explicit("Integration scenario. Interrupted-download recovery is timing-sensitive.")]
    public void UT_IT_TP_C030__InterruptedDownloadRecovery_RequiresIntegrationEnvironment()
    {
        Assert.Inconclusive("Verify interrupted download recovery in an integration environment.");
    }
}
