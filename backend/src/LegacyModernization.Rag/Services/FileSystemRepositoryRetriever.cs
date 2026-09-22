using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;
using System.Security.Cryptography;
using System.Text;

namespace LegacyModernization.Rag.Services;

public sealed class FileSystemRepositoryRetriever : IRepositoryRetriever
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".csproj",
            ".sln",
            ".json",
            ".md"
        };

    public async Task<RetrievedContext> RetrieveAsync(
        AnalysisIssue issue,
        string projectRoot,
        CancellationToken cancellationToken = default)
    {
        var root = GetFullPath(projectRoot);
        var primaryPath = ResolvePath(root, issue.FilePath);
        var documents = new List<RetrievedDocument>();

        if (primaryPath is not null)
        {
            documents.Add(await ReadDocumentAsync(
                primaryPath,
                "primary-source",
                issue.LineNumber > 0 ? issue.LineNumber : 1,
                issue.LineNumber > 0 ? issue.LineNumber : null,
                cancellationToken));
        }

        foreach (var relatedPath in FindRelatedFiles(root, primaryPath))
        {
            documents.Add(await ReadDocumentAsync(
                relatedPath,
                IsTestFile(relatedPath) ? "related-test" : "related-source",
                1,
                null,
                cancellationToken));
        }

        return new RetrievedContext
        {
            Documents = documents
        };
    }

    private static string GetFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Project root is required.", nameof(path));

        return Path.GetFullPath(path);
    }

    private static string? ResolvePath(string root, string issuePath)
    {
        if (string.IsNullOrWhiteSpace(issuePath))
            return null;

        var candidate = Path.IsPathRooted(issuePath)
            ? Path.GetFullPath(issuePath)
            : Path.GetFullPath(Path.Combine(root, issuePath));

        return IsWithinRoot(root, candidate) && File.Exists(candidate)
            ? candidate
            : null;
    }

    private static IEnumerable<string> FindRelatedFiles(string root, string? primaryPath)
    {
        if (primaryPath is null)
            return [];

        var directory = Path.GetDirectoryName(primaryPath);
        if (directory is null)
            return [];

        return Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => !PathsEqual(path, primaryPath))
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .Where(path => IsTestFile(path) ||
                           string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToArray();
    }

    private static async Task<RetrievedDocument> ReadDocumentAsync(
        string path,
        string sourceType,
        int lineStart,
        int? lineEnd,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var totalLines = content.Split('\n').Length;
        var resolvedLineEnd = lineEnd ?? totalLines;
        return new RetrievedDocument
        {
            EvidenceId = CreateEvidenceId(path, lineStart, resolvedLineEnd, content),
            SourceType = sourceType,
            RetrievalMethod = "deterministic-filesystem",
            Score = string.Equals(sourceType, "primary-source", StringComparison.Ordinal)
                ? 1.0
                : 0.5,
            FilePath = path,
            Symbol = Path.GetFileNameWithoutExtension(path),
            LineStart = lineStart,
            LineEnd = resolvedLineEnd,
            Content = content
        };
    }

    private static string CreateEvidenceId(string path, int lineStart, int lineEnd, string content)
    {
        var fingerprint = $"{Path.GetFullPath(path)}:{lineStart}:{lineEnd}:{content}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)))[..16].ToLowerInvariant();
    }

    private static bool IsTestFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Contains("test", StringComparison.OrdinalIgnoreCase) ||
               path.Contains(".Tests", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWithinRoot(string root, string path)
    {
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return PathsEqual(root, path) ||
               path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);
}
