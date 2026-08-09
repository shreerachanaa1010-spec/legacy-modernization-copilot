using LegacyModernization.Analyzer.Models;
using LegacyModernization.Analyzer.Rules;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Linq;
using System.Text.Json;

namespace LegacyModernization.Analyzer.Services;

/// <summary>
/// Analyzes a .NET project using Roslyn and runs legacy-pattern rules.
/// Produces a ProjectAnalysisResult containing classes, methods and detected issues.
/// </summary>
public class ProjectAnalyzer : IProjectAnalyzer
{
    public async Task<ProjectAnalysisResult> AnalyzeAsync(string projectPath)
    {
        // Register MSBuild so Roslyn can load .csproj files
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        var result = new ProjectAnalysisResult();

        using var workspace = MSBuildWorkspace.Create();

        var project = await workspace.OpenProjectAsync(projectPath);

        result.ProjectName = project.Name;

        // Initialize the pattern detection engine
        var ruleEngine = new PatternRuleEngine();

        foreach (var document in project.Documents)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync();

            if (syntaxRoot == null)
                continue;

            // -----------------------------
            // Run all legacy pattern rules
            // -----------------------------
            var issues = ruleEngine.Analyze(
                syntaxRoot,
                document.FilePath ?? document.Name);

            result.Issues.AddRange(issues);

            // -----------------------------
            // Extract Classes and Methods
            // -----------------------------
            var classes = syntaxRoot.DescendantNodes()
                                    .OfType<ClassDeclarationSyntax>();

            foreach (var classNode in classes)
            {
                var classInfo = new ClassInfo
                {
                    Name = classNode.Identifier.Text,
                    Namespace = classNode.Ancestors()
                                         .OfType<NamespaceDeclarationSyntax>()
                                         .FirstOrDefault()?.Name.ToString() ?? ""
                };

                foreach (var method in classNode.Members.OfType<MethodDeclarationSyntax>())
                {
                    classInfo.Methods.Add(new MethodInfo
                    {
                        Name = method.Identifier.Text,
                        ReturnType = method.ReturnType.ToString()
                    });
                }

                result.Classes.Add(classInfo);
            }
        }

        // Print the final analysis report as JSON
        var json = JsonSerializer.Serialize(
            result,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

        Console.WriteLine(json);

        return result;
    }
}