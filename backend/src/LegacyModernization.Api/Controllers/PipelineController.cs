using LegacyModernization.Analyzer.Services;
using LegacyModernization.Api.Models;
using LegacyModernization.Api.Services;
using LegacyModernization.Core.Models;
using LegacyModernization.LLM.Services;
using LegacyModernization.Rag.Models;
using LegacyModernization.Rag.Services;
using LegacyModernization.TestGenerator.Services;
using LegacyModernization.Verifier.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace LegacyModernization.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PipelineController : ControllerBase
{
    private readonly IProjectAnalyzer _analyzer;
    private readonly ILlmService _llmService;
    private readonly ITestGenerator _testGenerator;
    private readonly IRepositoryRetriever _retriever;
    private readonly IAcceptedRefactoringStore _acceptedRefactorings;
    private readonly VerificationService _verifier;

    public PipelineController(
        IProjectAnalyzer analyzer,
        ILlmService llmService,
        ITestGenerator testGenerator,
        IRepositoryRetriever retriever,
        IAcceptedRefactoringStore acceptedRefactorings,
        VerificationService verifier)
    {
        _analyzer = analyzer;
        _llmService = llmService;
        _testGenerator = testGenerator;
        _retriever = retriever;
        _acceptedRefactorings = acceptedRefactorings;
        _verifier = verifier;
    }

    /// <summary>
    /// Run the full pipeline: analyze → suggest → verify.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RunPipeline([FromBody] PipelineRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectPath))
            return BadRequest("ProjectPath is required.");

        var projectFullPath = ProjectPathResolver.Resolve(request.ProjectPath);

        if (projectFullPath is null)
            return NotFound($"No unique .csproj file was found for project path: {request.ProjectPath}");

        // Step 1: Analyze
        var analysis = await _analyzer.AnalyzeAsync(projectFullPath);
        var projectRoot = Path.GetDirectoryName(projectFullPath)!;

        // Step 2: Generate suggestions (parallel for speed)
        var suggestionTasks = analysis.Issues.Select(async issue =>
        {
            try
            {
                return await _llmService.GenerateSuggestionAsync(
                    issue,
                    await _retriever.RetrieveAsync(issue, projectRoot));
            }
            catch (Exception ex)
            {
                return new RefactorSuggestion
                {
                    RuleId = issue.RuleId,
                    IssueTitle = issue.Title,
                    Reason = issue.Description,
                    OriginalCode = issue.CodeSnippet,
                    RefactoredCode = "",
                    Explanation = $"Suggestion generation failed safely: {ex.GetBaseException().Message}",
                    GenerationStatus = "generation-error",
                    IsSafe = false
                };
            }
        });
        var suggestions = (await Task.WhenAll(suggestionTasks)).ToList();

        // Step 3: Generate tests for each issue
        var generatedTests = new List<GeneratedTest>();
        foreach (var issue in analysis.Issues)
        {
            try
            {
                var test = await _testGenerator.GenerateTestAsync(issue);
                generatedTests.Add(test);
            }
            catch (Exception ex)
            {
                generatedTests.Add(new GeneratedTest
                {
                    TestClassName = $"{issue.RuleId}GeneratedTests",
                    TestCode = "",
                    TargetFile = issue.FilePath,
                    Explanation = $"Test generation error: {ex.Message}"
                });
            }
        }

        // Step 4: Verify (if test project provided)
        VerificationResult? verification = null;
        if (!string.IsNullOrWhiteSpace(request.TestProjectPath))
        {
            var testFullPath = Path.GetFullPath(request.TestProjectPath);
            if (System.IO.File.Exists(testFullPath))
            {
                // Write generated test files into the test project directory
                var testProjectDir = Path.GetDirectoryName(testFullPath)!;
                foreach (var test in generatedTests.Where(t => !string.IsNullOrWhiteSpace(t.TestCode)))
                {
                    var testFilePath = Path.Combine(testProjectDir, $"{test.TestClassName}.cs");
                    await System.IO.File.WriteAllTextAsync(testFilePath, test.TestCode);
                }

                // Build refactored-files map from suggestions that have refactored code
                var refactoredFiles = new Dictionary<string, string>();
                for (int i = 0; i < analysis.Issues.Count; i++)
                {
                    if (i < suggestions.Count
                        && !string.IsNullOrWhiteSpace(suggestions[i].RefactoredCode)
                        && !string.IsNullOrWhiteSpace(analysis.Issues[i].FilePath))
                    {
                        var sourceFile = Path.GetFullPath(analysis.Issues[i].FilePath);
                        if (System.IO.File.Exists(sourceFile) && !refactoredFiles.ContainsKey(sourceFile))
                        {
                            refactoredFiles[sourceFile] = suggestions[i].RefactoredCode;
                        }
                    }
                }

                if (refactoredFiles.Count > 0)
                {
                    verification = await _verifier.VerifyWithRefactoredCodeAsync(testFullPath, refactoredFiles);
                }
                else
                {
                    verification = await _verifier.VerifyAsync(testFullPath);
                }

                var isSafe = verification.IsSafe;
                foreach (var suggestion in suggestions)
                {
                    suggestion.IsSafe = isSafe;
                }

                if (isSafe)
                {
                    for (var index = 0; index < suggestions.Count && index < analysis.Issues.Count; index++)
                    {
                        var suggestion = suggestions[index];
                        var issue = analysis.Issues[index];
                        await _acceptedRefactorings.SaveAsync(
                            CreateFindingFingerprint(issue),
                            issue.RuleId,
                            Hash(issue.CodeSnippet),
                            suggestion.RefactoredCode,
                            verification.Status);
                    }
                }
            }
        }

        return Ok(new PipelineResult
        {
            RetrievalMode = Environment.GetEnvironmentVariable("RAG_STORE_MODE") ?? "sqlite",
            EmbeddingModel = Environment.GetEnvironmentVariable("RAG_EMBEDDING_MODEL") ?? "",
            IndexVersion = Environment.GetEnvironmentVariable("RAG_INDEX_VERSION") ?? "1",
            Analysis = analysis,
            Suggestions = suggestions,
            GeneratedTests = generatedTests,
            Verification = verification
        });
    }

    private static string CreateFindingFingerprint(AnalysisIssue issue) =>
        Hash($"{issue.RuleId}:{issue.FilePath}:{issue.LineNumber}:{issue.CodeSnippet}");

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty))).ToLowerInvariant();
}
