using LegacyModernization.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;

namespace LegacyModernization.Analyzer.Tests;

public class WebClientRuleTests
{
    private readonly WebClientRule _rule = new();

    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void Detects_NewWebClient()
    {
        var code = """
            using System.Net;
            class Foo {
                void Bar() {
                    var client = new WebClient();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Contains(issues, i => i.RuleId == "LMC002" && i.CodeSnippet.Contains("WebClient"));
    }

    [Fact]
    public void Detects_FullyQualified_WebClient()
    {
        var code = """
            class Foo {
                void Bar() {
                    var client = new System.Net.WebClient();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Contains(issues, i => i.RuleId == "LMC002");
    }

    [Fact]
    public void NoIssues_When_Using_HttpClient()
    {
        var code = """
            using System.Net.Http;
            class Foo {
                void Bar() {
                    var client = new HttpClient();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void NoIssues_On_Empty_Class()
    {
        var code = "class Foo { }";

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void Severity_Is_Medium()
    {
        var code = """
            using System.Net;
            class Foo {
                void Bar() {
                    var client = new WebClient();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Contains(issues, i => i.Severity == "Medium");
    }
}
