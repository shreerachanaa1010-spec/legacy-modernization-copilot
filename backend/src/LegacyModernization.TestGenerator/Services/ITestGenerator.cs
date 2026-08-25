using LegacyModernization.Core.Models;

namespace LegacyModernization.TestGenerator.Services;

public interface ITestGenerator
{
    Task<GeneratedTest> GenerateTestAsync(AnalysisIssue issue);
}