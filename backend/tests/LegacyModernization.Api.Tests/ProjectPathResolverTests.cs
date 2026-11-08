using LegacyModernization.Api.Services;

namespace LegacyModernization.Api.Tests;

public sealed class ProjectPathResolverTests
{
    [Fact]
    public void DemoMode_AllowsBundledSampleProject()
    {
        var samplesRoot = FindSamplesRoot();
        var projectPath = Path.Combine(samplesRoot, "LegacySampleProject", "LegacySampleProject.csproj");

        Assert.Equal(Path.GetFullPath(projectPath), ProjectPathResolver.Resolve(projectPath, demoMode: true));
    }

    [Fact]
    public void DemoMode_RejectsProjectOutsideSamples()
    {
        var projectPath = Path.Combine(Path.GetTempPath(), $"outside-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(projectPath, "<Project />");

        try
        {
            Assert.Null(ProjectPathResolver.Resolve(projectPath, demoMode: true));
            Assert.Null(ProjectPathResolver.ResolveProjectFile(projectPath, demoMode: true));
        }
        finally
        {
            File.Delete(projectPath);
        }
    }

    [Fact]
    public void DemoMode_RejectsOtherBundledProjects()
    {
        var samplesRoot = FindSamplesRoot();
        var projectPath = Path.Combine(samplesRoot, "WebAPIContrib", "src", "WebApiContrib", "WebApiContrib.csproj");

        Assert.Null(ProjectPathResolver.Resolve(projectPath, demoMode: true));
        Assert.Null(ProjectPathResolver.ResolveProjectFile(projectPath, demoMode: true));
    }

    private static string FindSamplesRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var samplesRoot = Path.Combine(directory.FullName, "samples");
            if (Directory.Exists(samplesRoot))
            {
                return samplesRoot;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository samples directory.");
    }
}