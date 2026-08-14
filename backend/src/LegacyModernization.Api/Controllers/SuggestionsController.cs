using LegacyModernization.Analyzer.Services;
using LegacyModernization.Api.Models;
using LegacyModernization.LLM.Models;
using LegacyModernization.LLM.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyModernization.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuggestionsController : ControllerBase
{
    private readonly IProjectAnalyzer _analyzer;
    private readonly ILlmService _llmService;

    public SuggestionsController(IProjectAnalyzer analyzer, ILlmService llmService)
    {
        _analyzer = analyzer;
        _llmService = llmService;
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

        var suggestions = new List<RefactorSuggestion>();

        foreach (var issue in analysis.Issues)
        {
            try
            {
                var suggestion = await _llmService.GenerateSuggestionAsync(issue);
                suggestions.Add(suggestion);
            }
            catch (Exception ex)
            {
                suggestions.Add(new RefactorSuggestion
                {
                    RuleId = issue.RuleId,
                    IssueTitle = issue.Title,
                    Reason = issue.Description,
                    OriginalCode = issue.CodeSnippet,
                    RefactoredCode = "",
                    Explanation = $"LLM error: {ex.Message}",
                    IsSafe = false
                });
            }
        }

        return Ok(suggestions);
    }
}
