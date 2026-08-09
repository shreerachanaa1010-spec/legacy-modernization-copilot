using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using LegacyModernization.Analyzer.Services;
using LegacyModernization.LLM.Services;

Console.WriteLine("Analyzer host starting...");

var analyzer = new ProjectAnalyzer();

string projectPath;

if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
{
    projectPath = args[0];
}
else
{
    projectPath = System.IO.Path.GetFullPath(
        "samples/LegacySampleProject/LegacySampleProject.csproj");

    Console.WriteLine(
        "No project path provided, defaulting to sample project.");
}

Console.WriteLine($"Analyzing: {projectPath}");

var result = await analyzer.AnalyzeAsync(projectPath);

// Prepare reports directory
var reportsDir = Path.GetFullPath("reports");
Directory.CreateDirectory(reportsDir);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

// Save analysis results
var analysisPath = Path.Combine(reportsDir, "analysis-results.json");
File.WriteAllText(analysisPath, JsonSerializer.Serialize(result, jsonOptions));
Console.WriteLine($"Wrote analysis results to: {analysisPath}");

// If there are issues, ask the LLM for suggestions and save them
var suggestions = new List<LegacyModernization.LLM.Models.RefactorSuggestion>();
if (result.Issues.Any())
{
    Console.WriteLine();
    Console.WriteLine("Generating AI refactoring suggestions...");

    var llm = new GeminiService();

    foreach (var issue in result.Issues)
    {
        try
        {
            var suggestion = await llm.GenerateSuggestionAsync(issue);
            suggestions.Add(suggestion);
            Console.WriteLine($"Generated suggestion for {issue.RuleId} at {issue.FilePath}:{issue.LineNumber}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LLM error for {issue.RuleId}: {ex.Message}");
        }
    }

    var suggestionsPath = Path.Combine(reportsDir, "ai-suggestions.json");
    File.WriteAllText(suggestionsPath, JsonSerializer.Serialize(suggestions, jsonOptions));
    Console.WriteLine($"Wrote AI suggestions to: {suggestionsPath}");
}
else
{
    Console.WriteLine("No issues found.");
}

Console.WriteLine("Analysis complete.");
