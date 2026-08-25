using LegacyModernization.Core.Models;

namespace LegacyModernization.Verifier.Services;

public class VerificationService
{
    private readonly TestRunner _testRunner;

    public VerificationService(TestRunner testRunner)
    {
        _testRunner = testRunner;
    }

    /// <summary>
    /// Run tests against the original code only.
    /// </summary>
    public async Task<VerificationResult> VerifyAsync(string testProjectPath)
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

    /// <summary>
    /// Run tests against original code, then apply refactored code, re-run, and restore.
    /// refactoredFiles maps source file path → refactored content.
    /// </summary>
    public async Task<VerificationResult> VerifyWithRefactoredCodeAsync(
        string testProjectPath,
        Dictionary<string, string> refactoredFiles)
    {
        Console.WriteLine();
        Console.WriteLine("========== FULL VERIFICATION ==========");

        // Phase 1: Run tests against original code
        Console.WriteLine("Phase 1: Testing original code...");
        var originalResult = await _testRunner.RunTestsAsync(testProjectPath);
        Console.WriteLine(originalResult.Passed ? "ORIGINAL: PASS" : "ORIGINAL: FAIL");

        // Phase 2: Apply refactored code, run tests, then restore
        var backups = new Dictionary<string, string>();
        bool refactoredPassed = false;
        string refactoredOutput = "";

        try
        {
            // Backup and apply refactored code
            foreach (var (filePath, refactoredContent) in refactoredFiles)
            {
                if (File.Exists(filePath) && !string.IsNullOrWhiteSpace(refactoredContent))
                {
                    backups[filePath] = File.ReadAllText(filePath);
                    File.WriteAllText(filePath, refactoredContent);
                    Console.WriteLine($"Applied refactored code to: {filePath}");
                }
            }

            // Run tests against refactored code
            Console.WriteLine("Phase 2: Testing refactored code...");
            var refactoredResult = await _testRunner.RunTestsAsync(testProjectPath);
            refactoredPassed = refactoredResult.Passed;
            refactoredOutput = refactoredResult.Output;
            Console.WriteLine(refactoredPassed ? "REFACTORED: PASS" : "REFACTORED: FAIL");
        }
        finally
        {
            // Always restore original files
            foreach (var (filePath, originalContent) in backups)
            {
                File.WriteAllText(filePath, originalContent);
                Console.WriteLine($"Restored original: {filePath}");
            }
        }

        Console.WriteLine("========================================");

        string status;
        string explanation;

        if (originalResult.Passed && refactoredPassed)
        {
            status = "BOTH_PASS";
            explanation = "Tests pass against both original and refactored code. The refactoring is safe to apply.";
        }
        else if (originalResult.Passed && !refactoredPassed)
        {
            status = "REFACTORED_FAIL";
            explanation = "Tests pass against original code but fail against refactored code. The refactoring may introduce regressions.";
        }
        else if (!originalResult.Passed && refactoredPassed)
        {
            status = "ORIGINAL_FAIL";
            explanation = "Tests fail against original code but pass against refactored code. The refactoring fixes existing issues.";
        }
        else
        {
            status = "BOTH_FAIL";
            explanation = "Tests fail against both original and refactored code. Review the generated tests and suggestions.";
        }

        return new VerificationResult
        {
            OriginalTestPassed = originalResult.Passed,
            RefactoredTestPassed = refactoredPassed,
            Status = status,
            OriginalOutput = originalResult.Output,
            RefactoredOutput = refactoredOutput,
            Explanation = explanation
        };
    }
}