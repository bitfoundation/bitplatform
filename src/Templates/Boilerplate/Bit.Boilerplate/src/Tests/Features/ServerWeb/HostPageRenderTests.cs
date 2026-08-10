//+:cnd:noEmit
namespace Boilerplate.Tests.Features.ServerWeb;

/// <summary>
/// Pins what the Blazor host document (<c>Server.Web/Components/App.razor</c>) emits, for the two inputs any anonymous
/// caller controls: the <c>User-Agent</c> header and the <c>?no-prerender</c> query flag.
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("PreRendering")]
public partial class HostPageRenderTests
{
    /// <summary>Googlebot's documented crawler user agent, verbatim.</summary>
    private const string GooglebotUserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

    /// <summary>What PageSpeed Insights and every Lighthouse runner carry.</summary>
    private const string LighthouseUserAgent =
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko; Google Page Speed Insights) Chrome/75.0.3777.100 Safari/537.36 Chrome-Lighthouse";

    /// <summary>An ordinary browser - the control for every assertion below.</summary>
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private const string BlazorBootScript = "_framework/bit.blazor.web.es2019.js";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// A benchmark run and a crawler are both served the pre-rendered html without the Blazor bundle: the scripts would
    /// only cost the benchmark its score and give the crawler a payload it does not need. An ordinary browser is the
    /// control - it must always get them, so "drop the scripts for everyone" cannot pass.
    /// </summary>
    [TestMethod]
    public async Task HostPage_Should_DropTheScripts_ForBenchmarksAndCrawlers_WhenThereIsPrerenderedContent()
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true")
            .Start(TestContext.CancellationToken);

        // A bare HttpClient so the assertions are about what the server wrote, with no client side handlers in between.
        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        Assert.DoesNotContain(BlazorBootScript, await GetHomePage(visitorHttpClient, LighthouseUserAgent),
            "With pre-rendering on there IS html to measure, so a Lighthouse run should get the script-free document.");

        Assert.DoesNotContain(BlazorBootScript, await GetHomePage(visitorHttpClient, GooglebotUserAgent),
            "A crawler should be served the pre-rendered html without the Blazor bundle.");

        Assert.Contains(BlazorBootScript, await GetHomePage(visitorHttpClient, BrowserUserAgent),
            "An ordinary browser must always receive the Blazor bootstrap.");
    }

    /// <summary>
    /// At the shipped default (<c>PrerenderEnabled: false</c>) nothing is rendered server side, so the scripts are the
    /// only thing that could ever produce content - they must be present for every caller, benchmark and crawler
    /// included. This is the half that made a Googlebot fetch of a default-configured deployment come back blank.
    /// </summary>
    [TestMethod]
    public async Task HostPage_Should_AlwaysEmitTheScripts_WhenNothingIsPrerendered()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics())
                    .Start(TestContext.CancellationToken);

        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        foreach (var userAgent in new[] { GooglebotUserAgent, LighthouseUserAgent, BrowserUserAgent })
        {
            Assert.Contains(BlazorBootScript, await GetHomePage(visitorHttpClient, userAgent),
                $"'{userAgent}' received a document with no content and no way to fetch any.");
        }
    }

    /// <summary>
    /// Under <c>BlazorSsr</c> the whole component tree is rendered on the server, so a crawler gets the real page and
    /// needs no scripts at all - which is the one configuration where dropping them costs nothing.
    /// <para>
    /// This is the case the <c>noPrerenderedContent</c> predicate exists to keep apart: it keys on the effective
    /// <c>renderMode</c>, not on <c>PrerenderEnabled</c> alone, because <c>BlazorSsr</c> leaves <c>PrerenderEnabled</c>
    /// at its shipped <c>false</c> while still producing html. Reading the setting alone would treat this response as
    /// empty and hand the crawler the scripts (plus a loading shell over a fully rendered page).
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task UnderBlazorSsr_ACrawler_Should_GetTheRenderedPageWithoutScripts()
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:BlazorMode"] = "BlazorSsr")
            .Start(TestContext.CancellationToken);

        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        var html = await GetHomePage(visitorHttpClient, GooglebotUserAgent);

        Assert.DoesNotContain(BlazorBootScript, html, "There is nothing for the scripts to do on a statically rendered page.");
        Assert.DoesNotContain("bit-lds-wrapper", html, "A statically rendered page has its content already; it needs no loading shell.");
    }

    /// <summary>
    /// A response whose scripts were dropped must never be stored in a cache that is shared with ordinary visitors.
    /// <para>
    /// Nothing in the cache key varies by <c>User-Agent</c> (See <c>AppResponseCachePolicy.CacheRequestAsync</c>), so
    /// if a benchmark's or a crawler's script-less document were stored, the very next ordinary visitor would be
    /// served a shell that can never boot - and would keep being served it for the whole <c>SharedMaxAge</c>. That
    /// makes this pair - script omission and cache exclusion - a single decision, and the reason the two must never be
    /// changed independently.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task AScriptLessResponse_Should_NeverBeStoredInASharedCache()
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration =>
            {
                configuration["WebAppRender:PrerenderEnabled"] = "true";
                configuration["ResponseCaching:EnableOutputCaching"] = "true";
                configuration["ResponseCaching:EnableCdnEdgeCaching"] = "true";
            }).Start(TestContext.CancellationToken);

        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        foreach (var userAgent in new[] { LighthouseUserAgent, GooglebotUserAgent })
        {
            var (html, appCacheResponse) = await GetHomePageWithCacheDecision(visitorHttpClient, userAgent);

            // The two halves of the same decision, asserted together on the same response.
            Assert.DoesNotContain(BlazorBootScript, html, $"'{userAgent}' is expected to be served a script-less document.");
            Assert.Contains("Output:-1", appCacheResponse, $"The script-less response for '{userAgent}' was stored in the output cache.");
            Assert.Contains("Edge:-1", appCacheResponse, $"The script-less response for '{userAgent}' was offered to the CDN.");
        }

        // The control: an ordinary browser still gets the scripts AND is still cached, so the exclusion above is
        // scoped rather than "caching is off for this page".
        var (browserHtml, browserCacheResponse) = await GetHomePageWithCacheDecision(visitorHttpClient, BrowserUserAgent);

        Assert.Contains(BlazorBootScript, browserHtml);
        Assert.DoesNotContain("Output:-1", browserCacheResponse, "An ordinary visitor's page should still be cached.");
    }

    /// <summary>
    /// <c>App-Cache-Response</c> is what <c>AppResponseCachePolicy</c> reports its per-request decision through;
    /// <c>-1</c> for a layer means that layer was switched off for this request.
    /// </summary>
    private async Task<(string Html, string AppCacheResponse)> GetHomePageWithCacheDecision(HttpClient httpClient, string userAgent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        Assert.IsTrue(response.Headers.TryGetValues("App-Cache-Response", out var decision));

        return (await response.Content.ReadAsStringAsync(TestContext.CancellationToken), string.Concat(decision!));
    }

    /// <summary>
    /// <c>?no-prerender</c> means "skip pre-rendering", not "run this app in a different Blazor mode". The shipped
    /// service worker requests the app shell with that flag on every published deployment, so a mode override there
    /// would silently switch a Blazor Server deployment's PWA navigations onto the WebAssembly runtime.
    /// </summary>
    [TestMethod]
    public async Task NoPrerenderFlag_Should_KeepTheConfiguredBlazorMode()
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:BlazorMode"] = "BlazorServer")
            .Start(TestContext.CancellationToken);

        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        var html = await visitorHttpClient.GetStringAsync("/?no-prerender=true", TestContext.CancellationToken);

        // Blazor writes its render mode into the <!--Blazor:{"type":"server"|"webassembly",...}--> boot markers.
        Assert.Contains("\"type\":\"server\"", html,
            "?no-prerender replaced the configured BlazorServer mode with WebAssembly.");
        Assert.DoesNotContain("\"type\":\"webassembly\"", html);
    }

    private async Task<string> GetHomePage(HttpClient httpClient, string userAgent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
    }
}
