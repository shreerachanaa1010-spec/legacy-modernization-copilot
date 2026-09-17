using LegacyModernization.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

/// <summary>
/// Detects usage of DateTime.Now and DateTime.UtcNow which makes code hard to test.
/// Recommends using DateTimeOffset or an abstracted time provider (TimeProvider in .NET 8+).
/// </summary>
public class DateTimeNowRule : ILegacyRule
{
    private static readonly HashSet<string> FlaggedMembers = new()
    {
        "Now",
        "UtcNow",
        "Today"
    };

    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        var memberAccesses = root.DescendantNodes()
                                 .OfType<MemberAccessExpressionSyntax>();

        foreach (var access in memberAccesses)
        {
            var expressionText = access.Expression.ToString();
            var memberName = access.Name.Identifier.Text;

            if ((expressionText == "DateTime" || expressionText == "System.DateTime")
                && FlaggedMembers.Contains(memberName))
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC006",
                    Title = $"Avoid DateTime.{memberName}",
                    Description = $"DateTime.{memberName} is not testable and timezone-unsafe. Use TimeProvider (net8+) or DateTimeOffset instead.",
                    Severity = "Low",
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
