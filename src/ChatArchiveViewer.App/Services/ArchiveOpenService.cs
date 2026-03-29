using System.Runtime.InteropServices;
using ChatArchiveViewer.Core.Abstractions;
using ChatArchiveViewer.Core.Services;
using WinRT.Interop;

namespace ChatArchiveViewer.App.Services;

public sealed class ArchiveOpenService : IArchiveOpenService
{
    private const int HResultCanceled = unchecked((int)0x800704C7);

    private readonly IWindowProvider windowProvider;
    private readonly IBundledSampleLocator bundledSampleLocator;

    public ArchiveOpenService(IWindowProvider windowProvider, IBundledSampleLocator bundledSampleLocator)
    {
        this.windowProvider = windowProvider ?? throw new ArgumentNullException(nameof(windowProvider));
        this.bundledSampleLocator = bundledSampleLocator ?? throw new ArgumentNullException(nameof(bundledSampleLocator));
    }

    public Task<IArchiveSource?> OpenFolderAsync(CancellationToken ct)
        => OpenFolderAsync(bundledSampleLocator.HasSampleFolder ? bundledSampleLocator.SampleFolderPath : null, ct);

    public Task<IArchiveSource?> OpenFolderAsync(string? initialFolderPath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var selectedPath = ShowFolderDialog(initialFolderPath);
        return Task.FromResult<IArchiveSource?>(selectedPath is null ? null : new FolderArchiveSource(selectedPath));
    }

    public Task<IArchiveSource?> OpenZipAsync(CancellationToken ct)
        => OpenZipAsync(bundledSampleLocator.HasSampleZip ? bundledSampleLocator.SampleZipPath : null, ct);

    public Task<IArchiveSource?> OpenZipAsync(string? initialZipPath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var selectedPath = ShowZipDialog(initialZipPath);
        return Task.FromResult<IArchiveSource?>(selectedPath is null ? null : new ZipArchiveSource(selectedPath));
    }

    public Task<IArchiveSource> OpenBundledSampleAsync(BundledSampleKind kind, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return kind switch
        {
            BundledSampleKind.Folder => Task.FromResult<IArchiveSource>(new FolderArchiveSource(bundledSampleLocator.SampleFolderPath)),
            BundledSampleKind.Zip => Task.FromResult<IArchiveSource>(new ZipArchiveSource(bundledSampleLocator.SampleZipPath)),
            _ => throw new InvalidOperationException($"Unsupported sample kind: {kind}")
        };
    }

    private string? ShowFolderDialog(string? initialFolderPath)
    {
        var dialog = CreateFileOpenDialog();
        try
        {
            dialog.GetOptions(out var options);
            dialog.SetOptions(options | FileOpenOptions.ForceFileSystem | FileOpenOptions.PathMustExist | FileOpenOptions.PickFolders);
            ApplyInitialSelection(dialog, initialFolderPath, isFolderDialog: true);
            var result = dialog.Show(GetOwnerWindowHandle());
            if (result == HResultCanceled)
            {
                return null;
            }

            Marshal.ThrowExceptionForHR(result);
            dialog.GetResult(out var selectedItem);
            try
            {
                selectedItem.GetDisplayName(ShellItemDisplayName.FileSystemPath, out var path);
                return path;
            }
            finally
            {
                Marshal.ReleaseComObject(selectedItem);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    private string? ShowZipDialog(string? initialZipPath)
    {
        var dialog = CreateFileOpenDialog();
        try
        {
            dialog.GetOptions(out var options);
            dialog.SetOptions(options | FileOpenOptions.ForceFileSystem | FileOpenOptions.PathMustExist | FileOpenOptions.FileMustExist);
            dialog.SetFileTypes(
                1,
                [new FilterSpec("ZIP archives (*.zip)", "*.zip")]);
            dialog.SetFileTypeIndex(1);
            ApplyInitialSelection(dialog, initialZipPath, isFolderDialog: false);
            var result = dialog.Show(GetOwnerWindowHandle());
            if (result == HResultCanceled)
            {
                return null;
            }

            Marshal.ThrowExceptionForHR(result);
            dialog.GetResult(out var selectedItem);
            try
            {
                selectedItem.GetDisplayName(ShellItemDisplayName.FileSystemPath, out var path);
                return path;
            }
            finally
            {
                Marshal.ReleaseComObject(selectedItem);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    private void ApplyInitialSelection(IFileOpenDialog dialog, string? initialPath, bool isFolderDialog)
    {
        if (string.IsNullOrWhiteSpace(initialPath))
        {
            return;
        }

        var targetPath = Path.GetFullPath(initialPath);
        var targetExists = isFolderDialog ? Directory.Exists(targetPath) : File.Exists(targetPath);
        if (!targetExists)
        {
            return;
        }

        var folderPath = isFolderDialog
            ? Directory.GetParent(targetPath)?.FullName
            : Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            folderPath = isFolderDialog ? targetPath : null;
        }

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        IShellItem? folderItem = null;
        try
        {
            folderItem = DialogInterop.CreateShellItem(folderPath);
            dialog.SetDefaultFolder(folderItem);
            dialog.SetFolder(folderItem);
            dialog.SetFileName(Path.GetFileName(targetPath));
        }
        finally
        {
            if (folderItem is not null)
            {
                Marshal.ReleaseComObject(folderItem);
            }
        }
    }

    private IntPtr GetOwnerWindowHandle()
    {
        var ownerWindow = windowProvider.CurrentWindow
            ?? throw new InvalidOperationException("Window is not initialized.");
        return WindowNative.GetWindowHandle(ownerWindow);
    }

    private static IFileOpenDialog CreateFileOpenDialog()
    {
        var dialogType = Type.GetTypeFromCLSID(new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7"))
            ?? throw new InvalidOperationException("FileOpenDialog COM type is unavailable.");
        return (IFileOpenDialog)Activator.CreateInstance(dialogType)!;
    }

    [ComImport]
    [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig]
        int Show(IntPtr parent);

        void SetFileTypes(uint count, [MarshalAs(UnmanagedType.LPArray)] FilterSpec[] filterSpec);

        void SetFileTypeIndex(uint index);

        void GetFileTypeIndex(out uint index);

        void Advise(IntPtr events, out uint cookie);

        void Unadvise(uint cookie);

        void SetOptions(FileOpenOptions options);

        void GetOptions(out FileOpenOptions options);

        void SetDefaultFolder(IShellItem item);

        void SetFolder(IShellItem item);

        void GetFolder(out IShellItem item);

        void GetCurrentSelection(out IShellItem item);

        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);

        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);

        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);

        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);

        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);

        void GetResult(out IShellItem item);

        void AddPlace(IShellItem item, int alignment);

        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string defaultExtension);

        void Close(int hr);

        void SetClientGuid(ref Guid guid);

        void ClearClientData();

        void SetFilter(IntPtr filter);

        void GetResults(IntPtr items);

        void GetSelectedItems(IntPtr items);
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr bindContext, ref Guid bhid, ref Guid riid, out IntPtr result);

        void GetParent(out IShellItem item);

        void GetDisplayName(ShellItemDisplayName sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string name);

        void GetAttributes(uint attributes, out uint result);

        void Compare(IShellItem item, uint hint, out int order);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private readonly struct FilterSpec(string name, string spec)
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public readonly string Name = name;

        [MarshalAs(UnmanagedType.LPWStr)]
        public readonly string Spec = spec;
    }

    [Flags]
    private enum FileOpenOptions : uint
    {
        FileMustExist = 0x00001000,
        PathMustExist = 0x00000800,
        ForceFileSystem = 0x00000040,
        PickFolders = 0x00000020
    }

    private enum ShellItemDisplayName : uint
    {
        FileSystemPath = 0x80058000
    }

    private static class DialogInterop
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            IntPtr bindContext,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

        public static IShellItem CreateShellItem(string path)
        {
            var itemGuid = typeof(IShellItem).GUID;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref itemGuid, out var shellItem);
            return shellItem;
        }
    }
}
