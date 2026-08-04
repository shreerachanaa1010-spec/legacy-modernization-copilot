using LegacyModernization.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

public class WebClientRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(
        SyntaxNode root,
        string filePath)
    {
        var issues = new List<AnalysisIssue>();

        // Find every object creation expression (new Something())
        var objectCreations = root.DescendantNodes()
                                  .OfType<ObjectCreationExpressionSyntax>();

        foreach (var creation in objectCreations)
        {
            // Check if the object being created is WebClient (handles fully-qualified names)
            var typeName = creation.Type.ToString();
            if (typeName.EndsWith("WebClient") || typeName.Contains("WebClient"))
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC002",
                    Title = "WebClient is obsolete",
                    Description = "System.Net.WebClient is obsolete. Use HttpClient instead.",
                    Severity = "Medium",
                    FilePath = filePath,
                    LineNumber = creation.GetLocation()
                                         .GetLineSpan()
                                         .StartLinePosition.Line + 1,
                    CodeSnippet = creation.ToString()
                });
            }
        }

        // Also detect member access usages that reference WebClient, e.g. WebClient.DownloadString or new WebClient().DownloadString
        var memberAccesses = root.DescendantNodes()
                                 .OfType<MemberAccessExpressionSyntax>();

        foreach (var access in memberAccesses)
        {
            // Check for syntactic occurrences of "WebClient"
            var expressionText = access.Expression.ToString();
            var fullText = access.ToString();
            if (expressionText.Contains("WebClient") || fullText.Contains("WebClient"))
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC002",
                    Title = "WebClient is obsolete",
                    Description = "System.Net.WebClient is obsolete. Use HttpClient instead.",
                    Severity = "Medium",
                    FilePath = filePath,
                    LineNumber = access.GetLocation()
                                         .GetLineSpan()
                                         .StartLinePosition.Line + 1,
                    CodeSnippet = access.ToString()
                });
            }
        }

        return issues;
    }
}