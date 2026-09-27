using LegacyModernization.Api.Controllers;
using LegacyModernization.Api.Models;
using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;
using LegacyModernization.Rag.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyModernization.Api.Tests;

public sealed class ReviewControllerTests
{
    [Fact]
    public async Task Accept_AppliesSuggestionAndPersistsDecision()
    {
        using var repository = new TemporaryRepository("class Service { int Value => 1; }");
        var store = new InMemoryReviewDecisionStore();
        var controller = new ReviewController(store);

        var result = await controller.SaveDecision(repository.CreateRequest("approved"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("class Service { int Value => 2; }", await File.ReadAllTextAsync(repository.SourcePath));
        Assert.Equal("approved", Assert.Single(store.Decisions).Decision);
    }

    [Fact]
    public async Task Reject_PersistsDecisionWithoutChangingSource()
    {
        const string source = "class Service { int Value => 1; }";
        using var repository = new TemporaryRepository(source);
        var store = new InMemoryReviewDecisionStore();
        var controller = new ReviewController(store);

        var result = await controller.SaveDecision(repository.CreateRequest("rejected"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(source, await File.ReadAllTextAsync(repository.SourcePath));
        Assert.Equal("rejected", Assert.Single(store.Decisions).Decision);
    }

    [Fact]
    public async Task Reset_AfterAccept_RestoresOriginalSource()
    {
        const string source = "class Service { int Value => 1; }";
        using var repository = new TemporaryRepository(source);
        var controller = new ReviewController(new InMemoryReviewDecisionStore());

        Assert.IsType<OkObjectResult>(await controller.SaveDecision(repository.CreateRequest("approved"), CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controller.SaveDecision(repository.CreateRequest("pending"), CancellationToken.None));

        Assert.Equal(source, await File.ReadAllTextAsync(repository.SourcePath));
    }

    [Fact]
    public async Task Accept_RejectsAmbiguousSnippet()
    {
        using var repository = new TemporaryRepository("class Service { int First => 1; int Second => 1; }");
        var controller = new ReviewController(new InMemoryReviewDecisionStore());

        var result = await controller.SaveDecision(repository.CreateRequest("approved"), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    private sealed class InMemoryReviewDecisionStore : IReviewDecisionStore
    {
        public List<ReviewDecision> Decisions { get; } = [];

        public Task<ReviewDecision?> GetDecisionAsync(string findingFingerprint, CancellationToken cancellationToken = default) =>
            Task.FromResult(Decisions.FirstOrDefault(decision => decision.FindingFingerprint == findingFingerprint));

        public Task SaveDecisionAsync(ReviewDecision decision, CancellationToken cancellationToken = default)
        {
            Decisions.RemoveAll(existing => existing.FindingFingerprint == decision.FindingFingerprint);
            Decisions.Add(decision);
            return Task.CompletedTask;
        }
    }

    private sealed class TemporaryRepository : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), $"review-tests-{Guid.NewGuid():N}");

        public TemporaryRepository(string source)
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(Path.Combine(_root, "Review.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            SourcePath = Path.Combine(_root, "Service.cs");
            File.WriteAllText(SourcePath, source);
        }

        public string SourcePath { get; }

        public ReviewDecisionRequest CreateRequest(string decision)
        {
            const string original = "1";
            return new ReviewDecisionRequest
            {
                ProjectPath = Path.Combine(_root, "Review.csproj"),
                Decision = decision,
                Issue = new AnalysisIssue
                {
                    RuleId = "TEST001",
                    FilePath = SourcePath,
                    LineNumber = 1,
                    CodeSnippet = original
                },
                Suggestion = new RefactorSuggestion
                {
                    RuleId = "TEST001",
                    OriginalCode = original,
                    RefactoredCode = "2"
                }
            };
        }

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}