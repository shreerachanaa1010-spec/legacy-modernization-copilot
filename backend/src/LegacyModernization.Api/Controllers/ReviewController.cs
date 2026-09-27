using System.Security.Cryptography;
using System.Text;
using LegacyModernization.Api.Models;
using LegacyModernization.Api.Services;
using LegacyModernization.Rag.Models;
using LegacyModernization.Rag.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LegacyModernization.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReviewController : ControllerBase
{
    private static readonly SemaphoreSlim FileChangeGate = new(1, 1);
    private readonly IReviewDecisionStore _reviewDecisionStore;

    public ReviewController(IReviewDecisionStore reviewDecisionStore)
    {
        _reviewDecisionStore = reviewDecisionStore;
    }

    [HttpPost]
    public async Task<IActionResult> SaveDecision(
        [FromBody] ReviewDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectPath) || request.Issue is null || request.Suggestion is null)
        {
            return BadRequest("ProjectPath, issue, and suggestion are required.");
        }

        var decision = request.Decision.Trim().ToLowerInvariant();
        if (decision is not ("approved" or "rejected" or "pending"))
        {
            return BadRequest("Decision must be approved, rejected, or pending.");
        }

        var projectFile = ProjectPathResolver.Resolve(request.ProjectPath);
        if (projectFile is null)
        {
            return NotFound($"No unique .csproj file was found for project path: {request.ProjectPath}");
        }

        var projectRoot = Path.GetDirectoryName(projectFile)!;
        var issue = request.Issue;
        var suggestion = request.Suggestion;
        var sourcePath = Path.GetFullPath(Path.IsPathRooted(issue.FilePath)
            ? issue.FilePath
            : Path.Combine(projectRoot, issue.FilePath));

        if (!IsWithinRoot(projectRoot, sourcePath) || !System.IO.File.Exists(sourcePath))
        {
            return BadRequest("The issue source file must exist inside the analyzed project.");
        }

        if (!string.Equals(issue.CodeSnippet, suggestion.OriginalCode, StringComparison.Ordinal))
        {
            return BadRequest("The suggestion's original code does not match the analyzed issue.");
        }

        var fingerprint = CreateFingerprint(issue, sourcePath);
        await FileChangeGate.WaitAsync(cancellationToken);
        try
        {
            var previous = await _reviewDecisionStore.GetDecisionAsync(fingerprint, cancellationToken);
            if (decision == "approved")
            {
                if (string.IsNullOrWhiteSpace(suggestion.OriginalCode) ||
                    string.IsNullOrWhiteSpace(suggestion.RefactoredCode))
                {
                    return Conflict("This suggestion does not contain an applicable code change.");
                }

                if (previous?.Decision == "approved")
                {
                    return Conflict("This suggestion has already been accepted. Reset it before applying it again.");
                }

                var result = await ReplaceSourceAsync(
                    sourcePath,
                    suggestion.OriginalCode,
                    suggestion.RefactoredCode,
                    cancellationToken);
                if (result.Error is not null)
                {
                    return Conflict(result.Error);
                }

                try
                {
                    await SaveDecisionAsync(fingerprint, "approved", sourcePath, suggestion, cancellationToken);
                }
                catch
                {
                    await System.IO.File.WriteAllTextAsync(sourcePath, result.OriginalFile!, CancellationToken.None);
                    throw;
                }

                return Ok(new ReviewDecisionResponse
                {
                    Decision = "approved",
                    Message = "Suggestion accepted and applied to the source file.",
                    FilePath = sourcePath
                });
            }

            if (decision == "rejected" && previous?.Decision == "approved")
            {
                return Conflict("This suggestion is already applied. Reset it first to restore the original code.");
            }

            if (decision == "pending" && previous?.Decision == "approved")
            {
                var result = await ReplaceSourceAsync(
                    sourcePath,
                    previous.RefactoredCode,
                    previous.OriginalCode,
                    cancellationToken);
                if (result.Error is not null)
                {
                    return Conflict($"Could not safely restore the original code: {result.Error}");
                }

                try
                {
                    await SaveDecisionAsync(fingerprint, "pending", sourcePath, suggestion, cancellationToken);
                }
                catch
                {
                    await System.IO.File.WriteAllTextAsync(sourcePath, result.OriginalFile!, CancellationToken.None);
                    throw;
                }

                return Ok(new ReviewDecisionResponse
                {
                    Decision = "pending",
                    Message = "Review reset and original source code restored.",
                    FilePath = sourcePath
                });
            }

            await SaveDecisionAsync(fingerprint, decision, sourcePath, suggestion, cancellationToken);
            return Ok(new ReviewDecisionResponse
            {
                Decision = decision,
                Message = decision == "rejected" ? "Suggestion rejected." : "Review reset.",
                FilePath = sourcePath
            });
        }
        finally
        {
            FileChangeGate.Release();
        }
    }

    private async Task<(string? OriginalFile, string? Error)> ReplaceSourceAsync(
        string filePath,
        string searchText,
        string replacement,
        CancellationToken cancellationToken)
    {
        var originalFile = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
        var matchIndex = originalFile.IndexOf(searchText, StringComparison.Ordinal);
        if (matchIndex < 0)
        {
            return (null, "The source no longer contains the original snippet. Re-run analysis before reviewing this suggestion.");
        }

        if (originalFile.IndexOf(searchText, matchIndex + searchText.Length, StringComparison.Ordinal) >= 0)
        {
            return (null, "The original snippet occurs more than once in the file, so the edit is ambiguous.");
        }

        var updatedFile = string.Concat(
            originalFile.AsSpan(0, matchIndex),
            replacement,
            originalFile.AsSpan(matchIndex + searchText.Length));
        var originalErrors = CountSyntaxErrors(originalFile, filePath);
        var updatedErrors = CountSyntaxErrors(updatedFile, filePath);
        if (updatedErrors > originalErrors)
        {
            return (null, "The proposed edit introduces C# syntax errors and was not applied.");
        }

        await System.IO.File.WriteAllTextAsync(filePath, updatedFile, cancellationToken);
        return (originalFile, null);
    }

    private async Task SaveDecisionAsync(
        string fingerprint,
        string decision,
        string filePath,
        LegacyModernization.Core.Models.RefactorSuggestion suggestion,
        CancellationToken cancellationToken) =>
        await _reviewDecisionStore.SaveDecisionAsync(new ReviewDecision
        {
            FindingFingerprint = fingerprint,
            Decision = decision,
            FilePath = filePath,
            OriginalCode = suggestion.OriginalCode,
            RefactoredCode = suggestion.RefactoredCode,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

    private static int CountSyntaxErrors(string source, string filePath) =>
        CSharpSyntaxTree.ParseText(source, path: filePath)
            .GetDiagnostics()
            .Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    private static string CreateFingerprint(
        LegacyModernization.Core.Models.AnalysisIssue issue,
        string sourcePath) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{issue.RuleId}:{sourcePath}:{issue.LineNumber}:{issue.CodeSnippet}"))).ToLowerInvariant();

    private static bool IsWithinRoot(string root, string path)
    {
        var rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(fullPath, Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase);
    }
}