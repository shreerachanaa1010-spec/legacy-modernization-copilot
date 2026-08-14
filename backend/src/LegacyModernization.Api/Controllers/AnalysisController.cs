using LegacyModernization.Analyzer.Services;
using LegacyModernization.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace LegacyModernization.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IProjectAnalyzer _analyzer;

    public AnalysisController(IProjectAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    /// <summary>
    /// Analyze a .NET project for legacy patterns.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectPath))
            return BadRequest("ProjectPath is required.");

        var fullPath = Path.GetFullPath(request.ProjectPath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound($"Project file not found: {fullPath}");

        var result = await _analyzer.AnalyzeAsync(fullPath);
        return Ok(result);
    }
}
