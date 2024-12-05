using Microsoft.JSInterop;

namespace Bit.Besql;

public sealed class BrowserCacheBesqlStorage(IJSRuntime jsRuntime) : IBesqlStorage
{
    public async Task Init(string filename)
    {
        await jsRuntime.InvokeVoidAsync("BitBesql.init", filename);
    }

    public async Task Persist(string filename)
    {
        await jsRuntime.InvokeVoidAsync("BitBesql.persist", filename);
    }
}
