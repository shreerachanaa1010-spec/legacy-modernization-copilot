using LegacyModernization.Analyzer.Models;

namespace LegacyModernization.Analyzer.Services;

/// <summary>
/// Contract for a project analyzer that inspects a .csproj and returns analysis results.
/// </summary>
public interface IProjectAnalyzer
{
    Task<ProjectAnalysisResult> AnalyzeAsync(string projectPath);
}