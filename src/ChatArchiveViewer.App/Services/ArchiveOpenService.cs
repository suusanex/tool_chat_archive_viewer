using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Services;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ChatArchiveViewer.App.Services;

public sealed class ArchiveOpenService : IArchiveOpenService
{
    private readonly IWindowProvider windowProvider;

    public ArchiveOpenService(IWindowProvider windowProvider)
    {
        this.windowProvider = windowProvider ?? throw new ArgumentNullException(nameof(windowProvider));
    }

    public async Task<IArchiveSource?> OpenFolderAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializePicker(picker);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
        {
            return null;
        }

        return new FolderArchiveSource(folder.Path);
    }

    public async Task<IArchiveSource?> OpenZipAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".zip");
        InitializePicker(picker);
        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return null;
        }

        return new ZipArchiveSource(file.Path);
    }

    private void InitializePicker(object picker)
    {
        var ownerWindow = windowProvider.CurrentWindow
            ?? throw new InvalidOperationException("Window is not initialized.");
        var hwnd = WindowNative.GetWindowHandle(ownerWindow);
        InitializeWithWindow.Initialize(picker, hwnd);
    }
}
