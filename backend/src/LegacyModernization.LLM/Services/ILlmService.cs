using LegacyModernization.Analyzer.Models;
using LegacyModernization.LLM.Models;

namespace LegacyModernization.LLM.Services;

public interface ILlmService
{
    Task<RefactorSuggestion> GenerateSuggestionAsync(AnalysisIssue issue);
}