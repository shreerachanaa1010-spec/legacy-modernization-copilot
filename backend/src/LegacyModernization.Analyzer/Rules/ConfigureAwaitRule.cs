using LegacyModernization.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

public class ConfigureAwaitRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        // Find all await expressions and flag those that do not use ConfigureAwait(false)
        var awaits = root.DescendantNodes().OfType<AwaitExpressionSyntax>();

        foreach (var awaitExpr in awaits)
        {
            var exprText = awaitExpr.Expression.ToString();

            // If the awaited expression already contains ConfigureAwait(false), skip
            if (exprText.Contains("ConfigureAwait(false)"))
                continue;

            issues.Add(new AnalysisIssue
            {
                RuleId = "LMC003",
                Title = "Missing ConfigureAwait(false)",
                Description = "Awaited tasks in library code should use ConfigureAwait(false) to avoid capturing the synchronization context.",
                Severity = "Medium",
                FilePath = filePath,
                LineNumber = awaitExpr.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                CodeSnippet = awaitExpr.ToString()
            });
        }

        return issues;
    }
}