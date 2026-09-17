using LegacyModernization.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;

namespace LegacyModernization.Analyzer.Tests;

public class DateTimeNowRuleTests
{
    private readonly DateTimeNowRule _rule = new();

    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void Detects_DateTimeNow()
    {
        var code = """
            using System;
            class Foo {
                void Bar() {
                    var now = DateTime.Now;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC006", issues[0].RuleId);
        Assert.Contains("Now", issues[0].Title);
    }

    [Fact]
    public void Detects_DateTimeUtcNow()
    {
        var code = """
            using System;
            class Foo {
                void Bar() {
                    var now = DateTime.UtcNow;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC006", issues[0].RuleId);
        Assert.Contains("UtcNow", issues[0].Title);
    }

    [Fact]
    public void Detects_DateTimeToday()
    {
        var code = """
            using System;
            class Foo {
                void Bar() {
                    var today = DateTime.Today;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC006", issues[0].RuleId);
        Assert.Contains("Today", issues[0].Title);
    }

    [Fact]
    public void Detects_FullyQualified_DateTime()
    {
        var code = """
            class Foo {
                void Bar() {
                    var now = System.DateTime.Now;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC006", issues[0].RuleId);
    }

    [Fact]
    public void NoIssues_When_Using_DateTimeOffset()
    {
        var code = """
            using System;
            class Foo {
                void Bar() {
                    var now = DateTimeOffset.Now;
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

    [Fact]
    public void Severity_Is_Low()
    {
        var code = """
            using System;
            class Foo {
                void Bar() {
                    var now = DateTime.Now;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Equal("Low", issues[0].Severity);
    }

    [Fact]
    public void Detects_Multiple_Occurrences()
    {
        var code = """
            using System;
            class Foo {
                void Bar() {
                    var a = DateTime.Now;
                    var b = DateTime.UtcNow;
                    var c = DateTime.Today;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Equal(3, issues.Count);
        Assert.All(issues, i => Assert.Equal("LMC006", i.RuleId));
    }
}
