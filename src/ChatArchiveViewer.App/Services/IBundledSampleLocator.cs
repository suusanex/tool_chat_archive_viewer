namespace ChatArchiveViewer.App.Services;

public interface IBundledSampleLocator
{
    string SampleFolderPath { get; }

    string SampleZipPath { get; }

    bool HasSampleFolder { get; }

    bool HasSampleZip { get; }
}
