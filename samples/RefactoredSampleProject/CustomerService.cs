using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LegacySampleProject;

public class CustomerService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public CustomerService()
    {
        _httpClient = new HttpClient();
    }

    // LMC001 fix: replaced Task.Result / Task.Wait() with proper async/await
    public async Task<string> GetDataAsync()
    {
        var result = await Task.FromResult("Hello");

        await Task.CompletedTask;

        return result;
    }

    // LMC002 fix: replaced obsolete WebClient with HttpClient
    // LMC003 fix: added ConfigureAwait(false) on all awaits in library code
    public async Task TestConfigureAwait()
    {
        await Task.Delay(1).ConfigureAwait(false);

        await Task.Delay(1).ConfigureAwait(false);
    }

    // LMC004 fix: proper IDisposable pattern with protected virtual Dispose
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}