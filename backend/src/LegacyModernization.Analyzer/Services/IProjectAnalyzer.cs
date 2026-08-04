using LegacyModernization.Analyzer.Models;

namespace LegacyModernization.Analyzer.Services;

public interface IProjectAnalyzer
{
    Task<ProjectAnalysisResult> AnalyzeAsync(string projectPath);
}