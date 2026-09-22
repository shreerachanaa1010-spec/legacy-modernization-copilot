namespace LegacyModernization.Rag.Models;

public sealed class RetrievedDocument
{
    public string EvidenceId { get; init; } = "";

    public string SourceType { get; init; } = "";

    public string RetrievalMethod { get; init; } = "deterministic";

    public double? Score { get; init; }

    public string FilePath { get; init; } = "";

    public string ContentHash { get; init; } = "";

    public string Namespace { get; init; } = "";

    public string Symbol { get; init; } = "";

    public string Content { get; init; } = "";

    public int? LineStart { get; init; }

    public int? LineEnd { get; init; }
}
