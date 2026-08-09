namespace LegacyModernization.Analyzer.Models;

/// <summary>
/// Aggregated analysis output for a project: project name, classes and detected issues.
/// </summary>
public class ProjectAnalysisResult
{
    public string ProjectName { get; set; } = "";

    public List<ClassInfo> Classes { get; set; } = new();

    public List<AnalysisIssue> Issues { get; set; } = new();
}
