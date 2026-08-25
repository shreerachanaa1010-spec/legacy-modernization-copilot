using LegacyModernization.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;

namespace LegacyModernization.Analyzer.Tests;

public class HttpClientInstantiationRuleTests
{
    private readonly HttpClientInstantiationRule _rule = new();

    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void Detects_New_HttpClient()
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

        Assert.Single(issues);
        Assert.Equal("LMC005", issues[0].RuleId);
        Assert.Equal("High", issues[0].Severity);
    }

    [Fact]
    public void Detects_FullyQualified_HttpClient()
    {
        var code = """
            class Foo {
                void Bar() {
                    var client = new System.Net.Http.HttpClient();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC005", issues[0].RuleId);
    }

    [Fact]
    public void Detects_Implicit_New_HttpClient()
    {
        var code = """
            using System.Net.Http;
            class Foo {
                void Bar() {
                    HttpClient client = new();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC005", issues[0].RuleId);
    }

    [Fact]
    public void NoIssues_When_No_HttpClient_Created()
    {
        var code = """
            class Foo {
                void Bar() {
                    var x = new object();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void NoIssues_On_Empty_Class()
    {
        var issues = _rule.Analyze(Parse("class Foo { }"), "test.cs").ToList();

        Assert.Empty(issues);
    }
}
