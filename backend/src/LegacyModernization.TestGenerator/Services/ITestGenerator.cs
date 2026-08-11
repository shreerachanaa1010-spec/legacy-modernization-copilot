using LegacyModernization.Analyzer.Models;
using LegacyModernization.TestGenerator.Models;

namespace LegacyModernization.TestGenerator.Services;

public interface ITestGenerator
{
    Task<GeneratedTest> GenerateTestAsync(AnalysisIssue issue);
}