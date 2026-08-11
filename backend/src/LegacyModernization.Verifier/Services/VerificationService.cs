using LegacyModernization.Verifier.Models;

namespace LegacyModernization.Verifier.Services;

public class VerificationService
{
    private readonly TestRunner _testRunner;

    public VerificationService()
    {
        _testRunner = new TestRunner();
    }

    public async Task<VerificationResult> VerifyAsync(
        string testProjectPath)
    {
        Console.WriteLine();
        Console.WriteLine("========== VERIFICATION ==========");

        Console.WriteLine();
        Console.WriteLine("Running generated tests...");

        var result = await _testRunner.RunTestsAsync(testProjectPath);

        Console.WriteLine();

        if (result.Passed)
        {
            Console.WriteLine("TEST RESULT: PASS");
        }
        else
        {
            Console.WriteLine("TEST RESULT: FAIL");
        }

        Console.WriteLine("==================================");

        return new VerificationResult
        {
            OriginalTestPassed = result.Passed,
            RefactoredTestPassed = false,
            Status = result.Passed
                ? "ORIGINAL_PASS"
                : "ORIGINAL_FAIL",
            OriginalOutput = result.Output,
            Explanation = result.Passed
                ? "Generated test passed against the original code."
                : "Generated test failed against the original code."
        };
    }
}