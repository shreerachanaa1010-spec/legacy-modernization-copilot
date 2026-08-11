using System;
using System.IO;
using LegacyModernization.Analyzer.Models;
using LegacyModernization.TestGenerator.Services;

Console.WriteLine("Test Generator starting...");

var issue = new AnalysisIssue
{
    RuleId = "LMC001",
    Title = "Sync-over-Async",
    Description = "Synchronous blocking on an asynchronous operation can cause thread starvation.",
    FilePath = "samples/LegacySampleProject/CustomerService.cs",
    LineNumber = 10,
    CodeSnippet = "var result = task.Result"
};

var generator = new GeminiTestGenerator();

var generatedTest = await generator.GenerateTestAsync(issue);

Console.WriteLine();
Console.WriteLine("========== GENERATED TEST ==========");
Console.WriteLine(generatedTest.TestCode);
Console.WriteLine("====================================");

var outputDirectory = Path.GetFullPath("generated-tests");

Directory.CreateDirectory(outputDirectory);

var outputFile = Path.Combine(
    outputDirectory,
    $"{generatedTest.TestClassName}.cs"
);

await File.WriteAllTextAsync(
    outputFile,
    generatedTest.TestCode
);

Console.WriteLine();
Console.WriteLine($"Generated test saved to: {outputFile}");

Console.WriteLine();
Console.WriteLine("Test generation complete.");