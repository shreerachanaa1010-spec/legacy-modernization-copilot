using LegacyModernization.Analyzer.Models;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

public class WebClientRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        // Placeholder: detect usage of System.Net.WebClient
        return Enumerable.Empty<AnalysisIssue>();
    }
}