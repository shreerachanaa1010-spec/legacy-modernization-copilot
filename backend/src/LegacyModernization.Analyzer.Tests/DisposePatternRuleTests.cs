using LegacyModernization.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;

namespace LegacyModernization.Analyzer.Tests;

public class DisposePatternRuleTests
{
    private readonly DisposePatternRule _rule = new();

    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void Detects_Missing_DisposeBool()
    {
        var code = """
            using System;
            class Foo : IDisposable {
                public void Dispose() { }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC004", issues[0].RuleId);
        Assert.Equal("Foo", issues[0].CodeSnippet);
    }

    [Fact]
    public void NoIssues_When_DisposeBool_Present()
    {
        var code = """
            using System;
            class Foo : IDisposable {
                public void Dispose() {
                    Dispose(true);
                    GC.SuppressFinalize(this);
                }
                protected virtual void Dispose(bool disposing) { }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void NoIssues_When_Class_Does_Not_Implement_IDisposable()
    {
        var code = """
            class Foo {
                public void Dispose() { }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Detects_Across_Multiple_Classes()
    {
        var code = """
            using System;
            class Foo : IDisposable {
                public void Dispose() { }
            }
            class Bar : IDisposable {
                public void Dispose() { }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Equal(2, issues.Count);
        Assert.Contains(issues, i => i.CodeSnippet == "Foo");
        Assert.Contains(issues, i => i.CodeSnippet == "Bar");
    }

    [Fact]
    public void Severity_Is_Medium()
    {
        var code = """
            using System;
            class Foo : IDisposable {
                public void Dispose() { }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Equal("Medium", issues[0].Severity);
    }
}
