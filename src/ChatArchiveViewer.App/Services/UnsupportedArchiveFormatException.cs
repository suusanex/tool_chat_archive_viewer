namespace ChatArchiveViewer.App.Services;

public sealed class UnsupportedArchiveFormatException : Exception
{
    public UnsupportedArchiveFormatException()
        : base("No supported archive format was detected.")
    {
    }
}
