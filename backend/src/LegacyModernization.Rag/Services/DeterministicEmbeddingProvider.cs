using System.Security.Cryptography;

namespace LegacyModernization.Rag.Services;

public sealed class DeterministicEmbeddingProvider : IEmbeddingProvider
{
    public Task<IReadOnlyList<float>> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty));
        var embedding = bytes.Select(value => (float)(value / 255.0)).ToArray();
        return Task.FromResult<IReadOnlyList<float>>(embedding);
    }
}