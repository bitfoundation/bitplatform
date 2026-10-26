using System.Collections.Concurrent;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// The host this run drives (<see cref="E2EEnvironment.Host"/>) and the way a test gets a page on it.
/// </summary>
/// <remarks>
/// Every fixture in the suite goes through here, which is what lets the same tests run against every host: the
/// standalone WebAssembly sample, each render mode of the Blazor Web App harness, and the BlazorWebView harness.
/// Hosts start on first use and are shared by the whole run; lazy script loading is a property of a host's
/// process (it is a process-wide toggle in the library), so a web render mode runs a second process for it.
/// </remarks>
public static class HarnessTarget
{
    private static readonly ConcurrentDictionary<bool, Lazy<Task<WebHarnessHost>>> _webHosts = new();
    private static readonly Lazy<Task<SampleAppHost>> _standalone = new(() => SampleAppHost.Start(E2EEnvironment.ExternalBaseUrl));

    /// <summary>Starts the bundle-mode host up front, so a host that cannot start fails the run in one place.</summary>
    public static async Task StartAsync()
    {
        var host = E2EEnvironment.Host;

        if (host is HarnessHostKinds.Standalone) await _standalone.Value;
        else if (HarnessHostKinds.IsWebApp(host)) await WebHostAsync(lazyScripts: false);
        // The hybrid windows start as tests ask for them, up to the pool size - one failed start is remembered there.
    }

    public static async Task<IHarnessLease> AcquireAsync(bool lazyScripts)
    {
        var host = E2EEnvironment.Host;

        if (host is HarnessHostKinds.Hybrid) return await HybridHarnessLease.OpenAsync(lazyScripts);

        // The standalone sample picks its script-loading mode per page load, from ?lazy=1 on the start URL.
        var baseUrl = host is HarnessHostKinds.Standalone
            ? (await _standalone.Value).BaseUrl
            : (await WebHostAsync(lazyScripts)).BaseUrl;

        return await WebHarnessLease.OpenAsync(baseUrl);
    }

    public static async Task StopAllAsync()
    {
        await HybridHostPool.StopAllAsync();

        foreach (var host in _webHosts.Values.Where(h => h.IsValueCreated))
        {
            try { await (await host.Value).DisposeAsync(); }
            catch (Exception) { /* a host that failed to start already failed the tests that needed it */ }
        }
        _webHosts.Clear();

        if (_standalone.IsValueCreated)
        {
            try { await (await _standalone.Value).DisposeAsync(); }
            catch (Exception) { /* same */ }
        }
    }

    private static Task<WebHarnessHost> WebHostAsync(bool lazyScripts) =>
        _webHosts.GetOrAdd(lazyScripts, lazy => new Lazy<Task<WebHarnessHost>>(() => WebHarnessHost.StartAsync(E2EEnvironment.Host, lazy))).Value;
}
