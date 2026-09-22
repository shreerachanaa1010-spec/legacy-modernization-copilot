using LegacyModernization.Rag.Models;

namespace LegacyModernization.Rag.Services;

public interface IVectorStore
{
    Task UpsertAsync(VectorDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorDocument>> SearchAsync(
        IReadOnlyList<float> embedding,
        int limit,
        CancellationToken cancellationToken = default);
}