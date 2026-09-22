using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegacyModernization.LLM.Services;

internal sealed class StructuredSuggestionResponse
{
    public string Summary { get; init; } = "";
    public string Risk { get; init; } = "";
    public List<string> Preconditions { get; init; } = [];
    public string Patch { get; init; } = "";
    public List<string> EvidenceIds { get; init; } = [];
    public List<string> Assumptions { get; init; } = [];
    public List<string> TestPlan { get; init; } = [];
    public double? Confidence { get; init; }
    public string Status { get; init; } = "ok";
}

internal static class StructuredSuggestionParser
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static bool TryParse(
        string? responseText,
        IReadOnlySet<string> availableEvidenceIds,
        out StructuredSuggestionResponse response,
        out string error)
    {
        response = new StructuredSuggestionResponse();
        error = "";
        if (string.IsNullOrWhiteSpace(responseText))
        {
            error = "The model returned an empty response.";
            return false;
        }

        var json = StripCodeFence(responseText);
        try
        {
            response = JsonSerializer.Deserialize<StructuredSuggestionResponse>(json, Options)
                ?? throw new JsonException("The response was null.");
        }
        catch (JsonException exception)
        {
            error = $"The model response was not valid JSON: {exception.Message}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(response.Summary) ||
            string.IsNullOrWhiteSpace(response.Risk) ||
            string.IsNullOrWhiteSpace(response.Status))
        {
            error = "The structured response is missing summary, risk, or status.";
            return false;
        }

        var unknownEvidenceIds = response.EvidenceIds
            .Where(id => !availableEvidenceIds.Contains(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (unknownEvidenceIds.Length > 0)
        {
            error = $"The response cited unavailable evidence IDs: {string.Join(", ", unknownEvidenceIds)}.";
            return false;
        }

        if (!string.Equals(response.Status, "insufficient_evidence", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(response.Patch))
        {
            error = "A non-insufficient response must contain a patch.";
            return false;
        }

        return true;
    }

    private static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLineEnd = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd >= 0 && lastFence > firstLineEnd
            ? trimmed[(firstLineEnd + 1)..lastFence].Trim()
            : trimmed;
    }
}