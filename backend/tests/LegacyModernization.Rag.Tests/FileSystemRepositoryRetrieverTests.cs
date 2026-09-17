using LegacyModernization.Analyzer.Models;
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

    private static string CreateTemporaryRepository()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rag-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }
}
