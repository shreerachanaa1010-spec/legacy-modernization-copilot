using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;

namespace LegacyModernization.Rag.Services;

public interface IRepositoryRetriever
{
    Task<RetrievedContext> RetrieveAsync(
        AnalysisIssue issue,
        string projectRoot,
        CancellationToken cancellationToken = default);
}
