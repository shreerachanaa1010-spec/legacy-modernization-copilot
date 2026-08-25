using LegacyModernization.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

/// <summary>
/// Detects direct instantiation of HttpClient (new HttpClient()) which can exhaust sockets.
/// Recommends using IHttpClientFactory instead.
/// </summary>
public class HttpClientInstantiationRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(SyntaxNode root, string filePath)
    {
        var issues = new List<AnalysisIssue>();

        var objectCreations = root.DescendantNodes()
                                  .OfType<ObjectCreationExpressionSyntax>();

        foreach (var creation in objectCreations)
        {
            var typeName = creation.Type.ToString();

            if (typeName == "HttpClient" || typeName.EndsWith(".HttpClient"))
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC005",
                    Title = "Avoid direct HttpClient instantiation",
                    Description = "Creating HttpClient directly can exhaust socket connections. Use IHttpClientFactory instead.",
                    Severity = "High",
                    FilePath = filePath,
                    LineNumber = creation.GetLocation()
                                         .GetLineSpan()
                                         .StartLinePosition.Line + 1,
                    CodeSnippet = creation.ToString()
                });
            }
        }

        // Also detect implicit new: HttpClient client = new();
        var implicitCreations = root.DescendantNodes()
                                    .OfType<ImplicitObjectCreationExpressionSyntax>();

        foreach (var creation in implicitCreations)
        {
            // Check if the variable declaration type is HttpClient
            var variableDecl = creation.Ancestors()
                                       .OfType<VariableDeclarationSyntax>()
                                       .FirstOrDefault();

            if (variableDecl != null && variableDecl.Type.ToString().Contains("HttpClient"))
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC005",
                    Title = "Avoid direct HttpClient instantiation",
                    Description = "Creating HttpClient directly can exhaust socket connections. Use IHttpClientFactory instead.",
                    Severity = "High",
                    FilePath = filePath,
                    LineNumber = creation.GetLocation()
                                         .GetLineSpan()
                                         .StartLinePosition.Line + 1,
                    CodeSnippet = creation.ToString()
                });
            }
        }

        return issues;
    }
}
