namespace LegacyModernization.Analyzer.Models;

public class ProjectAnalysisResult
{
    public string ProjectName { get; set; } = "";

    public List<ClassInfo> Classes { get; set; } = new();

    public List<AnalysisIssue> Issues { get; set; } = new();
}
