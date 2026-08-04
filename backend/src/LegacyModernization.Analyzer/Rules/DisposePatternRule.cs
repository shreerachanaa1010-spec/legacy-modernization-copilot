using LegacyModernization.Analyzer.Models;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

public class DisposePatternRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        // Placeholder: detect incorrect dispose patterns
        return Enumerable.Empty<AnalysisIssue>();
    }
}
