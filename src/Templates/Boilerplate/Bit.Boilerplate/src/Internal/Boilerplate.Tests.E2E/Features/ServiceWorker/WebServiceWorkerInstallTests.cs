using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.ServiceWorker;

/// <summary>
/// One service-worker.published.js serves two hosts, Server.Web and the standalone app, and each serves assets the other
/// does not. The worker picks its asset lists by the <c>?host=server</c> App.razor registers it with; a list that names
/// an asset its host lacks 404s on every install. bswup's lax error tolerance keeps the app working through that, so
/// only its ERROR messages show it.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebServiceWorkerInstallTests : AppTestBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// bswup hands every lifecycle message to window.bitBswupHandler (bit-bswup.progress.js assigns it); this records
    /// each one on its way there, and survives the reload below.
    /// </summary>
    private const string recordBswupMessages = """
        (() => {
            window.__bswupMessages = [];
            let handler;
            Object.defineProperty(window, 'bitBswupHandler', {
                configurable: true,
                set(value) { handler = value; },
                get() {
                    if (typeof handler !== 'function') return undefined;
                    return (message, data) => {
                        window.__bswupMessages.push({ message, url: data?.url, status: data?.status, reason: data?.reason });
                        return handler(message, data);
                    };
                }
            });
        })();
        """;

    [TestMethod]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.TodoAot, DisplayName = nameof(App.TodoAot))]
    [DataRow(App.TodoSmall, DisplayName = nameof(App.TodoSmall))]
    [DataRow(App.TodoOffline, DisplayName = nameof(App.TodoOffline))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = nameof(App.AdminPanelWasmStandalone))]
    public async Task ServiceWorkerInstall_Should_FetchEveryAssetItLists(App app)
    {
        await Page.AddInitScriptAsync(recordBswupMessages);

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        // A first install into an empty cache downloads nothing in passive mode (NoPrerender): it answers 'bypass' and
        // fills the cache as the app fetches. The lists are downloaded by an install that finds the cache already filled
        // - which is what every deployment's update is - so the worker is installed again over the filled cache. Unregistered
        // any sooner, the second install finds the cache as empty as the first did, and proves nothing.
        await page.WaitForFunctionAsync("""
            () => navigator.serviceWorker.controller
                ? caches.keys().then(names => Promise.all(names.map(name => caches.open(name).then(cache => cache.keys()))))
                                .then(buckets => buckets.some(entries => entries.length > 0))
                : false
            """, options: new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds, PollingInterval = 500 });

        await page.EvaluateAsync("() => navigator.serviceWorker.getRegistration().then(r => r?.unregister())");
        await page.ReloadAsync();

        await page.WaitForFunctionAsync("() => window.__bswupMessages.some(m => m.message === 'DOWNLOAD_FINISHED' || m.message === 'ERROR')",
            options: new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds, PollingInterval = 500 });

        // Each failed asset reports on its own, so the rest of the download is let finish first.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errors = await page.EvaluateAsync<string[]>("() => window.__bswupMessages.filter(m => m.message === 'ERROR').map(m => `${m.reason} ${m.status ?? ''} ${m.url ?? ''}`)");

        Assert.IsEmpty(errors,
            $"The service worker lists assets {DeployedApps.AddressOf(app)} does not serve:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }
}
