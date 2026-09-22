namespace LegacyModernization.Core.Models;

public class RefactorSuggestion
{
    public string RuleId { get; set; } = "";

    public string IssueTitle { get; set; } = "";

    public string Reason { get; set; } = "";

    public string OriginalCode { get; set; } = "";

    public string RefactoredCode { get; set; } = "";

    public string Explanation { get; set; } = "";

    public List<string> EvidenceIds { get; set; } = [];

    public string GenerationStatus { get; set; } = "pending-verification";

    public double? Confidence { get; set; }

    public bool IsSafe { get; set; }
}
