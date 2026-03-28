namespace ChatArchiveViewer.Core.Models;

public sealed class LoadDiagnostic
{
    public required DiagnosticSeverity Severity { get; init; }

    public required string Message { get; init; }

    public string? SourceHint { get; init; }
}

public enum DiagnosticSeverity
{
    Information,
    Warning,
    Error
}
