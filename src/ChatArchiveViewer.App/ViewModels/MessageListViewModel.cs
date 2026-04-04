using CommunityToolkit.Mvvm.ComponentModel;

namespace ChatArchiveViewer.App.ViewModels;

public sealed partial class MessageListViewModel : ViewModelBase
{
    public ObservableCollection<MessageViewItem> Messages { get; } = new();

    [ObservableProperty]
    private string contextTitle = LocalizedStrings.Get("Browse.Context.SelectDate");

    public void SetMessages(string context, IReadOnlyList<MessageViewItem> messages)
    {
        ContextTitle = context;
        Messages.Clear();
        foreach (var message in messages)
        {
            Messages.Add(message);
        }
    }
}
