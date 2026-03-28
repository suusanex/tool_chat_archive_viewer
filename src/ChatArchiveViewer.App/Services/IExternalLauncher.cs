namespace ChatArchiveViewer.App.Services;

public interface IExternalLauncher
{
    Task<bool> LaunchUriAsync(Uri uri);
}
