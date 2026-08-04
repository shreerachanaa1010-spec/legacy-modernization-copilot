using LegacyModernization.Analyzer.Models;
using Microsoft.CodeAnalysis;

namespace LegacyModernization.Analyzer.Rules;

public class PatternRuleEngine
{
    private readonly List<ILegacyRule> _rules;

    public PatternRuleEngine()
    {
        _rules = new List<ILegacyRule>
        {
            new SyncOverAsyncRule(),
            new WebClientRule(),
            new ConfigureAwaitRule(),
            new DisposePatternRule()
        };
    }

    public List<AnalysisIssue> Analyze(
        SyntaxNode root,
        string filePath)
    {
        var issues = new List<AnalysisIssue>();

        foreach (var rule in _rules)
        {
            issues.AddRange(rule.Analyze(root, filePath));
        }

        return issues;
    }
}