using LegacyModernization.Verifier.Services;

Console.WriteLine("Verification starting...");

var verifier = new VerificationService();

var result = await verifier.VerifyAsync(
    "samples/LegacySampleProject.Tests/LegacySampleProject.Tests.csproj"
);

Console.WriteLine();
Console.WriteLine("========== VERIFICATION RESULT ==========");

Console.WriteLine(
    $"Original tests passed: {result.OriginalTestPassed}"
);

Console.WriteLine(
    $"Status: {result.Status}"
);

Console.WriteLine(
    $"Explanation: {result.Explanation}"
);

Console.WriteLine("=========================================");