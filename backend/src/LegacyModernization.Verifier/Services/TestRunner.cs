using System.Diagnostics;

namespace LegacyModernization.Verifier.Services;

public class TestRunner
{
    public async Task<(bool Passed, string Output)> RunTestsAsync(
        string projectPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"test \"{projectPath}\" --no-restore",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        var combinedOutput = output + Environment.NewLine + error;

        return (
            process.ExitCode == 0,
            combinedOutput
        );
    }
}
