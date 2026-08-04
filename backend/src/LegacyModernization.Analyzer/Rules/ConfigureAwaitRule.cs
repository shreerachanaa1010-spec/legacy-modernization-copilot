using LegacyModernization.Analyzer.Models;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

public class ConfigureAwaitRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        // Placeholder: detect missing ConfigureAwait(false) in library code
        return Enumerable.Empty<AnalysisIssue>();
    }
}
