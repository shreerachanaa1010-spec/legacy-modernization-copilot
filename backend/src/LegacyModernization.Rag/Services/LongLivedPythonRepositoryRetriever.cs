using System.Diagnostics;
using System.Text.Json;
using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;

namespace LegacyModernization.Rag.Services;

public sealed class LongLivedPythonRepositoryRetriever : IRepositoryRetriever, IDisposable
{
    private const int DefaultMaxOutputBytes = 1_048_576;
    private readonly FileSystemRepositoryRetriever _fallback = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Process? _process;
    private StreamWriter? _writer;
    private StreamReader? _reader;

    public async Task<RetrievedContext> RetrieveAsync(
        AnalysisIssue issue,
        string projectRoot,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(projectRoot);
        var scriptPath = ResolveScriptPath(root);
        if (scriptPath is null)
        {
            return await _fallback.RetrieveAsync(issue, root, cancellationToken);
        }

        try
        {
            var payload = await QueryAsync(scriptPath, root, BuildQuery(issue), cancellationToken);
            var documents = ParseEvidence(payload);
            return documents.Count == 0
                ? await _fallback.RetrieveAsync(issue, root, cancellationToken)
                : new RetrievedContext { Documents = documents };
        }
        catch
        {
            StopProcess();
            return await _fallback.RetrieveAsync(issue, root, cancellationToken);
        }
    }

    private async Task<JsonDocument> QueryAsync(
        string scriptPath,
        string projectRoot,
        string query,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureProcessAsync(scriptPath, projectRoot, cancellationToken);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(GetIntSetting("RAG_PYTHON_TIMEOUT_SECONDS", 30)));

            await _writer!.WriteLineAsync(JsonSerializer.Serialize(new { query, limit = 5 }));
            await _writer.FlushAsync(timeoutSource.Token);
            var line = await _reader!.ReadLineAsync(timeoutSource.Token);
            if (string.IsNullOrWhiteSpace(line) || line.Length > GetIntSetting("RAG_PYTHON_MAX_OUTPUT_BYTES", DefaultMaxOutputBytes))
            {
                throw new InvalidOperationException("Python retrieval response was empty or exceeded its output limit.");
            }

            var payload = JsonDocument.Parse(line);
            if (payload.RootElement.TryGetProperty("error", out var error))
            {
                throw new InvalidOperationException(error.GetString() ?? "Python retrieval failed.");
            }

            return payload;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureProcessAsync(string scriptPath, string projectRoot, CancellationToken cancellationToken)
    {
        if (_process is { HasExited: false })
        {
            return;
        }

        StopProcess();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("PYTHON") ?? "python",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add(scriptPath);
        process.StartInfo.ArgumentList.Add("--repo-root");
        process.StartInfo.ArgumentList.Add(projectRoot);
        process.StartInfo.ArgumentList.Add("--server");
        var geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            process.StartInfo.Environment["GEMINI_API_KEY"] = geminiKey;
        }

        process.Start();
        _process = process;
        _writer = process.StandardInput;
        _reader = process.StandardOutput;
        _ = process.StandardError.ReadToEndAsync(cancellationToken);
    }

    private static List<RetrievedDocument> ParseEvidence(JsonDocument payload)
    {
        if (!payload.RootElement.TryGetProperty("evidence", out var evidence) || evidence.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return evidence.EnumerateArray().Select(item => new RetrievedDocument
        {
            SourceType = "python-rag-evidence",
            RetrievalMethod = "python-hybrid-enrichment",
            EvidenceId = item.TryGetProperty("evidence_id", out var evidenceId) ? evidenceId.GetString() ?? "" : "",
            FilePath = item.TryGetProperty("source_path", out var path) ? path.GetString() ?? "" : "",
            Content = item.TryGetProperty("content", out var content) ? content.GetString() ?? "" : "",
            Score = item.TryGetProperty("score", out var score) && score.TryGetDouble(out var value) ? value : null,
            Symbol = item.TryGetProperty("symbol", out var symbol) ? symbol.GetString() ?? "" : "",
            LineStart = item.TryGetProperty("line_start", out var start) && start.TryGetInt32(out var startValue) ? startValue : null,
            LineEnd = item.TryGetProperty("line_end", out var end) && end.TryGetInt32(out var endValue) ? endValue : null
        }).Where(document => !string.IsNullOrWhiteSpace(document.FilePath) || !string.IsNullOrWhiteSpace(document.Content)).ToList();
    }

    private static string BuildQuery(AnalysisIssue issue) => string.Join(" ", new[]
    {
        issue.RuleId, issue.Title, issue.Description, issue.CodeSnippet
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string? ResolveScriptPath(string projectRoot)
    {
        for (var directory = new DirectoryInfo(projectRoot); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "python", "agentic_rag.py");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static int GetIntSetting(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? Math.Max(1, value) : fallback;

    private void StopProcess()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }

        _process.Dispose();
        _process = null;
        _writer = null;
        _reader = null;
    }

    public void Dispose()
    {
        StopProcess();
        _gate.Dispose();
    }
}
