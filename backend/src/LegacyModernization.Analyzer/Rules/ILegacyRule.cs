using Microsoft.CodeAnalysis;
using LegacyModernization.Analyzer.Models;

namespace LegacyModernization.Analyzer.Rules;

/// <summary>
/// Rule interface for legacy/detection rules. Implement Analyze to return detected issues.
/// </summary>
public interface ILegacyRule
{
    IEnumerable<AnalysisIssue> Analyze(
        SyntaxNode root,
        string filePath);
}