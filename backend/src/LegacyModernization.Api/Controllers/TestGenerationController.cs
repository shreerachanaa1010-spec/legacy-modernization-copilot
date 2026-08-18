using LegacyModernization.Analyzer.Services;
using LegacyModernization.Api.Models;
using LegacyModernization.TestGenerator.Models;
using LegacyModernization.TestGenerator.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyModernization.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestGenerationController : ControllerBase
{
    private readonly IProjectAnalyzer _analyzer;
    private readonly ITestGenerator _testGenerator;

    public TestGenerationController(IProjectAnalyzer analyzer, ITestGenerator testGenerator)
    {
        _analyzer = analyzer;
        _testGenerator = testGenerator;
    }

    /// <summary>
    /// Analyze a project and generate xUnit tests for all detected legacy issues.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GenerateTests([FromBody] AnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectPath))
            return BadRequest("ProjectPath is required.");

        var fullPath = Path.GetFullPath(request.ProjectPath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound($"Project file not found: {fullPath}");

        var analysis = await _analyzer.AnalyzeAsync(fullPath);

        var tests = new List<GeneratedTest>();

        foreach (var issue in analysis.Issues)
        {
            try
            {
                var test = await _testGenerator.GenerateTestAsync(issue);
                tests.Add(test);
            }
            catch (Exception ex)
            {
                tests.Add(new GeneratedTest
                {
                    TestClassName = $"{issue.RuleId}GeneratedTests",
                    TestCode = "",
                    TargetFile = issue.FilePath,
                    Explanation = $"Generation error: {ex.Message}"
                });
            }
        }

        return Ok(tests);
    }
}
