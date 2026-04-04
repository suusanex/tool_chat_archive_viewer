using Windows.System;

namespace ChatArchiveViewer.App.Services;

public sealed class ExternalLauncher : IExternalLauncher
{
    public Task<bool> LaunchUriAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return Launcher.LaunchUriAsync(uri).AsTask();
    }
}
