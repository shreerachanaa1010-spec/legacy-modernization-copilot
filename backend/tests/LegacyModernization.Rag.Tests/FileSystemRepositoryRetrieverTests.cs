using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;
using LegacyModernization.Rag.Services;
using System.Text.Json;
using Xunit;

namespace LegacyModernization.Rag.Tests;

public sealed class FileSystemRepositoryRetrieverTests
{
    [Fact]
    public async Task RetrieveAsync_ReturnsPrimarySourceAndNearbyTests()
    {
        var root = CreateTemporaryRepository();
        var sourcePath = Path.Combine(root, "CustomerService.cs");
        var testPath = Path.Combine(root, "CustomerServiceTests.cs");

        await File.WriteAllTextAsync(sourcePath, "class CustomerService { }");
        await File.WriteAllTextAsync(testPath, "class CustomerServiceTests { }");

        var issue = new AnalysisIssue
        {
            FilePath = sourcePath,
            CodeSnippet = "task.Result"
        };

        var context = await new FileSystemRepositoryRetriever()
            .RetrieveAsync(issue, root);

        Assert.Contains(context.Documents, document =>
            document.SourceType == "primary-source" &&
            document.Content.Contains("CustomerService", StringComparison.Ordinal));
        Assert.Contains(context.Documents, document =>
            document.SourceType == "related-test" &&
            document.Content.Contains("CustomerServiceTests", StringComparison.Ordinal));
        Assert.All(context.Documents, document => Assert.False(string.IsNullOrWhiteSpace(document.EvidenceId)));

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task RetrieveAsync_DoesNotReadIssueFileOutsideRepositoryRoot()
    {
        var root = CreateTemporaryRepository();
        var outsidePath = Path.Combine(Path.GetTempPath(), $"outside-{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(outsidePath, "class OutsideRepository { }");

        var issue = new AnalysisIssue
        {
            FilePath = outsidePath,
            CodeSnippet = "outside"
        };

        var context = await new FileSystemRepositoryRetriever()
            .RetrieveAsync(issue, root);

        Assert.Empty(context.Documents);

        Directory.Delete(root, recursive: true);
        File.Delete(outsidePath);
    }

    [Fact]
    public async Task PythonRetriever_ThrowsWhenRagScriptIsUnavailable()
    {
        var root = CreateTemporaryRepository();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LongLivedPythonRepositoryRetriever().RetrieveAsync(
                new AnalysisIssue { FilePath = Path.Combine(root, "CustomerService.cs") },
                root));

        Assert.Contains("agentic_rag.py was not found", exception.Message, StringComparison.Ordinal);
        Directory.Delete(root, recursive: true);
    }

        [Fact]
        public void PythonRetriever_ParsesNullOptionalEvidenceFields()
        {
                using var payload = JsonDocument.Parse("""
                        {
                            "evidence": [
                                {
                                    "evidence_id": "ev-1",
                                    "source_path": "src/Service.cs",
                                    "content": "class Service {}",
                                    "score": null,
                                    "line_start": null,
                                    "line_end": null
                                }
                            ]
                        }
                        """);

                var evidence = Assert.Single(LongLivedPythonRepositoryRetriever.ParseEvidence(payload));

                Assert.Equal("ev-1", evidence.EvidenceId);
                Assert.Null(evidence.Score);
                Assert.Null(evidence.LineStart);
                Assert.Null(evidence.LineEnd);
        }

    [Fact]
    public async Task SqliteVectorStore_PersistsAndRanksDocuments()
    {
        var root = CreateTemporaryRepository();
        var databasePath = Path.Combine(root, "rag.db");
        var store = new SqliteVectorStore(databasePath);
        var document = new RetrievedDocument
        {
            SourceType = "primary-source",
            RetrievalMethod = "deterministic-filesystem",
            FilePath = "CustomerService.cs",
            LineStart = 4,
            LineEnd = 8,
            Symbol = "CustomerService",
            Content = "class CustomerService { }"
        };

        await store.UpsertAsync(new VectorDocument
        {
            Id = "customer-service",
            Document = document,
            Embedding = [1, 0, 0]
        });

        var results = await store.SearchAsync([1, 0, 0], 1);

        var result = Assert.Single(results);
        Assert.Equal("CustomerService.cs", result.Document.FilePath);
        Assert.Equal("sqlite-vector", result.Document.RetrievalMethod);
        Assert.Equal(4, result.Document.LineStart);
        Assert.Equal(8, result.Document.LineEnd);
        Assert.Equal(1, result.Document.Score);

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task SymbolAwareRetriever_PopulatesContainingMethodAndLineRange()
    {
        var root = CreateTemporaryRepository();
        var sourcePath = Path.Combine(root, "CustomerService.cs");
        await File.WriteAllTextAsync(sourcePath, "namespace Demo;\nclass CustomerService\n{\n    void Process()\n    {\n    }\n}");

        var context = await new SymbolAwareRepositoryRetriever(new FileSystemRepositoryRetriever())
            .RetrieveAsync(new AnalysisIssue { FilePath = sourcePath, LineNumber = 5 }, root);

        var primary = Assert.Single(context.Documents, document => document.SourceType == "primary-source");
        Assert.Equal("CustomerService.Process", primary.Symbol);
        Assert.Equal(4, primary.LineStart);
        Assert.Equal(6, primary.LineEnd);

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task SqliteVectorStore_PersistsAcceptedRefactoring()
    {
        var root = CreateTemporaryRepository();
        var store = new SqliteVectorStore(Path.Combine(root, "rag.db"));

        await store.SaveAsync("finding-1", "LMC001", "original", "refactored", "BOTH_PASS");

        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(root, "rag.db"),
                Pooling = false
            }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT verification_status FROM accepted_refactorings WHERE finding_fingerprint = 'finding-1'";
        Assert.Equal("BOTH_PASS", (string?)await command.ExecuteScalarAsync());
        await connection.CloseAsync();

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task SqliteVectorStore_PersistsReviewDecision()
    {
        var root = CreateTemporaryRepository();
        var store = new SqliteVectorStore(Path.Combine(root, "rag.db"));
        var decision = new ReviewDecision
        {
            FindingFingerprint = "finding-2",
            Decision = "approved",
            FilePath = Path.Combine(root, "Service.cs"),
            OriginalCode = "client.Result",
            RefactoredCode = "await client",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await store.SaveDecisionAsync(decision);
        var persisted = await store.GetDecisionAsync("finding-2");

        Assert.NotNull(persisted);
        Assert.Equal("approved", persisted.Decision);
        Assert.Equal(decision.FilePath, persisted.FilePath);
        Assert.Equal(decision.OriginalCode, persisted.OriginalCode);
        Assert.Equal(decision.RefactoredCode, persisted.RefactoredCode);

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task SqliteRepositoryIndexer_IndexesCSharpFilesLocally()
    {
        var root = CreateTemporaryRepository();
        await File.WriteAllTextAsync(Path.Combine(root, "Service.cs"), "class Service { }");
        var store = new SqliteVectorStore(Path.Combine(root, "rag.db"));
        var result = await new SqliteRepositoryIndexer(store).IndexAsync(root, "repo-1");

        Assert.Equal(1, result.IndexedFiles);
        Directory.Delete(root, recursive: true);
    }

    private static string CreateTemporaryRepository()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rag-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }
}
