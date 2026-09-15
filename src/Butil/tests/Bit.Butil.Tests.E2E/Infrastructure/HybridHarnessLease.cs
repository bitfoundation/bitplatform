using Microsoft.Playwright;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// A hybrid app window rented from its pool. The window outlives the test, so what one test leaves behind in
/// the page's storage is cleared before the next one gets it; navigating to the harness route then reloads the
/// page, which gives the test a fresh DI scope and a fresh <c>WebViewJSRuntime</c>.
/// </summary>
public sealed class HybridHarnessLease : IHarnessLease
{
    private readonly HybridHostPool _pool;
    private readonly HybridHarnessHost _host;

    private HybridHarnessLease(HybridHostPool pool, HybridHarnessHost host)
    {
        _pool = pool;
        _host = host;
    }

    public IPage Page => _host.Page;

    public string BaseUrl => HybridHarnessHost.AppOrigin;

    public static async Task<HybridHarnessLease> OpenAsync(bool lazyScripts)
    {
        var pool = HybridHostPool.For(lazyScripts);
        var host = await pool.RentAsync();

        try
        {
            await host.Page.Context.ClearCookiesAsync();
            await host.Page.EvaluateAsync("() => { try { localStorage.clear(); sessionStorage.clear(); } catch { } }");

            // A test that emulates a device (the screen orientation one does, through DevTools) would otherwise
            // hand every later test in this window a rotated phone-sized screen.
            var cdp = await host.Page.Context.NewCDPSessionAsync(host.Page);
            await cdp.SendAsync("Emulation.clearDeviceMetricsOverride");

            // The user data folder lives as long as the window, so an IndexedDB database, a cache, an OPFS file
            // or a service worker one test created would otherwise be there waiting for the next.
            await cdp.SendAsync("Storage.clearDataForOrigin", new Dictionary<string, object>
            {
                ["origin"] = HybridHarnessHost.AppOrigin,
                ["storageTypes"] = "all"
            });
            await cdp.DetachAsync();
        }
        catch
        {
            pool.Return(host);
            throw;
        }

        return new HybridHarnessLease(pool, host);
    }

    public ValueTask DisposeAsync()
    {
        _pool.Return(_host);
        return ValueTask.CompletedTask;
    }
}
