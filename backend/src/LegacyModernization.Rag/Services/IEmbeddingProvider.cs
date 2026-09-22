namespace LegacyModernization.Rag.Services;

public interface IEmbeddingProvider
{
    Task<IReadOnlyList<float>> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}