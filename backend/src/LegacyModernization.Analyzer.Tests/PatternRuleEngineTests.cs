using LegacyModernization.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;

namespace LegacyModernization.Analyzer.Tests;

public class PatternRuleEngineTests
{
    private readonly PatternRuleEngine _engine = new();

    private static Microsoft.CodeAnalysis.SyntaxNode Parse(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void Aggregates_Issues_From_All_Rules()
    {
        // Code with issues for all 4 rules
        var code = """
            using System;
            using System.Net;
            using System.Threading.Tasks;

            class Service : IDisposable {
                void Bad() {
                    var t = Task.FromResult(1);
                    var r = t.Result;
                    var client = new WebClient();
                }

                async Task Async() {
                    await Task.Delay(1);
                }

                public void Dispose() { }
            }
            """;

        var issues = _engine.Analyze(Parse(code), "test.cs");

        // LMC001 (Result), LMC002 (WebClient creation + member accesses), LMC003 (missing ConfigureAwait), LMC004 (dispose)
        Assert.Contains(issues, i => i.RuleId == "LMC001");
        Assert.Contains(issues, i => i.RuleId == "LMC002");
        Assert.Contains(issues, i => i.RuleId == "LMC003");
        Assert.Contains(issues, i => i.RuleId == "LMC004");
    }

    [Fact]
    public void Returns_Empty_For_Clean_Code()
    {
        var code = """
            using System;
            using System.Threading.Tasks;

            class Service : IDisposable {
                async Task Good() {
                    var r = await Task.FromResult(1).ConfigureAwait(false);
                }

                public void Dispose() {
                    Dispose(true);
                    GC.SuppressFinalize(this);
                }
                protected virtual void Dispose(bool disposing) { }
            }
            """;

        var issues = _engine.Analyze(Parse(code), "test.cs");

        Assert.Empty(issues);
    }

    [Fact]
    public void FilePath_Is_Propagated_To_All_Issues()
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

        var issues = _engine.Analyze(Parse(code), "MyFile.cs");

        Assert.All(issues, i => Assert.Equal("MyFile.cs", i.FilePath));
    }

    [Fact]
    public void Returns_Empty_For_Empty_File()
    {
        var issues = _engine.Analyze(Parse(""), "empty.cs");

        Assert.Empty(issues);
    }
}
