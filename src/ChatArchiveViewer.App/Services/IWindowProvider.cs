using Microsoft.UI.Xaml;

namespace ChatArchiveViewer.App.Services;

public interface IWindowProvider
{
    Window? CurrentWindow { get; set; }
}
