namespace LegacyModernization.Rag.Models;

public sealed class ReviewDecision
{
    public string FindingFingerprint { get; init; } = "";
    public string Decision { get; init; } = "pending";
    public string FilePath { get; init; } = "";
    public string OriginalCode { get; init; } = "";
    public string RefactoredCode { get; init; } = "";
    public DateTimeOffset UpdatedAt { get; init; }
}