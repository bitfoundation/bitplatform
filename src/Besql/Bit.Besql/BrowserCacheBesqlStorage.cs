using Microsoft.JSInterop;

namespace Bit.Besql;

public sealed class BrowserCacheBesqlStorage : IAsyncDisposable, IBesqlStorage
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public BrowserCacheBesqlStorage(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Bit.Besql/browserCache.js").AsTask()!);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }

    public async Task<int> SyncDb(string filename)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<int>("synchronizeDbWithCache", filename);
    }
}
