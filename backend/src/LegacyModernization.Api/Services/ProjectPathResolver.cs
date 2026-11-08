namespace LegacyModernization.Api.Services;

public static class ProjectPathResolver
{
    public static bool IsDemoMode =>
        bool.TryParse(Environment.GetEnvironmentVariable("DEMO_MODE"), out var enabled) && enabled;

    public static string? Resolve(string input, bool demoMode = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        string path;
        try
        {
            path = Path.GetFullPath(input.Trim());
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        if (demoMode && !IsUnderSamples(path))
        {
            return null;
        }

        if (File.Exists(path) && Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return !demoMode || IsAllowedDemoProject(path) ? path : null;
        }

        if (!Directory.Exists(path))
        {
            return null;
        }

        var projects = Directory.EnumerateFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).ToArray();
        return projects.Length == 1 && (!demoMode || IsAllowedDemoProject(projects[0])) ? projects[0] : null;
    }

    public static string? ResolveProjectFile(string input, bool demoMode = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        string path;
        try
        {
            path = Path.GetFullPath(input.Trim());
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        return Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase) &&
               File.Exists(path) &&
               (!demoMode || IsAllowedDemoTestProject(path))
            ? path
            : null;
    }

    private static bool IsAllowedDemoProject(string path) =>
        IsExpectedDemoPath(path, Path.Combine("LegacySampleProject", "LegacySampleProject.csproj"));

    private static bool IsAllowedDemoTestProject(string path) =>
        IsExpectedDemoPath(path, Path.Combine("LegacySampleProject.Tests", "LegacySampleProject.Tests.csproj"));

    private static bool IsExpectedDemoPath(string path, string relativePath)
    {
        var samplesRoot = FindSamplesRoot();
        if (samplesRoot is null)
        {
            return false;
        }

        var expectedPath = Path.GetFullPath(Path.Combine(samplesRoot, relativePath));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(path), expectedPath, comparison);
    }

    private static bool IsUnderSamples(string path)
    {
        var samplesRoot = FindSamplesRoot();
        return samplesRoot is not null && IsWithinRoot(samplesRoot, path);
    }

    private static string? FindSamplesRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                var samplesRoot = Path.Combine(directory.FullName, "samples");
                if (Directory.Exists(samplesRoot))
                {
                    return samplesRoot;
                }
            }
        }

        return null;
    }

    private static bool IsWithinRoot(string root, string path)
    {
        var rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
         var comparison = OperatingSystem.IsWindows()
             ? StringComparison.OrdinalIgnoreCase
             : StringComparison.Ordinal;
         return fullPath.StartsWith(rootPath, comparison) ||
             string.Equals(fullPath, Path.GetFullPath(root), comparison);
    }
}
