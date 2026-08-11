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

    /// <summary>
    /// What the Lighthouse CLI and Chrome DevTools' audit panel send: an ordinary Chrome user agent with
    /// <c>Chrome-Lighthouse</c> appended, and deliberately no search-engine substring in it. That is what makes it
    /// able to pin the benchmark half - a user agent that matches <c>IsCrawlerClient()</c> as well would leave every
    /// assertion below satisfied by the crawler branch alone.
    /// </summary>
    private const string LighthouseUserAgent =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Chrome-Lighthouse";

    /// <summary>
    /// PageSpeed Insights, which is both: it carries <c>Chrome-Lighthouse</c> AND <c>Google Page Speed Insights</c>, so
    /// it satisfies both predicates. Kept as its own constant so the two cases stay distinguishable.
    /// </summary>
    private const string PageSpeedInsightsUserAgent =
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko; Google Page Speed Insights) Chrome/75.0.3777.100 Safari/537.36 Chrome-Lighthouse";

    /// <summary>An ordinary browser - the control for every assertion below.</summary>
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private const string BlazorBootScript = "_framework/bit.blazor.web.es2019.js";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The user agents above are only useful if each one matches the predicate the test using it means. This is not
    /// hypothetical: the constant that used to stand for "a Lighthouse run" was PageSpeed Insights' one, which contains
    /// "Google" and therefore also satisfies <c>IsCrawlerClient()</c> - so every assertion about the benchmark carve-out
    /// was in fact being satisfied by the crawler branch, and deleting <c>IsLightHouseRequest()</c> from both of its
    /// call sites would have left this file green.
    /// </summary>
    [TestMethod]
    public void TheUserAgents_Should_MatchOnlyThePredicateTheyStandFor()
    {
        Assert.IsTrue(RequestWith(LighthouseUserAgent).IsLightHouseRequest());
        Assert.IsFalse(RequestWith(LighthouseUserAgent).IsCrawlerClient(),
            "The Lighthouse constant must NOT be a crawler, or it cannot pin anything IsLightHouseRequest() decides on its own.");

        Assert.IsTrue(RequestWith(GooglebotUserAgent).IsCrawlerClient());
        Assert.IsFalse(RequestWith(GooglebotUserAgent).IsLightHouseRequest());

        Assert.IsTrue(RequestWith(PageSpeedInsightsUserAgent).IsCrawlerClient());
        Assert.IsTrue(RequestWith(PageSpeedInsightsUserAgent).IsLightHouseRequest());

        Assert.IsFalse(RequestWith(BrowserUserAgent).IsCrawlerClient());
        Assert.IsFalse(RequestWith(BrowserUserAgent).IsLightHouseRequest());
    }

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

        Assert.DoesNotContain(BlazorBootScript, await GetPage(visitorHttpClient, "/", LighthouseUserAgent),
            "With pre-rendering on there IS html to measure, so a Lighthouse run should get the script-free document.");

        Assert.DoesNotContain(BlazorBootScript, await GetPage(visitorHttpClient, "/", GooglebotUserAgent),
            "A crawler should be served the pre-rendered html without the Blazor bundle.");

        Assert.Contains(BlazorBootScript, await GetPage(visitorHttpClient, "/", BrowserUserAgent),
            "An ordinary browser must always receive the Blazor bootstrap.");
    }

    /// <summary>
    /// Streaming pre-rendering delivers everything the first render pass could not finish in later
    /// <c>&lt;blazor-ssr&gt;</c> blocks, which are inert markup until the Blazor script applies them. So a caller whose
    /// scripts were removed must not be streamed to, or the half of the page the pre-render existed to produce never
    /// arrives - and for a benchmark run that is precisely the content being measured.
    /// <para>
    /// The ordinary browser is the control and it is load-bearing twice: it establishes that streaming is on at all for
    /// this configuration (otherwise the assertion below would pass because nothing streams anywhere), and
    /// <c>&lt;blazor-ssr&gt;</c> is the only observable evidence that content was deferred.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task StreamingPrerendering_Should_BeSuppressed_ForABenchmarkRun()
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true")
            .Start(TestContext.CancellationToken);

        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        Assert.Contains("<blazor-ssr", await GetPage(visitorHttpClient, "/", BrowserUserAgent),
            "An ordinary visitor is streamed to, which is what makes the assertion below mean something.");

        var lighthouseHtml = await GetPage(visitorHttpClient, "/", LighthouseUserAgent);

        Assert.DoesNotContain(BlazorBootScript, lighthouseHtml, "A benchmark run is served the script-less document.");
        Assert.DoesNotContain("<blazor-ssr", lighthouseHtml,
            "A benchmark run has no script to apply a streamed update with, so it must not be streamed to.");
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
            Assert.Contains(BlazorBootScript, await GetPage(visitorHttpClient, "/", userAgent),
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

        var html = await GetPage(visitorHttpClient, "/", GooglebotUserAgent);

        // The positive half first: removing the scripts is only defensible because the page itself is in the response.
        // Asserting only the two absences below would stay green if a BlazorSsr deployment started serving a blank 200.
        var homeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), CultureInfo.InvariantCulture)!;
        Assert.Contains(homeMessage, html, "A statically rendered page must actually carry the page.");

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
    /// <para>
    /// Only <c>Output</c> is asserted, deliberately: a pre-rendered page has its edge and client caching switched off
    /// for every caller anyway (<c>IsBlazorPageContext()</c> plus non-invariant globalization), so an
    /// <c>Edge:-1</c> assertion here would hold no matter what the carve-out did. The edge half of the carve-out is
    /// pinned on a non-page endpoint instead, in <see cref="ACrawlersSiteMap_Should_StillBeShareable"/>.
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
            var (html, appCacheResponse) = await GetPageWithCacheDecision(visitorHttpClient, "/", userAgent);

            // The two halves of the same decision, asserted together on the same response.
            Assert.DoesNotContain(BlazorBootScript, html, $"'{userAgent}' is expected to be served a script-less document.");
            Assert.Contains("Output:-1", appCacheResponse, $"The script-less response for '{userAgent}' was stored in the output cache.");
        }

        // The control: an ordinary browser still gets the scripts AND is still cached, so the exclusion above is
        // scoped rather than "caching is off for this page".
        var (browserHtml, browserCacheResponse) = await GetPageWithCacheDecision(visitorHttpClient, "/", BrowserUserAgent);

        Assert.Contains(BlazorBootScript, browserHtml);
        Assert.DoesNotContain("Output:-1", browserCacheResponse, "An ordinary visitor's page should still be cached.");
    }

    /// <summary>
    /// The carve-out above must stay scoped to the host page. <c>/sitemap.xml</c> is not a page context: it produces the
    /// same bytes for every user agent, crawlers are the only clients that ever request it, and it declares a seven day
    /// <c>SharedMaxAge</c> - so excluding crawlers from caching it would mean the one caller the cache exists for is the
    /// one caller never served from it, and every hit would re-run the reflection scan behind it.
    /// </summary>
    [TestMethod]
    public async Task ACrawlersSiteMap_Should_StillBeShareable()
    {
        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration =>
            {
                configuration["ResponseCaching:EnableOutputCaching"] = "true";
                configuration["ResponseCaching:EnableCdnEdgeCaching"] = "true";
            }).Start(TestContext.CancellationToken);

        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        var (_, siteMapDecision) = await GetPageWithCacheDecision(visitorHttpClient, "/sitemap.xml", GooglebotUserAgent);

        Assert.DoesNotContain("Output:-1", siteMapDecision, "A crawler's sitemap must be storable, or its SharedMaxAge means nothing.");
        Assert.DoesNotContain("Edge:-1", siteMapDecision, "A crawler's sitemap must be offerable to the CDN, or its SharedMaxAge means nothing.");

        // The control that keeps this from reading as "the carve-out was deleted": the same crawler asking for the host
        // page - the one response that really is user-agent specific - is still excluded from both shared caches.
        var (_, pageDecision) = await GetPageWithCacheDecision(visitorHttpClient, "/", GooglebotUserAgent);

        Assert.Contains("Output:-1", pageDecision, "The host page's crawler carve-out must still apply.");
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

    private static HttpRequest RequestWith(string userAgent)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = userAgent;
        return httpContext.Request;
    }

    /// <summary>
    /// <c>App-Cache-Response</c> is what <c>AppResponseCachePolicy</c> reports its per-request decision through;
    /// <c>-1</c> for a layer means that layer was switched off for this request.
    /// </summary>
    private async Task<(string Html, string AppCacheResponse)> GetPageWithCacheDecision(HttpClient httpClient, string path, string userAgent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        Assert.IsTrue(response.Headers.TryGetValues("App-Cache-Response", out var decision),
            $"'{path}' reported no caching decision at all, so nothing about its caching can be asserted.");

        return (await response.Content.ReadAsStringAsync(TestContext.CancellationToken), string.Concat(decision!));
    }

    private async Task<string> GetPage(HttpClient httpClient, string path, string userAgent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
    }
}
