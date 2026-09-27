using LegacyModernization.Core.Models;

namespace LegacyModernization.Api.Models;

public sealed class ReviewDecisionRequest
{
    public string ProjectPath { get; set; } = "";
    public AnalysisIssue? Issue { get; set; }
    public RefactorSuggestion? Suggestion { get; set; }
    public string Decision { get; set; } = "";
}

public sealed class ReviewDecisionResponse
{
    public string Decision { get; init; } = "";
    public string Message { get; init; } = "";
    public string FilePath { get; init; } = "";
}