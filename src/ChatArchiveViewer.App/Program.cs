using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ChatArchiveViewer.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace ChatArchiveViewer.App;

public static class Program
{
    private const int ErrorInsufficientBuffer = 122;
    private const int AppModelErrorNoPackage = 15700;
    private const uint WindowsAppSdkReleaseMajorMinor = 0x00010008;
    private const string WindowsAppSdkReleaseVersionTag = "";
    private const ulong WindowsAppSdkRuntimeVersion = 0x1F40032608CC0000;

    [STAThread]
    private static void Main(string[] args)
    {
        AppLogging.EnsureNLogConfigured();
        using var bootstrapLoggerFactory = AppLogging.CreateBootstrapLoggerFactory();
        var logger = bootstrapLoggerFactory.CreateLogger(nameof(Program));
        var launchOptions = AppLaunchOptions.Parse(args);
        var hasPackageIdentity = HasPackageIdentity();

        logger.LogInformation("Application startup requested.");
        StartupDiagnostics.LogStartupSnapshot(logger, launchOptions, hasPackageIdentity, "before-initialize");

        WinRT.ComWrappersSupport.InitializeComWrappers();
        InitializeWindowsAppSdk(hasPackageIdentity, logger);
        ApplyStartupCulture(launchOptions, logger);
        StartupDiagnostics.LogStartupSnapshot(logger, launchOptions, hasPackageIdentity, "after-apply-startup-culture");

        Application.Start(
            __ =>
            {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
    }

    private static void ApplyStartupCulture(AppLaunchOptions launchOptions, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(launchOptions);
        ArgumentNullException.ThrowIfNull(logger);
#if DEBUG
        if (!string.IsNullOrWhiteSpace(launchOptions.DebugPrimaryLanguageOverride))
        {
            var requested = CultureInfo.GetCultureInfo(launchOptions.DebugPrimaryLanguageOverride);
            var selected = ApplicationCulture.ApplySupportedCulture(requested);
            logger.LogInformation(
                "Startup culture applied with debug override. RequestedCulture={RequestedCulture}, SelectedCulture={SelectedCulture}",
                requested.Name,
                selected.Name);
            return;
        }
#endif
        var defaultSelected = ApplicationCulture.ApplySupportedCulture();
        logger.LogInformation(
            "Startup culture applied from CurrentUICulture. SelectedCulture={SelectedCulture}",
            defaultSelected.Name);
    }

    private static void InitializeWindowsAppSdk(bool hasPackageIdentity, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            if (hasPackageIdentity)
            {
                logger.LogInformation("WindowsAppSDK initialization path: packaged.");
                InitializePackagedWindowsAppSdk(logger);
                return;
            }

            logger.LogInformation("WindowsAppSDK initialization path: unpackaged.");
            InitializeUnpackagedWindowsAppSdk(logger);
        }
        catch (Exception ex)
        {
            logger.LogError("WindowsAppSDK initialization failed. Exception={Exception}", ex.ToString());
            Trace.WriteLine(ex.ToString());
            throw;
        }
    }

    private static void InitializePackagedWindowsAppSdk(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var options = new Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentInitializeOptions
        {
            OnErrorShowUI = true,
        };

        var deploymentResult = Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentManager.Initialize(options);
        logger.LogInformation(
            "WindowsAppSDK packaged initialize result: Status={Status}, ExtendedHResult=0x{HResult:X8}",
            deploymentResult.Status,
            deploymentResult.ExtendedError.HResult);
        if (deploymentResult.Status == Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentStatus.Ok)
        {
            return;
        }

        throw new InvalidOperationException(
            $"WindowsAppSDK deployment initialization failed with status '{deploymentResult.Status}' and HRESULT 0x{deploymentResult.ExtendedError.HResult:X8}.");
    }

    private static void InitializeUnpackagedWindowsAppSdk(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var majorMinorVersion = WindowsAppSdkReleaseMajorMinor;
        var versionTag = WindowsAppSdkReleaseVersionTag;
        var minVersion = new Microsoft.Windows.ApplicationModel.DynamicDependency.PackageVersion(
            WindowsAppSdkRuntimeVersion);
        var options = Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.InitializeOptions.OnNoMatch_ShowUI;
        logger.LogInformation(
            "WindowsAppSDK unpackaged initialize request: MajorMinor=0x{MajorMinor:X8}, VersionTag={VersionTag}, MinVersion={MinVersion}, Options={Options}",
            majorMinorVersion,
            string.IsNullOrWhiteSpace(versionTag) ? "<empty>" : versionTag,
            minVersion.ToString(),
            options);

        var initialized = Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.TryInitialize(
            majorMinorVersion,
            versionTag,
            minVersion,
            options,
            out var hr);
        logger.LogInformation(
            "WindowsAppSDK unpackaged initialize result: Initialized={Initialized}, HResult=0x{HResult:X8}",
            initialized,
            hr);
        if (initialized)
        {
            return;
        }

        throw new Win32Exception(hr, $"WindowsAppSDK bootstrap initialization failed with HRESULT 0x{hr:X8}.");
    }

    private static bool HasPackageIdentity()
    {
        var packageFullNameLength = 0;
        var result = GetCurrentPackageFullName(ref packageFullNameLength, null);
        return result switch
        {
            ErrorInsufficientBuffer => true,
            AppModelErrorNoPackage => false,
            _ => throw new Win32Exception(result, $"GetCurrentPackageFullName failed with error {result}."),
        };
    }

    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
