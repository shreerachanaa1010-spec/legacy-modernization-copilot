using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using LegacyModernization.Rag.Models;
using Microsoft.Data.Sqlite;

namespace LegacyModernization.Rag.Services;

public sealed class SqliteVectorStore : IVectorStore, IAcceptedRefactoringStore
{
    private readonly string _connectionString;

    public SqliteVectorStore(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Pooling = false
        }.ToString();
        Initialize();
    }

    public async Task UpsertAsync(VectorDocument document, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var contentHash = string.IsNullOrWhiteSpace(document.Document.ContentHash)
            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document.Document.Content))).ToLowerInvariant()
            : document.Document.ContentHash;

        await using (var repositoryCommand = connection.CreateCommand())
        {
            repositoryCommand.Transaction = transaction;
            repositoryCommand.CommandText = "INSERT INTO repositories(repository_id, root_path, last_commit_sha, created_at) VALUES ($id, $root, $commit, $created) ON CONFLICT(repository_id) DO UPDATE SET root_path = excluded.root_path, last_commit_sha = excluded.last_commit_sha";
            repositoryCommand.Parameters.AddWithValue("$id", document.RepositoryId);
            repositoryCommand.Parameters.AddWithValue("$root", document.Document.FilePath);
            repositoryCommand.Parameters.AddWithValue("$commit", document.CommitSha);
            repositoryCommand.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
            await repositoryCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO chunks(
                chunk_id, repository_id, source_path, content, content_hash,
                symbol_name, namespace, source_type, line_start, line_end,
                     embedding_model, embedding_dim, embedding_version, embedding, indexed_at)
            VALUES($id, $repository, $path, $content, $hash, $symbol, $namespace,
                         $sourceType, $lineStart, $lineEnd, $model, $dimension, $version, $embedding, $indexed)
            ON CONFLICT(chunk_id) DO UPDATE SET
                content = excluded.content,
                content_hash = excluded.content_hash,
                symbol_name = excluded.symbol_name,
                namespace = excluded.namespace,
                source_type = excluded.source_type,
                line_start = excluded.line_start,
                line_end = excluded.line_end,
                embedding_model = excluded.embedding_model,
                embedding_dim = excluded.embedding_dim,
                embedding_version = excluded.embedding_version,
                embedding = excluded.embedding,
                indexed_at = excluded.indexed_at
            """;
        command.Parameters.AddWithValue("$id", document.Id);
        command.Parameters.AddWithValue("$repository", document.RepositoryId);
        command.Parameters.AddWithValue("$path", document.Document.FilePath);
        command.Parameters.AddWithValue("$content", document.Document.Content);
        command.Parameters.AddWithValue("$hash", contentHash);
        command.Parameters.AddWithValue("$symbol", document.Document.Symbol);
        command.Parameters.AddWithValue("$namespace", document.Document.Namespace);
        command.Parameters.AddWithValue("$sourceType", document.Document.SourceType);
        command.Parameters.AddWithValue("$lineStart", (object?)document.Document.LineStart ?? DBNull.Value);
        command.Parameters.AddWithValue("$lineEnd", (object?)document.Document.LineEnd ?? DBNull.Value);
        command.Parameters.AddWithValue("$model", document.EmbeddingModel);
        command.Parameters.AddWithValue("$dimension", document.Embedding.Count == 0 ? DBNull.Value : document.Embedding.Count);
        command.Parameters.AddWithValue("$version", document.EmbeddingVersion);
        command.Parameters.AddWithValue("$embedding", document.Embedding.Count == 0
            ? DBNull.Value
            : JsonSerializer.Serialize(document.Embedding));
        command.Parameters.AddWithValue("$indexed", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorDocument>> SearchAsync(
        IReadOnlyList<float> embedding,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT chunk_id, repository_id, source_path, content, content_hash, symbol_name, namespace, source_type, line_start, line_end, embedding_model, embedding_version, embedding FROM chunks WHERE embedding IS NOT NULL";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<(VectorDocument Document, double Score)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var storedEmbedding = JsonSerializer.Deserialize<float[]>(reader.GetString(12)) ?? [];
            var document = new RetrievedDocument
            {
                EvidenceId = reader.GetString(0),
                FilePath = reader.GetString(2),
                Content = reader.GetString(3),
                ContentHash = reader.GetString(4),
                Symbol = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Namespace = reader.IsDBNull(6) ? "" : reader.GetString(6),
                SourceType = reader.GetString(7),
                LineStart = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                LineEnd = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                RetrievalMethod = "sqlite-vector"
            };
            results.Add((new VectorDocument
            {
                Id = reader.GetString(0),
                RepositoryId = reader.GetString(1),
                Document = document,
                EmbeddingModel = reader.IsDBNull(10) ? "" : reader.GetString(10),
                EmbeddingVersion = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Embedding = storedEmbedding
            }, CosineSimilarity(embedding, storedEmbedding)));
        }

        return results.OrderByDescending(result => result.Score).Take(Math.Max(0, limit)).Select(result => new VectorDocument
        {
            Id = result.Document.Id,
            RepositoryId = result.Document.RepositoryId,
            EmbeddingModel = result.Document.EmbeddingModel,
            EmbeddingVersion = result.Document.EmbeddingVersion,
            Document = new RetrievedDocument
            {
                EvidenceId = result.Document.Document.EvidenceId,
                SourceType = result.Document.Document.SourceType,
                ContentHash = result.Document.Document.ContentHash,
                Namespace = result.Document.Document.Namespace,
                RetrievalMethod = "sqlite-vector",
                Score = result.Score,
                FilePath = result.Document.Document.FilePath,
                Symbol = result.Document.Document.Symbol,
                Content = result.Document.Document.Content,
                LineStart = result.Document.Document.LineStart,
                LineEnd = result.Document.Document.LineEnd
            },
            Embedding = result.Document.Embedding
        }).ToArray();
    }

    public async Task SaveAsync(
        string findingFingerprint,
        string ruleId,
        string originalCodeHash,
        string refactoredCode,
        string verificationStatus,
        CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO accepted_refactorings(
                finding_fingerprint, rule_id, original_code_hash,
                refactored_code, verification_status, created_at)
            VALUES($fingerprint, $rule, $originalHash, $refactored, $status, $created)
            ON CONFLICT(finding_fingerprint) DO UPDATE SET
                refactored_code = excluded.refactored_code,
                verification_status = excluded.verification_status,
                created_at = excluded.created_at
            """;
        command.Parameters.AddWithValue("$fingerprint", findingFingerprint);
        command.Parameters.AddWithValue("$rule", ruleId);
        command.Parameters.AddWithValue("$originalHash", originalCodeHash);
        command.Parameters.AddWithValue("$refactored", refactoredCode);
        command.Parameters.AddWithValue("$status", verificationStatus);
        command.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            CREATE TABLE IF NOT EXISTS repositories(
                repository_id TEXT PRIMARY KEY,
                root_path TEXT NOT NULL,
                last_commit_sha TEXT,
                created_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS chunks(
                chunk_id TEXT PRIMARY KEY,
                repository_id TEXT NOT NULL REFERENCES repositories(repository_id),
                source_path TEXT NOT NULL,
                content TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                symbol_name TEXT,
                namespace TEXT,
                source_type TEXT NOT NULL,
                line_start INTEGER,
                line_end INTEGER,
                embedding_model TEXT,
                embedding_dim INTEGER,
                embedding_version TEXT,
                embedding TEXT,
                indexed_at TEXT NOT NULL,
                UNIQUE(repository_id, source_path, content_hash, embedding_model));
            CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
                source_path, symbol_name, content,
                content='chunks', content_rowid='rowid');
            CREATE TABLE IF NOT EXISTS accepted_refactorings(
                finding_fingerprint TEXT PRIMARY KEY,
                rule_id TEXT NOT NULL,
                original_code_hash TEXT NOT NULL,
                refactored_code TEXT NOT NULL,
                verification_status TEXT NOT NULL,
                created_at TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();

        using var triggerCommand = connection.CreateCommand();
        triggerCommand.CommandText = """
            CREATE TRIGGER IF NOT EXISTS chunks_fts_insert AFTER INSERT ON chunks BEGIN
                INSERT INTO chunks_fts(rowid, source_path, symbol_name, content)
                VALUES(new.rowid, new.source_path, new.symbol_name, new.content);
            END;
            CREATE TRIGGER IF NOT EXISTS chunks_fts_delete AFTER DELETE ON chunks BEGIN
                INSERT INTO chunks_fts(chunks_fts, rowid, source_path, symbol_name, content)
                VALUES('delete', old.rowid, old.source_path, old.symbol_name, old.content);
            END;
            CREATE TRIGGER IF NOT EXISTS chunks_fts_update AFTER UPDATE ON chunks BEGIN
                INSERT INTO chunks_fts(chunks_fts, rowid, source_path, symbol_name, content)
                VALUES('delete', old.rowid, old.source_path, old.symbol_name, old.content);
                INSERT INTO chunks_fts(rowid, source_path, symbol_name, content)
                VALUES(new.rowid, new.source_path, new.symbol_name, new.content);
            END;
            """;
        triggerCommand.ExecuteNonQuery();
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var length = Math.Min(left.Count, right.Count);
        double dot = 0, leftMagnitude = 0, rightMagnitude = 0;
        for (var index = 0; index < length; index++)
        {
            dot += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        return leftMagnitude == 0 || rightMagnitude == 0
            ? 0
            : dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}