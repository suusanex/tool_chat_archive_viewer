using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ChatArchiveViewer.App.Services;
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
        _ = args;

        WinRT.ComWrappersSupport.InitializeComWrappers();
        InitializeWindowsAppSdk();
        ApplicationCulture.ApplySupportedCulture();

        Application.Start(
            _ =>
            {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                var app = new App();
            });
    }

    private static void InitializeWindowsAppSdk()
    {
        try
        {
            if (HasPackageIdentity())
            {
                InitializePackagedWindowsAppSdk();
                return;
            }

            InitializeUnpackagedWindowsAppSdk();
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
            throw;
        }
    }

    private static void InitializePackagedWindowsAppSdk()
    {
        var options = new Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentInitializeOptions
        {
            OnErrorShowUI = true,
        };

        var deploymentResult = Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentManager.Initialize(options);
        if (deploymentResult.Status == Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentStatus.Ok)
        {
            return;
        }

        throw new InvalidOperationException(
            $"WindowsAppSDK deployment initialization failed with status '{deploymentResult.Status}' and HRESULT 0x{deploymentResult.ExtendedError.HResult:X8}.");
    }

    private static void InitializeUnpackagedWindowsAppSdk()
    {
        var majorMinorVersion = WindowsAppSdkReleaseMajorMinor;
        var versionTag = WindowsAppSdkReleaseVersionTag;
        var minVersion = new Microsoft.Windows.ApplicationModel.DynamicDependency.PackageVersion(
            WindowsAppSdkRuntimeVersion);
        var options = Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.InitializeOptions.OnNoMatch_ShowUI;

        if (Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.TryInitialize(majorMinorVersion, versionTag, minVersion, options, out var hr))
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
