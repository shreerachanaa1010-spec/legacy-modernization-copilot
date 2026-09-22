using LegacyModernization.Analyzer.Services;
using LegacyModernization.Api.Models;
using LegacyModernization.Core.Models;
using LegacyModernization.LLM.Services;
using LegacyModernization.Rag.Models;
using LegacyModernization.Rag.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyModernization.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuggestionsController : ControllerBase
{
    private readonly IProjectAnalyzer _analyzer;
    private readonly ILlmService _llmService;
    private readonly IRepositoryRetriever _retriever;

    public SuggestionsController(
        IProjectAnalyzer analyzer,
        ILlmService llmService,
        IRepositoryRetriever retriever)
    {
        _analyzer = analyzer;
        _llmService = llmService;
        _retriever = retriever;
    }

    /// <summary>
    /// Analyze a project and generate AI refactoring suggestions for all detected issues.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetSuggestions([FromBody] SuggestionsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectPath))
            return BadRequest("ProjectPath is required.");

        var fullPath = Path.GetFullPath(request.ProjectPath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound($"Project file not found: {fullPath}");

        var analysis = await _analyzer.AnalyzeAsync(fullPath);
        var projectRoot = Path.GetDirectoryName(fullPath)!;

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
                    Explanation = $"LLM error: {ex.Message}",
                    IsSafe = false
                };
            }
        });
        var suggestions = (await Task.WhenAll(suggestionTasks)).ToList();

        return Ok(suggestions);
    }
}
