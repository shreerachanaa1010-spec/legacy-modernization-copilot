using LegacyModernization.Analyzer.Models;
using LegacyModernization.LLM.Models;
using LegacyModernization.Rag.Models;

namespace LegacyModernization.LLM.Services;

public interface ILlmService
{
    Task<RefactorSuggestion> GenerateSuggestionAsync(
        AnalysisIssue issue,
        RetrievedContext context);
}