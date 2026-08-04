using Microsoft.CodeAnalysis;
using LegacyModernization.Analyzer.Models;

namespace LegacyModernization.Analyzer.Rules;

public interface ILegacyRule
{
    IEnumerable<AnalysisIssue> Analyze(
        SyntaxNode root,
        string filePath);
}