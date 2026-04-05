using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.Globalization;
using Windows.ApplicationModel;
using Windows.System.UserProfile;

namespace ChatArchiveViewer.App.Services;

internal static class StartupDiagnostics
{
    private static readonly CultureInfo JapaneseCulture = CultureInfo.GetCultureInfo("ja-JP");
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

    public static void LogStartupSnapshot(
        ILogger logger,
        AppLaunchOptions launchOptions,
        bool hasPackageIdentity,
        string phase)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(launchOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);

        logger.LogInformation(
            "Startup environment ({Phase}): HasPackageIdentity={HasPackageIdentity}, BaseDirectory={BaseDirectory}, CurrentDirectory={CurrentDirectory}, Framework={Framework}, OSDescription={OSDescription}, OSVersion={OSVersion}, OSArchitecture={OSArchitecture}, ProcessArchitecture={ProcessArchitecture}",
            phase,
            hasPackageIdentity,
            AppContext.BaseDirectory,
            Environment.CurrentDirectory,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            Environment.OSVersion.VersionString,
            RuntimeInformation.OSArchitecture,
            RuntimeInformation.ProcessArchitecture);

        logger.LogInformation(
            "Startup culture ({Phase}): CurrentCulture={CurrentCulture}, CurrentUICulture={CurrentUICulture}, DefaultThreadCurrentCulture={DefaultThreadCurrentCulture}, DefaultThreadCurrentUICulture={DefaultThreadCurrentUICulture}, InstalledUICulture={InstalledUICulture}, DebugPrimaryLanguageOverride={DebugPrimaryLanguageOverride}",
            phase,
            CultureInfo.CurrentCulture.Name,
            CultureInfo.CurrentUICulture.Name,
            CultureInfo.DefaultThreadCurrentCulture?.Name ?? "<null>",
            CultureInfo.DefaultThreadCurrentUICulture?.Name ?? "<null>",
            CultureInfo.InstalledUICulture.Name,
            launchOptions.DebugPrimaryLanguageOverride ?? "<null>");

        LogLanguagePreferences(logger, phase);
        LogResourceSnapshot(logger, phase);
        LogWindowsAppSdkSnapshot(logger, phase, hasPackageIdentity);
    }

    private static void LogLanguagePreferences(ILogger logger, string phase)
    {
        try
        {
            logger.LogInformation(
                "Language preference ({Phase}): GlobalizationPreferences.Languages={Languages}",
                phase,
                FormatList(GlobalizationPreferences.Languages));
        }
        catch (Exception ex)
        {
            LogDiagnosticException(logger, ex, "Failed to read GlobalizationPreferences.Languages.");
        }

        try
        {
            logger.LogInformation(
                "Language preference ({Phase}): ApplicationLanguages.Languages={Languages}, PrimaryLanguageOverride={PrimaryLanguageOverride}",
                phase,
                FormatList(ApplicationLanguages.Languages),
                string.IsNullOrWhiteSpace(ApplicationLanguages.PrimaryLanguageOverride)
                    ? "<null-or-empty>"
                    : ApplicationLanguages.PrimaryLanguageOverride);
        }
        catch (Exception ex)
        {
            LogDiagnosticException(logger, ex, "Failed to read ApplicationLanguages settings.");
        }
    }

    private static void LogResourceSnapshot(ILogger logger, string phase)
    {
        try
        {
            var jaPath = LocalizedStrings.ResolveResourcePath("ja-JP") ?? "<not-found>";
            var enPath = LocalizedStrings.ResolveResourcePath("en-US") ?? "<not-found>";
            var priPath = Path.Combine(AppContext.BaseDirectory, $"{AppIdentity.AssemblyName}.pri");
            var searchRoots = string.Join(" | ", LocalizedStrings.GetResourceSearchRoots());
            var jaBrowse = LocalizedStrings.Get("Nav.Browse", JapaneseCulture, "<missing>");
            var enBrowse = LocalizedStrings.Get("Nav.Browse", EnglishCulture, "<missing>");

            logger.LogInformation(
                "Localization resource ({Phase}): JaReswPath={JaReswPath}, EnReswPath={EnReswPath}, PriPath={PriPath}, PriExists={PriExists}",
                phase,
                jaPath,
                enPath,
                priPath,
                File.Exists(priPath));
            logger.LogInformation(
                "Localization probe ({Phase}): Nav.Browse.ja-JP={JaBrowse}, Nav.Browse.en-US={EnBrowse}",
                phase,
                jaBrowse,
                enBrowse);
            logger.LogInformation(
                "Localization resource roots ({Phase}): {SearchRoots}",
                phase,
                string.IsNullOrWhiteSpace(searchRoots) ? "<empty>" : searchRoots);
        }
        catch (Exception ex)
        {
            LogDiagnosticException(logger, ex, "Failed to read localization resource diagnostics.");
        }
    }

    private static void LogWindowsAppSdkSnapshot(ILogger logger, string phase, bool hasPackageIdentity)
    {
        try
        {
            var winUiVersion = typeof(Microsoft.UI.Xaml.Application).Assembly.GetName().Version?.ToString() ?? "<unknown>";
            var dynamicDependencyVersion = typeof(Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap).Assembly.GetName().Version?.ToString() ?? "<unknown>";

            logger.LogInformation(
                "WindowsAppSDK ({Phase}): WinUIAssemblyVersion={WinUiVersion}, DynamicDependencyAssemblyVersion={DynamicDependencyVersion}",
                phase,
                winUiVersion,
                dynamicDependencyVersion);
        }
        catch (Exception ex)
        {
            LogDiagnosticException(logger, ex, "Failed to read Windows App SDK assembly versions.");
        }

        if (!hasPackageIdentity)
        {
            logger.LogInformation("Windows package identity ({Phase}): <none>", phase);
            return;
        }

        try
        {
            var package = Package.Current;
            var version = package.Id.Version;
            logger.LogInformation(
                "Windows package identity ({Phase}): FullName={FullName}, FamilyName={FamilyName}, Version={Major}.{Minor}.{Build}.{Revision}",
                phase,
                package.Id.FullName,
                package.Id.FamilyName,
                version.Major,
                version.Minor,
                version.Build,
                version.Revision);
        }
        catch (Exception ex)
        {
            LogDiagnosticException(logger, ex, "Failed to read package identity.");
        }
    }

    private static void LogDiagnosticException(ILogger logger, Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        logger.LogError(exception, "{Message}", message);
        Trace.WriteLine(exception.ToString());
    }

    private static string FormatList(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var materialized = values.Where(static v => !string.IsNullOrWhiteSpace(v)).ToArray();
        return materialized.Length == 0 ? "<empty>" : string.Join(", ", materialized);
    }
}
