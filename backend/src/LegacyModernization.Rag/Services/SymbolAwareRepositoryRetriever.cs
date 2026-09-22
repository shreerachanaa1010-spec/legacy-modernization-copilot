using LegacyModernization.Core.Models;
using LegacyModernization.Rag.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography;
using System.Text;

namespace LegacyModernization.Rag.Services;

public sealed class SymbolAwareRepositoryRetriever : IRepositoryRetriever
{
    private readonly FileSystemRepositoryRetriever _filesystem;

    public SymbolAwareRepositoryRetriever(FileSystemRepositoryRetriever filesystem)
    {
        _filesystem = filesystem;
    }

    public async Task<RetrievedContext> RetrieveAsync(
        AnalysisIssue issue,
        string projectRoot,
        CancellationToken cancellationToken = default)
    {
        var context = await _filesystem.RetrieveAsync(issue, projectRoot, cancellationToken);
        var documents = context.Documents.ToList();
        var primary = documents.FirstOrDefault(document => document.SourceType == "primary-source");
        if (primary is null || !File.Exists(primary.FilePath))
        {
            return context;
        }

        var source = await File.ReadAllTextAsync(primary.FilePath, cancellationToken);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = await tree.GetRootAsync(cancellationToken);
        var containingMethod = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(method => method.SpanStart <= GetPosition(source, issue.LineNumber) &&
                                      method.Span.End >= GetPosition(source, issue.LineNumber));
        var containingClass = containingMethod?.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()
            ?? root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if (containingMethod is null && containingClass is null)
        {
            return context;
        }

        var symbol = containingMethod is null
            ? containingClass!.Identifier.Text
            : $"{containingClass?.Identifier.Text}.{containingMethod.Identifier.Text}";
        var lineSpan = containingMethod?.GetLocation().GetLineSpan()
            ?? containingClass!.GetLocation().GetLineSpan();
        var enrichedPrimary = new RetrievedDocument
        {
            EvidenceId = primary.EvidenceId,
            SourceType = primary.SourceType,
            RetrievalMethod = "deterministic-roslyn",
            Score = primary.Score,
            FilePath = primary.FilePath,
            ContentHash = primary.ContentHash,
            Namespace = primary.Namespace,
            Symbol = symbol,
            Content = primary.Content,
            LineStart = lineSpan.StartLinePosition.Line + 1,
            LineEnd = lineSpan.EndLinePosition.Line + 1
        };

        documents[documents.IndexOf(primary)] = enrichedPrimary;
        var related = await FindRelatedEvidenceAsync(
            root,
            projectRoot,
            containingMethod?.Identifier.Text,
            containingClass?.Identifier.Text,
            cancellationToken);
        documents.AddRange(related.Where(candidate =>
            documents.All(existing => !PathsEqual(existing.FilePath, candidate.FilePath))));
        return new RetrievedContext { Documents = documents };
    }

    private static async Task<IReadOnlyList<RetrievedDocument>> FindRelatedEvidenceAsync(
        SyntaxNode root,
        string projectRoot,
        string? methodName,
        string? className,
        CancellationToken cancellationToken)
    {
        var results = new List<RetrievedDocument>();
        var sourceFiles = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase));

        foreach (var path in sourceFiles.Take(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            if (content.Length == 0)
            {
                continue;
            }

            var tree = CSharpSyntaxTree.ParseText(content);
            var candidateRoot = await tree.GetRootAsync(cancellationToken);
            var containsCall = methodName is not null && candidateRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => invocation.Expression.ToString().EndsWith($".{methodName}", StringComparison.Ordinal) ||
                                   invocation.Expression.ToString().Equals(methodName, StringComparison.Ordinal));
            var containsImplementation = className is not null && candidateRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Any(candidate => candidate.Identifier.Text.Equals(className, StringComparison.Ordinal));
            var isTest = Path.GetFileName(path).Contains("test", StringComparison.OrdinalIgnoreCase) ||
                         path.Contains(".Tests", StringComparison.OrdinalIgnoreCase);

            if (!containsCall && !containsImplementation && !isTest)
            {
                continue;
            }

            var sourceType = containsCall
                ? "related-caller"
                : containsImplementation ? "related-implementation" : "related-test";
            var lineEnd = content.Split('\n').Length;
            results.Add(new RetrievedDocument
            {
                EvidenceId = CreateEvidenceId(path, 1, lineEnd, content),
                SourceType = sourceType,
                RetrievalMethod = "deterministic-roslyn",
                Score = containsCall ? 0.8 : 0.6,
                FilePath = path,
                Symbol = className ?? Path.GetFileNameWithoutExtension(path),
                Content = content,
                LineStart = 1,
                LineEnd = lineEnd
            });
        }

        return results;
    }

    private static int GetPosition(string source, int lineNumber)
    {
        if (lineNumber <= 1)
        {
            return 0;
        }

        var position = 0;
        for (var line = 1; line < lineNumber && position < source.Length; line++)
        {
            var newline = source.IndexOf('\n', position);
            position = newline < 0 ? source.Length : newline + 1;
        }

        return position;
    }

    private static string CreateEvidenceId(string path, int lineStart, int lineEnd, string content)
    {
        var value = $"{Path.GetFullPath(path)}:{lineStart}:{lineEnd}:{content}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}
