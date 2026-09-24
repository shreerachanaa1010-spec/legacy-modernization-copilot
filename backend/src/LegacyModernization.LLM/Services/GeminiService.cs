using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;
using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;

namespace LegacyModernization.LLM.Services;

public class GeminiService : ILlmService
{
    private readonly string? _apiKey;

    public GeminiService(IConfiguration configuration)
    {
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                 ?? configuration["Gemini:ApiKey"];
    }

    public async Task<RefactorSuggestion> GenerateSuggestionAsync(
        AnalysisIssue issue,
        RetrievedContext context)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new RefactorSuggestion
            {
                RuleId = issue.RuleId,
                IssueTitle = issue.Title,
                Reason = issue.Description,
                OriginalCode = issue.CodeSnippet,
                Explanation = "No external model is configured. Configure GEMINI_API_KEY for generation, or review the deterministic evidence manually.",
                GenerationStatus = "model-not-configured",
                IsSafe = false
            };
        }

        try
        {
            var googleAI = new GoogleAI(_apiKey);
            var model = googleAI.GenerativeModel(GetModelName());
            if (model is null)
            {
                return CreateGenerationFailure(issue, "Gemini did not return a generative model.");
            }

            var documents = context?.Documents ?? [];
            var retrievedContext = string.Join(
                Environment.NewLine + Environment.NewLine,
                documents.Select(document =>
                    $"Evidence ID: {document.EvidenceId}; source: {document.SourceType}; method: {document.RetrievalMethod}; score: {document.Score?.ToString("F4") ?? "n/a"}; " +
                    $"file: {document.FilePath}; lines: {document.LineStart}-{document.LineEnd}; symbol: {document.Symbol}" +
                    $"{Environment.NewLine}{document.Content}"));

            var prompt = $$"""
You are a senior .NET modernization expert.

Rule:
{{issue.RuleId}}

Title:
{{issue.Title}}

Description:
{{issue.Description}}

Code:
{{issue.CodeSnippet}}

Repository context retrieved for this issue:
{{retrievedContext}}

Return only JSON with this shape:
{
    "summary": "why the issue matters and the proposed modernization",
    "risk": "regression or compatibility risk",
    "preconditions": ["required conditions"],
    "patch": "corrected C# code, or an empty string when evidence is insufficient",
    "evidenceIds": ["IDs from the context above"],
    "assumptions": ["explicit assumptions"],
    "testPlan": ["tests needed to verify the change"],
    "confidence": 0.0,
    "status": "ok or insufficient_evidence"
}

Evidence requirements:
- Cite only Evidence IDs present in the repository context.
- Do not invent files, symbols, or evidence IDs.
- Return insufficient_evidence when the context does not support a claim.
""";

            var generationTask = model.GenerateContent(prompt);
            var completedTask = await Task.WhenAny(
                generationTask,
                Task.Delay(GetTimeout()));

            if (completedTask != generationTask)
            {
                return CreateLocalFallback(
                    issue,
                    $"Gemini timed out after {GetTimeout().TotalSeconds:0} seconds. Showing a deterministic rule-based recommendation.");
            }

            var response = await generationTask;
            var responseText = response?.Text;
            var evidenceIds = documents
            .Select(document => document.EvidenceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

            if (!StructuredSuggestionParser.TryParse(responseText, evidenceIds, out var structured, out var parseError))
            {
                return CreateGenerationFailure(issue, parseError, "invalid-structured-response");
            }

            return new RefactorSuggestion
            {
                RuleId = issue.RuleId,
                IssueTitle = issue.Title,
                Reason = issue.Description,
                OriginalCode = issue.CodeSnippet,
                RefactoredCode = structured.Patch,
                Explanation = structured.Summary,
                EvidenceIds = structured.EvidenceIds,
                Confidence = structured.Confidence,
                GenerationStatus = structured.Status,
                IsSafe = false
            };
        }
        catch (Exception exception)
        {
            return CreateLocalFallback(
                issue,
                $"Gemini request failed: {exception.GetBaseException().Message}");
        }
    }

    private static RefactorSuggestion CreateLocalFallback(AnalysisIssue issue, string reason) =>
        new()
        {
            RuleId = issue.RuleId,
            IssueTitle = issue.Title,
            Reason = issue.Description,
            OriginalCode = issue.CodeSnippet,
            RefactoredCode = issue.RuleId switch
            {
                "LMC001" when issue.CodeSnippet.Contains(".Result", StringComparison.Ordinal) =>
                    "var result = await task;",
                "LMC001" when issue.CodeSnippet.Contains(".Wait", StringComparison.Ordinal) =>
                    "await task;",
                "LMC002" => "using var client = new HttpClient();",
                "LMC003" => $"{issue.CodeSnippet}.ConfigureAwait(false)",
                "LMC004" => "protected virtual void Dispose(bool disposing) { }\n\npublic void Dispose()\n{\n    Dispose(true);\n    GC.SuppressFinalize(this);\n}",
                _ => "Review the detected pattern and apply the recommended modern .NET equivalent."
            },
            Explanation = $"{reason} This local recommendation is not verified; review and test it before applying.",
            GenerationStatus = "local-fallback",
            IsSafe = false
        };

    private static RefactorSuggestion CreateGenerationFailure(
        AnalysisIssue issue,
        string explanation,
        string status = "generation-error") =>
        new()
        {
            RuleId = issue.RuleId,
            IssueTitle = issue.Title,
            Reason = issue.Description,
            OriginalCode = issue.CodeSnippet,
            Explanation = explanation,
            GenerationStatus = status,
            IsSafe = false
        };

    private static TimeSpan GetTimeout() =>
        TimeSpan.FromSeconds(
            int.TryParse(Environment.GetEnvironmentVariable("GEMINI_TIMEOUT_SECONDS"), out var seconds)
                ? Math.Clamp(seconds, 5, 300)
                : 20);

    private static string GetModelName() =>
        Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-3.8-flash";
}