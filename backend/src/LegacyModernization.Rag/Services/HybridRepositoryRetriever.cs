using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;

namespace LegacyModernization.Rag.Services;

public sealed class HybridRepositoryRetriever : IRepositoryRetriever
{
    private readonly SymbolAwareRepositoryRetriever _deterministic;
    private readonly LongLivedPythonRepositoryRetriever _enrichment;

    public HybridRepositoryRetriever(
        SymbolAwareRepositoryRetriever deterministic,
        LongLivedPythonRepositoryRetriever enrichment)
    {
        _deterministic = deterministic;
        _enrichment = enrichment;
    }

    public async Task<RetrievedContext> RetrieveAsync(
        AnalysisIssue issue,
        string projectRoot,
        CancellationToken cancellationToken = default)
    {
        var deterministic = await _deterministic.RetrieveAsync(issue, projectRoot, cancellationToken);
        var enrichment = await _enrichment.RetrieveAsync(issue, projectRoot, cancellationToken);
        var documents = deterministic.Documents
            .Concat(enrichment.Documents)
            .GroupBy(document => new
            {
                document.FilePath,
                document.LineStart,
                document.LineEnd,
                document.Content
            })
            .Select(group => group.First())
            .ToArray();

        return new RetrievedContext { Documents = documents };
    }
}
