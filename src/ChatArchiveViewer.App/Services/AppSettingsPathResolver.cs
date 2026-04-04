using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Storage;

namespace ChatArchiveViewer.App.Services;

internal static class AppSettingsPathResolver
{
    private const string AppSettingsFileName = "appsettings.json";
    private const int ErrorInsufficientBuffer = 122;
    private const int AppModelErrorNoPackage = 15700;

    public static IReadOnlyList<string> GetConfigurationFilePaths()
    {
        var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, AppSettingsFileName);
        var paths = new List<string>
        {
            baseDirectoryPath
        };

        if (HasPackageIdentity())
        {
            var localStatePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, AppSettingsFileName);
            if (!string.Equals(baseDirectoryPath, localStatePath, StringComparison.OrdinalIgnoreCase))
            {
                paths.Add(localStatePath);
            }
        }

        return paths;
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
