namespace LegacyModernization.Rag.Services;

public interface IAcceptedRefactoringStore
{
    Task SaveAsync(
        string findingFingerprint,
        string ruleId,
        string originalCodeHash,
        string refactoredCode,
        string verificationStatus,
        CancellationToken cancellationToken = default);
}
