namespace LegacyModernization.Rag.Models;

public sealed class RetrievedDocument
{
    public string SourceType { get; init; } = "";

    public string FilePath { get; init; } = "";

    public string Content { get; init; } = "";

    public int? LineStart { get; init; }

    public int? LineEnd { get; init; }
}
