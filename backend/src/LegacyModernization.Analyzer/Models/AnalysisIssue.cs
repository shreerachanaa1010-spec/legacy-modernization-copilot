namespace LegacyModernization.Analyzer.Models;

public class AnalysisIssue
{
    public string RuleId { get; set; } = "";

    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string Severity { get; set; } = "";

    public string FilePath { get; set; } = "";

    public int LineNumber { get; set; }

    public string CodeSnippet { get; set; } = "";
}