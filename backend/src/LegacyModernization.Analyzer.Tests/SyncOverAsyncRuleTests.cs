using LegacyModernization.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;

namespace LegacyModernization.Analyzer.Tests;

public class SyncOverAsyncRuleTests
{
    private readonly SyncOverAsyncRule _rule = new();

    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void Detects_TaskResult()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                void Bar() {
                    var t = Task.FromResult(1);
                    var r = t.Result;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC001", issues[0].RuleId);
        Assert.Contains("Result", issues[0].Title);
        Assert.Equal("High", issues[0].Severity);
        Assert.Equal("test.cs", issues[0].FilePath);
    }

    [Fact]
    public void Detects_TaskWait()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                void Bar() {
                    var t = Task.FromResult(1);
                    t.Wait();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Single(issues);
        Assert.Equal("LMC001", issues[0].RuleId);
        Assert.Contains("Wait", issues[0].Title);
    }

    [Fact]
    public void Detects_Both_Result_And_Wait()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                void Bar() {
                    var t = Task.FromResult(1);
                    var r = t.Result;
                    t.Wait();
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Equal(2, issues.Count);
        Assert.All(issues, i => Assert.Equal("LMC001", i.RuleId));
    }

    [Fact]
    public void NoIssues_When_Using_Await()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                async Task Bar() {
                    var r = await Task.FromResult(1);
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
    public void LineNumber_Is_Correct()
    {
        var code = """
            using System.Threading.Tasks;
            class Foo {
                void Bar() {
                    var t = Task.FromResult(1);
                    var r = t.Result;
                }
            }
            """;

        var issues = _rule.Analyze(Parse(code), "test.cs").ToList();

        Assert.Equal(5, issues[0].LineNumber);
    }
}
