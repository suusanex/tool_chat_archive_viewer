namespace ChatArchiveViewer.CloudArchiveUpdater.Services;

public sealed class ArchiveUpdaterOptions
{
    public required string AdditionalZipPath { get; init; }

    public static ArchiveUpdaterOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length != 1)
        {
            throw new ArgumentException("Usage: ChatArchiveViewer.CloudArchiveUpdater <additional-zip-path>");
        }

        var additionalZipPath = Path.GetFullPath(args[0]);
        if (!File.Exists(additionalZipPath))
        {
            throw new FileNotFoundException("The additional zip file was not found.", additionalZipPath);
        }

        return new ArchiveUpdaterOptions
        {
            AdditionalZipPath = additionalZipPath
        };
    }
}
