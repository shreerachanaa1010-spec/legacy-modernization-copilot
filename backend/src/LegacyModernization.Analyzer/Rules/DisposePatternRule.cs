using LegacyModernization.Analyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace LegacyModernization.Analyzer.Rules;

public class DisposePatternRule : ILegacyRule
{
    public IEnumerable<AnalysisIssue> Analyze(
        SyntaxNode root,
        string filePath)
    {
        var issues = new List<AnalysisIssue>();

        // Find every class in the syntax tree
        var classes = root.DescendantNodes()
                          .OfType<ClassDeclarationSyntax>();

        foreach (var classNode in classes)
        {
            // Does this class implement IDisposable?
            bool implementsDisposable =
                classNode.BaseList?.Types.Any(baseType =>
                    baseType.Type.ToString() == "IDisposable") ?? false;

            if (!implementsDisposable)
                continue;

            // Does it contain Dispose(bool disposing)?
            bool hasDisposeBool =
                classNode.Members
                         .OfType<MethodDeclarationSyntax>()
                         .Any(method =>
                             method.Identifier.Text == "Dispose" &&
                             method.ParameterList.Parameters.Count == 1 &&
                             method.ParameterList.Parameters[0].Type?.ToString() == "bool");

            if (!hasDisposeBool)
            {
                issues.Add(new AnalysisIssue
                {
                    RuleId = "LMC004",
                    Title = "Non-standard IDisposable pattern",
                    Description = "Class implements IDisposable but does not define Dispose(bool disposing).",
                    Severity = "Medium",
                    FilePath = filePath,
                    LineNumber = classNode.GetLocation()
                                          .GetLineSpan()
                                          .StartLinePosition.Line + 1,
                    CodeSnippet = classNode.Identifier.Text
                });
            }
        }

        return issues;
    }
}