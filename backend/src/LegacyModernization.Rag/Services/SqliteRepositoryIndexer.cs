using System.Security.Cryptography;
using System.Text;
using LegacyModernization.Rag.Models;

namespace LegacyModernization.Rag.Services;

public sealed class SqliteRepositoryIndexer : IRepositoryIndexer
{
    private readonly SqliteVectorStore _store;

    public SqliteRepositoryIndexer(SqliteVectorStore store)
    {
        _store = store;
    }

    public async Task<IndexingResult> IndexAsync(
        string repositoryRoot,
        string repositoryId,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(repositoryRoot);
        var activePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var indexed = 0;
        var skipped = 0;

        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase))
                     .Where(path => !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(root, path);
            activePaths.Add(relativePath);
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            var contentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
            var chunkId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{repositoryId}:{relativePath}:{contentHash}"))).ToLowerInvariant();

            await _store.UpsertAsync(new VectorDocument
            {
                Id = chunkId,
                RepositoryId = repositoryId,
                Document = new RetrievedDocument
                {
                    EvidenceId = chunkId[..16],
                    FilePath = relativePath,
                    Content = content,
                    ContentHash = contentHash,
                    SourceType = Path.GetFileName(path).Contains("test", StringComparison.OrdinalIgnoreCase)
                        ? "related-test"
                        : "related-source",
                    RetrievalMethod = "incremental-filesystem",
                    LineStart = 1,
                    LineEnd = content.Split('\n').Length
                }
            }, cancellationToken);
            indexed++;
        }

        await _store.PruneRepositoryAsync(repositoryId, activePaths, cancellationToken);
        return new IndexingResult(indexed, skipped, 0);
    }
}
