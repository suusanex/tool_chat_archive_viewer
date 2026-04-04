namespace ChatArchiveViewer.App.Services;

public sealed class BundledSampleLocator : IBundledSampleLocator
{
    private static readonly string SampleFolderRelativePath = Path.Combine("Samples", "Sample Slack export");
    private static readonly string SampleZipRelativePath = Path.Combine("Samples", "Sample Slack export.zip");

    public BundledSampleLocator(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("Base directory is required.", nameof(baseDirectory));
        }

        var normalizedBaseDirectory = Path.GetFullPath(baseDirectory);
        SampleFolderPath = Path.GetFullPath(Path.Combine(normalizedBaseDirectory, SampleFolderRelativePath));
        SampleZipPath = Path.GetFullPath(Path.Combine(normalizedBaseDirectory, SampleZipRelativePath));
    }

    public string SampleFolderPath { get; }

    public string SampleZipPath { get; }

    public bool HasSampleFolder => Directory.Exists(SampleFolderPath);

    public bool HasSampleZip => File.Exists(SampleZipPath);
}
