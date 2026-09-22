namespace LegacyModernization.Rag.Models;

public sealed class VectorDocument
{
    public string Id { get; init; } = "";

    public string RepositoryId { get; init; } = "default";

    public string CommitSha { get; init; } = "";

    public string EmbeddingModel { get; init; } = "";

    public string EmbeddingVersion { get; init; } = "";

    public RetrievedDocument Document { get; init; } = new();

    public IReadOnlyList<float> Embedding { get; init; } = [];
}