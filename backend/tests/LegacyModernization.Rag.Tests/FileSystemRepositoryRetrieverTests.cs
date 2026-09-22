using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;
using LegacyModernization.Rag.Services;
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

    private static string CreateTemporaryRepository()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rag-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }
}
