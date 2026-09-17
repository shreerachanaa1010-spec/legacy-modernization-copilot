using LegacyModernization.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;

namespace LegacyModernization.Analyzer.Tests;

public class ConfigureAwaitRuleTests
{
    private readonly ConfigureAwaitRule _rule = new();

    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void Detects_Missing_ConfigureAwait()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                async Task Bar() {
                    await Task.Delay(1);
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC003", issues[0].RuleId);
        Assert.Equal("Medium", issues[0].Severity);
    }

    [Fact]
    public void NoIssues_When_ConfigureAwaitFalse_Present()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                async Task Bar() {
                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Detects_Multiple_Missing_ConfigureAwait()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                async Task Bar() {
                    await Task.Delay(1);
                    await Task.Delay(2);
                    await Task.Delay(3);
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Equal(3, issues.Count);
        Assert.All(issues, i => Assert.Equal("LMC003", i.RuleId));
    }

    [Fact]
    public void Mixed_With_And_Without_ConfigureAwait()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                async Task Bar() {
                    await Task.Delay(1);
                    await Task.Delay(2).ConfigureAwait(false);
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Contains("Task.Delay(1)", issues[0].CodeSnippet);
    }

    [Fact]
    public void NoIssues_When_No_Awaits()
    {
        var code = """
            class Foo {
                void Bar() {
                    var x = 1;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Empty(issues);
    }
}
