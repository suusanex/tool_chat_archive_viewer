using Microsoft.UI.Xaml;

namespace ChatArchiveViewer.App.Services;

public sealed class WindowProvider : IWindowProvider
{
    public Window? CurrentWindow { get; set; }
}
