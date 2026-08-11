namespace LegacyModernization.Verifier.Models;

public class VerificationResult
{
    public bool OriginalTestPassed { get; set; }

    public bool RefactoredTestPassed { get; set; }

    public string Status { get; set; } = string.Empty;

    public string OriginalOutput { get; set; } = string.Empty;

    public string RefactoredOutput { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;
}