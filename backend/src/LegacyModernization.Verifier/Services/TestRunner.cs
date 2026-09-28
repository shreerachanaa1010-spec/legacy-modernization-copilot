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

        var timeout = GetTimeout();
        using var timeoutSource = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            await process.WaitForExitAsync();
            var timedOutOutput = await outputTask + Environment.NewLine + await errorTask;
            return (false, $"dotnet test timed out after {timeout.TotalSeconds:0} seconds.{Environment.NewLine}{timedOutOutput}");
        }

        var output = await outputTask;
        var error = await errorTask;

        var combinedOutput = output + Environment.NewLine + error;

        return (
            process.ExitCode == 0,
            combinedOutput
        );
    }

    private static TimeSpan GetTimeout() =>
        TimeSpan.FromSeconds(
            int.TryParse(Environment.GetEnvironmentVariable("TEST_RUN_TIMEOUT_SECONDS"), out var seconds)
                ? Math.Clamp(seconds, 10, 600)
                : 120);
}
