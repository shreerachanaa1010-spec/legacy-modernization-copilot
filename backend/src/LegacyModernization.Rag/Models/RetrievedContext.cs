namespace LegacyModernization.Rag.Models;

public sealed class RetrievedContext
{
    public IReadOnlyList<RetrievedDocument> Documents { get; init; } = [];

    public bool HasRequiredRagEvidence => Documents.Any(document =>
        document.RetrievalMethod == "python-hybrid-enrichment" &&
        !string.IsNullOrWhiteSpace(document.EvidenceId) &&
        !string.IsNullOrWhiteSpace(document.Content));
}
