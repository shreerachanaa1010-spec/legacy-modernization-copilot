using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;
using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;

namespace LegacyModernization.LLM.Services;

public class GeminiService : ILlmService
{{
    private readonly GoogleAI _googleAI;

    public GeminiService(IConfiguration configuration)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                     ?? configuration["Gemini:ApiKey"]
                     ?? throw new Exception("Gemini API key not found.");

        _googleAI = new GoogleAI(apiKey);
    }

    public async Task<RefactorSuggestion> GenerateSuggestionAsync(
        AnalysisIssue issue,
        RetrievedContext context)
    {
        var model = _googleAI.GenerativeModel("gemini-3.6-flash");

        var retrievedContext = string.Join(
            Environment.NewLine + Environment.NewLine,
            context.Documents.Select(document =>
                $"Evidence ID: {document.EvidenceId}; source: {document.SourceType}; method: {document.RetrievalMethod}; score: {document.Score?.ToString("F4") ?? "n/a"}; " +
                $"file: {document.FilePath}; lines: {document.LineStart}-{document.LineEnd}; symbol: {document.Symbol}" +
                $"{Environment.NewLine}{document.Content}"));

        var prompt = $"""
You are a senior .NET modernization expert.

Rule:
{issue.RuleId}

Title:
{issue.Title}

Description:
{issue.Description}

Code:
{issue.CodeSnippet}

Repository context retrieved for this issue:
{retrievedContext}

Return only JSON with this shape:
{{
    "summary": "why the issue matters and the proposed modernization",
    "risk": "regression or compatibility risk",
    "preconditions": ["required conditions"],
    "patch": "corrected C# code, or an empty string when evidence is insufficient",
    "evidenceIds": ["IDs from the context above"],
    "assumptions": ["explicit assumptions"],
    "testPlan": ["tests needed to verify the change"],
    "confidence": 0.0,
    "status": "ok or insufficient_evidence"
}}

Evidence requirements:
- Cite only Evidence IDs present in the repository context.
- Do not invent files, symbols, or evidence IDs.
- Return insufficient_evidence when the context does not support a claim.
""";

        var response = await model.GenerateContent(prompt);
        var evidenceIds = context.Documents
            .Select(document => document.EvidenceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        if (!StructuredSuggestionParser.TryParse(response.Text, evidenceIds, out var structured, out var parseError))
        {
            return new RefactorSuggestion
            {
                RuleId = issue.RuleId,
                IssueTitle = issue.Title,
                Reason = issue.Description,
                OriginalCode = issue.CodeSnippet,
                Explanation = parseError,
                GenerationStatus = "invalid-structured-response",
                IsSafe = false
            };
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
}}