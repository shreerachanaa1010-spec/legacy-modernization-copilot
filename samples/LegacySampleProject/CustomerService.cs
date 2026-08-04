using System;
using System.Threading.Tasks;
using System.Net;

namespace LegacySampleProject;

public class CustomerService : IDisposable
{
    public void BadMethod()
    {
        var task = Task.FromResult("Hello");

        var result = task.Result;

        task.Wait();

        var client = new WebClient();
    }

    public async Task TestConfigureAwait()
    {
        await Task.Delay(1);

        await Task.Delay(1).ConfigureAwait(false);
    }

    public void Dispose()
    {
        // Intentionally incomplete dispose pattern
    }
}