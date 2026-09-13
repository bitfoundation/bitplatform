//+:cnd:noEmit
namespace Boilerplate.Tests.Features.Caching;

/// <summary>
/// A CDN refuses to cache a response that carries a <c>Set-Cookie</c> - Cloudflare answers
/// <c>cf-cache-status: BYPASS</c> - whatever the cookie is and whatever <c>s-maxage</c> the response also asks for. So
/// the culture cookie <c>App.razor</c> writes on every pre-rendered page used to cost the entire site its edge cache:
/// every page declaring <c>[AppResponseCache]</c> was re-rendered by the origin for every visitor, while its
/// <c>Cache-Control: public, s-maxage=...</c> said otherwise and made the loss invisible from the outside.
/// <para>
/// <c>AppResponseCachePolicy.IsResponseCacheable</c> exempts this one cookie from its own storage check, on the sound
/// argument that an edge entry only ever lives under a culture-prefixed url. That argument persuades ASP.NET Core's
/// output cache, which can be told about it, and nothing else: a CDN applies the blanket rule. Hence the cookie is not
/// written at all when the response is headed for a shared cache, and the client writes it instead (See
/// <c>CultureService.PersistCurrentCulture</c>).
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Caching")]
public partial class CultureCookieCacheabilityTests
{
    /// <summary>An ordinary browser: a crawler or Lighthouse user agent is excluded from the shared caches anyway.</summary>
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AnEdgeCacheablePage_Should_CarryNoSetCookie()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("No culture cookie is ever written when globalization is invariant.");
        }

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

        using var response = await GetPage(visitorHttpClient, "/");

        // Non-vacuity: without this the assertion below would also pass on a page that is simply not cached.
        var decision = string.Concat(response.Headers.GetValues("App-Cache-Response"));
        Assert.DoesNotContain("Edge:-1", decision, "The page under test was not offered to the CDN edge at all.");

        Assert.IsFalse(response.Headers.Contains("Set-Cookie"),
            $"A page offered to the CDN edge ({decision}) carries a Set-Cookie, which makes the CDN bypass its cache: " +
            $"{string.Join(", ", response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [])}");
    }

    /// <summary>
    /// The control, and the reason the rule above is a carve-out rather than "the culture cookie is gone": a response
    /// no shared cache will store has nothing to lose by carrying it, and that is the response a first-time visitor of
    /// a non-cacheable page gets. Losing it there would mean the pre-rendered document stopped persisting the culture
    /// on every platform at once - including Blazor Server, where the client-side write is what covers the gap.
    /// </summary>
    [TestMethod]
    public async Task APageOutsideAnySharedCache_Should_StillWriteTheCultureCookie()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("No culture cookie is ever written when globalization is invariant.");
        }

        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration =>
            {
                configuration["WebAppRender:PrerenderEnabled"] = "true";
                configuration["ResponseCaching:EnableOutputCaching"] = "false";
                configuration["ResponseCaching:EnableCdnEdgeCaching"] = "false";
            }).Start(TestContext.CancellationToken);

        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        using var response = await GetPage(visitorHttpClient, "/");

        Assert.IsTrue(response.Headers.TryGetValues("Set-Cookie", out var cookies)
                      && cookies.Any(c => c.StartsWith(CultureService.CultureCookieName, StringComparison.Ordinal)),
            "A page no shared cache will store must still persist the visitor's culture.");
    }

    private async Task<HttpResponseMessage> GetPage(HttpClient httpClient, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);

        var response = await httpClient.SendAsync(request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        return response;
    }
}
