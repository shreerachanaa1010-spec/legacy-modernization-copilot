using LegacyModernization.Core.Models;

namespace LegacyModernization.LLM.Services;

public interface ILlmService
{
    Task<RefactorSuggestion> GenerateSuggestionAsync(AnalysisIssue issue);
}