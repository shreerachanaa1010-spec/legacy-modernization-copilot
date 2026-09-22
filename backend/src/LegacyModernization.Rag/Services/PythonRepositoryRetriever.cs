using System.Diagnostics;
using System.Text.Json;
using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;

namespace LegacyModernization.Rag.Services;

public sealed class PythonRepositoryRetriever : IRepositoryRetriever
{
    private readonly FileSystemRepositoryRetriever _fallbackRetriever = new();

    public async Task<RetrievedContext> RetrieveAsync(
        AnalysisIssue issue,
        string projectRoot,
        CancellationToken cancellationToken = default)
    {
        var root = GetFullPath(projectRoot);
        var scriptPath = ResolveScriptPath(root);

        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
        {
            return await _fallbackRetriever.RetrieveAsync(issue, root, cancellationToken);
        }

        try
        {
            var query = BuildQuery(issue);
            var payload = await RunPythonQueryAsync(scriptPath, root, query, cancellationToken);

            if (payload is null)
            {
                return await _fallbackRetriever.RetrieveAsync(issue, root, cancellationToken);
            }

            var documents = ParseEvidence(payload);
            return documents.Count > 0
                ? new RetrievedContext { Documents = documents }
                : await _fallbackRetriever.RetrieveAsync(issue, root, cancellationToken);
        }
        catch
        {
            return await _fallbackRetriever.RetrieveAsync(issue, root, cancellationToken);
        }
    }

    private static string BuildQuery(AnalysisIssue issue)
    {
        var parts = new[]
        {
            issue.RuleId,
            issue.Title,
            issue.Description,
            issue.CodeSnippet
        };

        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static async Task<JsonDocument?> RunPythonQueryAsync(
        string scriptPath,
        string projectRoot,
        string query,
        CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = GetPythonExecutable(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.StartInfo.ArgumentList.Add(scriptPath);
        process.StartInfo.ArgumentList.Add("--repo-root");
        process.StartInfo.ArgumentList.Add(projectRoot);
        process.StartInfo.ArgumentList.Add("--query");
        process.StartInfo.ArgumentList.Add(query);
        process.StartInfo.ArgumentList.Add("--limit");
        process.StartInfo.ArgumentList.Add("5");

        var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            process.StartInfo.Environment["GEMINI_API_KEY"] = geminiKey;
        }

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Python RAG script failed: {stderr}");
        }

        if (string.IsNullOrWhiteSpace(stdout))
        {
            return null;
        }

        return JsonDocument.Parse(stdout);
    }

    private static List<RetrievedDocument> ParseEvidence(JsonDocument payload)
    {
        var documents = new List<RetrievedDocument>();
        var root = payload.RootElement;

        if (!root.TryGetProperty("evidence", out var evidenceElement) || evidenceElement.ValueKind != JsonValueKind.Array)
        {
            return documents;
        }

        foreach (var item in evidenceElement.EnumerateArray())
        {
            var sourcePath = item.TryGetProperty("source_path", out var pathElement)
                ? pathElement.GetString() ?? ""
                : string.Empty;

            var content = item.TryGetProperty("content", out var contentElement)
                ? contentElement.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(sourcePath) && string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            documents.Add(new RetrievedDocument
            {
                SourceType = "python-rag-evidence",
                RetrievalMethod = "vector-enrichment",
                EvidenceId = item.TryGetProperty("evidence_id", out var evidenceId)
                    ? evidenceId.GetString() ?? string.Empty
                    : string.Empty,
                Score = item.TryGetProperty("score", out var scoreElement) &&
                        scoreElement.TryGetDouble(out var score)
                    ? score
                    : null,
                Symbol = item.TryGetProperty("symbol", out var symbolElement)
                    ? symbolElement.GetString() ?? string.Empty
                    : string.Empty,
                FilePath = sourcePath,
                Content = content,
                LineStart = item.TryGetProperty("line_start", out var lineStartElement) &&
                            lineStartElement.TryGetInt32(out var lineStart)
                    ? lineStart
                    : null,
                LineEnd = item.TryGetProperty("line_end", out var lineEndElement) &&
                          lineEndElement.TryGetInt32(out var lineEnd)
                    ? lineEnd
                    : null
            });
        }

        return documents;
    }

    private static string GetFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Project root is required.", nameof(path));
        }

        return Path.GetFullPath(path);
    }

    private static string? ResolveScriptPath(string projectRoot)
    {
        var current = new DirectoryInfo(projectRoot);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "python", "agentic_rag.py");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string GetPythonExecutable()
    {
        var configured = Environment.GetEnvironmentVariable("PYTHON");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return "python";
    }
}
