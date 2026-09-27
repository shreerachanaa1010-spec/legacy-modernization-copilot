using LegacyModernization.Rag.Models;

namespace LegacyModernization.Rag.Services;

public interface IReviewDecisionStore
{
    Task<ReviewDecision?> GetDecisionAsync(
        string findingFingerprint,
        CancellationToken cancellationToken = default);

    Task SaveDecisionAsync(
        ReviewDecision decision,
        CancellationToken cancellationToken = default);
}