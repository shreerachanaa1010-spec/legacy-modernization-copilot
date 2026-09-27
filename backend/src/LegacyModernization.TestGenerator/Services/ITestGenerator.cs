using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;

namespace LegacyModernization.TestGenerator.Services;

public interface ITestGenerator
{
    Task<GeneratedTest> GenerateTestAsync(AnalysisIssue issue, RetrievedContext context);
}