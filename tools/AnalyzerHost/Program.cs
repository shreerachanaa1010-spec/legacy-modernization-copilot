using System;
using LegacyModernization.Analyzer.Services;

Console.WriteLine("Analyzer host starting...");

var analyzer = new ProjectAnalyzer();

string projectPath;
if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
{
    projectPath = args[0];
}
else
{
    projectPath = System.IO.Path.GetFullPath("samples/LegacySampleProject/LegacySampleProject.csproj");
    Console.WriteLine("No project path provided, defaulting to sample project.");
}

Console.WriteLine($"Analyzing: {projectPath}");

await analyzer.AnalyzeAsync(projectPath);

Console.WriteLine("Analysis complete.");
