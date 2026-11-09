using System;
using System.Collections.Generic;
using System.Linq;
using LegacyModernization.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LegacyModernization.Analyzer.Rules;

public sealed class ThreadSleepRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member ||
                member.Name.Identifier.Text != "Sleep" ||
                member.Expression.ToString() != "Thread")
            {
                continue;
            }

            issues.Add(new AnalysisIssue
            {
                RuleId = "LMC007",
                Title = "Avoid Thread.Sleep()",
                Description = "Blocking the thread with Thread.Sleep() prevents responsive async behavior. Prefer Task.Delay and async flow control.",
                Severity = "Medium",
                FilePath = filePath,
                LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                CodeSnippet = invocation.ToString()
            });
        }

        return issues;
    }
}

public sealed class StringCaseRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member ||
                member.Expression is null)
            {
                continue;
            }

            var methodName = member.Name.Identifier.Text;
            if ((methodName == "ToLower" || methodName == "ToUpper") &&
                invocation.ArgumentList.Arguments.Count == 0)
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC008",
                    Title = "Use culture-invariant casing APIs",
                    Description = "ToLower()/ToUpper() is culture-sensitive by default. Prefer ToLowerInvariant()/ToUpperInvariant() or explicit culture-aware overloads.",
                    Severity = "Medium",
                    FilePath = filePath,
                    LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    CodeSnippet = invocation.ToString()
                });
            }
        }

        return issues;
    }
}

public sealed class StringComparisonRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            var methodName = member.Name.Identifier.Text;
            if (methodName is not ("Equals" or "StartsWith" or "EndsWith"))
            {
                continue;
            }

            var hasStringComparison = invocation.ArgumentList.Arguments.Any(arg =>
                arg.Expression.ToString().Contains("StringComparison", StringComparison.Ordinal));

            if (!hasStringComparison)
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC009",
                    Title = "Specify StringComparison explicitly",
                    Description = "String comparisons without an explicit StringComparison can behave differently across cultures and locales.",
                    Severity = "Medium",
                    FilePath = filePath,
                    LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    CodeSnippet = invocation.ToString()
                });
            }
        }

        return issues;
    }
}

public sealed class AsyncVoidMethodRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (method.Modifiers.Any(m => m.Text == "async") && method.ReturnType.ToString() == "void")
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC010",
                    Title = "Avoid async void methods",
                    Description = "async void is difficult to test and error-prone. Prefer Task-returning methods and handle completion explicitly.",
                    Severity = "Medium",
                    FilePath = filePath,
                    LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    CodeSnippet = method.ToString()
                });
            }
        }

        return issues;
    }
}

public sealed class DateTimeParseRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member)
            {
                continue;
            }

            var methodName = member.Name.Identifier.Text;
            var targetText = member.Expression.ToString();
            if ((targetText == "DateTime" || targetText == "DateTimeOffset") &&
                (methodName == "Parse" || methodName == "TryParse" || methodName == "ParseExact" || methodName == "TryParseExact"))
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC011",
                    Title = "Use culture-aware parsing when needed",
                    Description = "Parsing dates without an explicit culture or format can introduce locale-dependent behavior.",
                    Severity = "Low",
                    FilePath = filePath,
                    LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    CodeSnippet = invocation.ToString()
                });
            }
        }

        return issues;
    }
}

public sealed class NewRandomRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (creation.Type.ToString() == "Random")
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC012",
                    Title = "Avoid creating Random repeatedly",
                    Description = "Creating Random in quick succession can produce identical values. Prefer a single shared random source or a deterministic strategy.",
                    Severity = "Low",
                    FilePath = filePath,
                    LineNumber = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    CodeSnippet = creation.ToString()
                });
            }
        }

        return issues;
    }
}

public sealed class TaskRunRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.Text is "Run" or "StartNew")
            {
                var targetType = member.Expression.ToString();
                if (targetType == "Task" || targetType == "Task.Factory")
                {
                    issues.Add(new AnalysisIssue
                    {
                        RuleId = "LMC013",
                        Title = "Review Task.Run / StartNew usage",
                        Description = "Task.Run and Task.Factory.StartNew can hide UI or thread-affinity issues. Use async/await and explicit scheduling where needed.",
                        Severity = "Medium",
                        FilePath = filePath,
                        LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        CodeSnippet = invocation.ToString()
                    });
                }
            }
        }

        return issues;
    }
}

public sealed class LockThisRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var lockStatement in root.DescendantNodes().OfType<LockStatementSyntax>())
        {
            if (lockStatement.Expression is ThisExpressionSyntax or TypeOfExpressionSyntax)
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC014",
                    Title = "Avoid locking on this or type objects",
                    Description = "Locking on this or a type object may expose synchronization state across unrelated code paths. Prefer a private readonly object.",
                    Severity = "Medium",
                    FilePath = filePath,
                    LineNumber = lockStatement.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    CodeSnippet = lockStatement.ToString()
                });
            }
        }

        return issues;
    }
}

public sealed class CancellationTokenRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var returnType = method.ReturnType.ToString();
            var isAsyncTask = returnType.StartsWith("Task", StringComparison.Ordinal) ||
                              returnType.StartsWith("ValueTask", StringComparison.Ordinal);
            if (!isAsyncTask)
            {
                continue;
            }

            var hasCancellationToken = method.ParameterList.Parameters.Any(parameter =>
                parameter.Type?.ToString() == "CancellationToken" ||
                parameter.Type?.ToString() == "System.Threading.CancellationToken");

            if (!hasCancellationToken)
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC015",
                    Title = "Add CancellationToken to async APIs",
                    Description = "Async APIs should accept CancellationToken to support cooperative cancellation and timeouts.",
                    Severity = "Low",
                    FilePath = filePath,
                    LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    CodeSnippet = method.ToString()
                });
            }
        }

        return issues;
    }
}

public sealed class StringLiteralComparisonRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var comparison in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
        {
            if (comparison.Kind() is not (Microsoft.CodeAnalysis.CSharp.SyntaxKind.EqualsExpression or Microsoft.CodeAnalysis.CSharp.SyntaxKind.NotEqualsExpression))
            {
                continue;
            }

            var leftIsStringLiteral = comparison.Left is LiteralExpressionSyntax leftLiteral &&
                                     leftLiteral.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression;
            var rightIsStringLiteral = comparison.Right is LiteralExpressionSyntax rightLiteral &&
                                      rightLiteral.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression;

            if (leftIsStringLiteral || rightIsStringLiteral)
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC016",
                    Title = "Use string helpers for null-safe comparisons",
                    Description = "String literal comparisons can be made clearer and safer with string.IsNullOrEmpty or a culture-aware comparison API.",
                    Severity = "Low",
                    FilePath = filePath,
                    LineNumber = comparison.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    CodeSnippet = comparison.ToString()
                });
            }
        }

        return issues;
    }
}
