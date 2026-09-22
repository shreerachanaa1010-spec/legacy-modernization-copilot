using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;

namespace LegacyModernization.LLM.Services;

public interface ILlmService
{
    Task<RefactorSuggestion> GenerateSuggestionAsync(
        AnalysisIssue issue,
        RetrievedContext context);

    Task<RefactorSuggestion> GenerateSuggestionAsync(AnalysisIssue issue)
    {
        return GenerateSuggestionAsync(issue, new RetrievedContext { Documents = [] });
    }
}