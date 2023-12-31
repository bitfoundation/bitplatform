using Microsoft.JSInterop;

namespace Bit.Besql;

public sealed class BrowserCacheBesqlStorage(IJSRuntime jsRuntime) : IBesqlStorage
{
    public async Task<int> SyncDb(string filename)
    {
        return await jsRuntime.InvokeAsync<int>("synchronizeDbWithCache", filename);
    }
}
