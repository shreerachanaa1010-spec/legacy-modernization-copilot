using LegacyModernization.Api.Models;
using LegacyModernization.Verifier.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyModernization.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VerificationController : ControllerBase
{
    private readonly VerificationService _verifier;

    public VerificationController(VerificationService verifier)
    {
        _verifier = verifier;
    }

    /// <summary>
    /// Run generated tests against a test project and return verification status.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TestProjectPath))
            return BadRequest("TestProjectPath is required.");

        var fullPath = Path.GetFullPath(request.TestProjectPath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound($"Test project file not found: {fullPath}");

        var result = await _verifier.VerifyAsync(fullPath);
        return Ok(result);
    }
}
