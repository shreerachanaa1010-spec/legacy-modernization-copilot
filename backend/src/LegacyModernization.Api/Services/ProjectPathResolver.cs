namespace LegacyModernization.Api.Services;

public static class ProjectPathResolver
{
    public static string? Resolve(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var path = Path.GetFullPath(input.Trim());
        if (File.Exists(path))
        {
            return path;
        }

        if (!Directory.Exists(path))
        {
            return null;
        }

        var projects = Directory.EnumerateFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).ToArray();
        return projects.Length == 1 ? projects[0] : null;
    }
}
