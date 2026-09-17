namespace LegacyModernization.Rag.Models;

public sealed class RetrievedContext
{
    public IReadOnlyList<RetrievedDocument> Documents { get; init; } = [];
}
