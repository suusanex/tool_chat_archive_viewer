using ChatArchiveViewer.App.Services;

namespace ChatArchiveViewer.App;

internal static class AppIdentity
{
    public static string AppName => LocalizedStrings.Get("App.DisplayName", "Local Chat Archive Viewer");

    public const string Version = "1.0.0";

    public const string AssemblyName = "LocalChatArchiveViewer.App";
}
