using LegacyModernization.Analyzer.Services;
using LegacyModernization.Api.Models;
using LegacyModernization.LLM.Models;
using LegacyModernization.LLM.Services;
using LegacyModernization.Verifier.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyModernization.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PipelineController : ControllerBase
{
    private readonly IProjectAnalyzer _analyzer;
    private readonly ILlmService _llmService;
    private readonly VerificationService _verifier;

    public PipelineController(
        IProjectAnalyzer analyzer,
        ILlmService llmService,
        VerificationService verifier)
    {
        _analyzer = analyzer;
        _llmService = llmService;
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

        var projectFullPath = Path.GetFullPath(request.ProjectPath);

        if (!System.IO.File.Exists(projectFullPath))
            return NotFound($"Project file not found: {projectFullPath}");

        // Step 1: Analyze
        var analysis = await _analyzer.AnalyzeAsync(projectFullPath);

        // Step 2: Generate suggestions (parallel for speed)
        var suggestionTasks = analysis.Issues.Select(async issue =>
        {
            try
            {
                return await _llmService.GenerateSuggestionAsync(issue);
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
                    Explanation = $"LLM error: {ex.Message}",
                    IsSafe = false
                };
            }
        });
        var suggestions = (await Task.WhenAll(suggestionTasks)).ToList();

        // Step 3: Verify (if test project provided)
        Verifier.Models.VerificationResult? verification = null;
        if (!string.IsNullOrWhiteSpace(request.TestProjectPath))
        {
            var testFullPath = Path.GetFullPath(request.TestProjectPath);
            if (System.IO.File.Exists(testFullPath))
            {
                verification = await _verifier.VerifyAsync(testFullPath);
            }
        }

        return Ok(new PipelineResult
        {
            Analysis = analysis,
            Suggestions = suggestions,
            Verification = verification
        });
    }
}
