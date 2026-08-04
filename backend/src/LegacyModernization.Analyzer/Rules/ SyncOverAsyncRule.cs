using LegacyModernization.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

public class SyncOverAsyncRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(
        SyntaxNode root,
        string filePath)
    {
        var issues = new List<AnalysisIssue>();

        // Detect Task.Result
        var memberAccesses = root.DescendantNodes()
                                 .OfType<MemberAccessExpressionSyntax>();

        foreach (var access in memberAccesses)
        {
            if (access.Name.Identifier.Text == "Result")
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC001",
                    Title = "Avoid Task.Result",
                    Description = "Task.Result blocks the calling thread and may cause deadlocks.",
                    Severity = "High",
                    FilePath = filePath,
                    LineNumber = access.GetLocation()
                                       .GetLineSpan()
                                       .StartLinePosition.Line + 1,
                    CodeSnippet = access.ToString()
                });
            }
        }

        // Detect Task.Wait()
        var invocations = root.DescendantNodes()
                              .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.Text == "Wait")
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC001",
                    Title = "Avoid Task.Wait()",
                    Description = "Task.Wait blocks the calling thread.",
                    Severity = "High",
                    FilePath = filePath,
                    LineNumber = invocation.GetLocation()
                                           .GetLineSpan()
                                           .StartLinePosition.Line + 1,
                    CodeSnippet = invocation.ToString()
                });
            }
        }

        return issues;
    }
}