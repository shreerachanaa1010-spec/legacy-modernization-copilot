namespace LegacyModernization.Rag.Services;

public interface IRepositoryIndexer
{
    Task<IndexingResult> IndexAsync(
        string repositoryRoot,
        string repositoryId,
        CancellationToken cancellationToken = default);
}

public sealed record IndexingResult(int IndexedFiles, int SkippedFiles, int RemovedFiles);
