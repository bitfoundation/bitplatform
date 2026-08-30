//+:cnd:noEmit
using Microsoft.AspNetCore.Localization;

namespace Boilerplate.Tests.Features.Culture;

/// <summary>
/// Pins <c>UseCultureUrlRedirection</c>: on a multilingual build, <c>/{culture}/...</c> is the ONE url a Blazor page
/// is served under. Every other form of a page url - culture-less, <c>?culture=</c>, a non-canonical casing - is
/// 302ed onto it, and the redirect's target is where the user's preference is honored: the culture cookie first,
/// then <c>Accept-Language</c>, then the default culture. That canonicalization is what makes pre-rendered pages
/// edge and client cacheable at all (See <c>AppResponseCachePolicy</c> and
/// <c>ProductResponseCacheTests.EdgeCaching_Should_BeEnabled_ForCulturePrefixedPrerenderedPages</c>): the url alone
/// identifies the language variant, so a cache that only keys on urls can never hand one caller another caller's
/// language.
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Localization")]
public partial class CultureUrlRedirectionTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task ACultureLessPageUrl_Should_RedirectToTheDefaultCulture_WhenTheCallerStatesNoPreference()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var response = await httpClient.GetAsync(PageUrls.Terms, TestContext.CancellationToken);

        AssertRedirectsTo($"/{CultureInfoManager.DefaultCulture.Name}{PageUrls.Terms}", response);

        // The site root as well - the most common entry url of all.
        using var rootResponse = await httpClient.GetAsync("/", TestContext.CancellationToken);

        AssertRedirectsTo($"/{CultureInfoManager.DefaultCulture.Name}/", rootResponse);
    }

    /// <summary>
    /// The culture cookie is the user's own (last) choice, written by <c>CultureService.ChangeCulture</c> and by every
    /// pre-rendered page - and it must outrank <c>Accept-Language</c>, or picking a language in the app menu would
    /// only last until the next culture-less url, when the browser's language would take over again. This is the
    /// request where "keeping the culture in the url" and "respecting the user's preference" meet: the url states no
    /// culture, so the preference decides which culture url the caller is sent to.
    /// </summary>
    [TestMethod]
    public async Task ACultureLessPageUrl_Should_RedirectToTheCookiePreferredCulture_OverAcceptLanguage()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var request = new HttpRequestMessage(HttpMethod.Get, PageUrls.Terms);
        request.Headers.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}=c%3Dfa-IR%7Cuic%3Dfa-IR");
        request.Headers.AcceptLanguage.Add(new("sv-SE"));

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);

        AssertRedirectsTo($"/fa-IR{PageUrls.Terms}", response);
    }

    /// <summary>
    /// With no cookie the browser's language decides - including a browser that advertises only the NEUTRAL culture
    /// "fa", which <c>AppAcceptLanguageRequestCultureProvider</c> maps up to the supported specific "fa-IR".
    /// </summary>
    [TestMethod]
    public async Task ACultureLessPageUrl_Should_RedirectToTheAcceptLanguageCulture_WhenThereIsNoCookie()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var request = new HttpRequestMessage(HttpMethod.Get, PageUrls.Terms);
        request.Headers.AcceptLanguage.Add(new("fa"));

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);

        AssertRedirectsTo($"/fa-IR{PageUrls.Terms}", response);
    }

    /// <summary>
    /// <c>?culture=</c> is a supported way of addressing a culture (See <c>UriExtensions.GetCulture</c>), but it may
    /// not stay the url a page is served under: <c>QueryKeys = "*"</c> would give it a cache entry apart from the
    /// path-prefixed form of the very same document. It is folded onto the canonical url instead - and it outranks the
    /// path segment, exactly like <c>QueryStringRequestCultureProvider</c> runs before
    /// <c>RouteDataRequestCultureProvider</c>.
    /// </summary>
    [TestMethod]
    public async Task ACultureQueryString_Should_BeFoldedIntoTheCulturePrefixedPath()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var response = await httpClient.GetAsync($"{PageUrls.Terms}?culture=fa-IR&utm_source=test", TestContext.CancellationToken);

        AssertRedirectsTo($"/fa-IR{PageUrls.Terms}?utm_source=test", response);

        using var overridingResponse = await httpClient.GetAsync($"/sv-SE{PageUrls.Terms}?culture=fa-IR", TestContext.CancellationToken);

        AssertRedirectsTo($"/fa-IR{PageUrls.Terms}", overridingResponse);
    }

    /// <summary>
    /// Culture names in urls resolve case-insensitively, but every casing a page is served under is a cache entry of
    /// its own on the edge - so the casings are folded onto the canonical one rather than each served in place.
    /// </summary>
    [TestMethod]
    public async Task ANonCanonicallyCasedCultureUrl_Should_RedirectToTheCanonicalCasing()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var response = await httpClient.GetAsync($"/FA-ir{PageUrls.Terms}", TestContext.CancellationToken);

        AssertRedirectsTo($"/fa-IR{PageUrls.Terms}", response);
    }

    /// <summary>
    /// The canonical url is served in place, whatever the caller's cookie or browser language says: a shared
    /// <c>/fa-IR/...</c> link must render Persian for everyone, and a cacheable url may not secretly vary on who asks.
    /// </summary>
    [TestMethod]
    public async Task ACulturePrefixedPageUrl_Should_BeServedInPlace()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/fa-IR{PageUrls.Terms}");
        request.Headers.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}=c%3Dsv-SE%7Cuic%3Dsv-SE");
        request.Headers.AcceptLanguage.Add(new("de-DE"));

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "A canonical culture-prefixed page url must be served in place; redirecting it again would loop.");
    }

    /// <summary>
    /// The redirect's target depends on the caller's own cookie and <c>Accept-Language</c>, so the redirect itself is
    /// the one page response that must never be stored anywhere shared: a CDN replaying the first caller's 302 would
    /// pin that caller's language onto the bare url for everyone.
    /// </summary>
    [TestMethod]
    public async Task TheRedirect_Should_NotBeCacheable()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var response = await httpClient.GetAsync(PageUrls.Terms, TestContext.CancellationToken);

        var hasNoStore = response.Headers.CacheControl?.NoStore is true;

        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.IsTrue(hasNoStore,
            "The redirect must carry `no-store`, or a shared cache could replay one caller's language choice to everyone.");
        Assert.IsFalse(response.Headers.Contains("App-Cache-Response"),
            "The redirect short-circuits before UseOutputCache, so AppResponseCachePolicy must never have run for it.");
    }

    /// <summary>
    /// Only Blazor page urls are canonicalized. An API or document endpoint resolves its culture from the request
    /// (cookie / Accept-Language) like it always did - redirecting those would break every non-browser client.
    /// </summary>
    [TestMethod]
    public async Task NonPageEndpoints_Should_NotBeRedirected()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/sitemap.xml");
        request.Headers.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}=c%3Dfa-IR%7Cuic%3Dfa-IR");

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "A non-page endpoint must be served in place, culture cookie or not.");
    }

    /// <summary>
    /// The app shell request the service worker caches (<c>?no-prerender</c>, See bit-bswup) must be served as
    /// requested: a browser refuses a `redirected` response when a service worker answers a navigation with it, so
    /// redirecting the shell request would break offline navigation - and with nothing pre-rendered its response is
    /// not culture-meaningful anyway.
    /// </summary>
    [TestMethod]
    public async Task TheServiceWorkersAppShellRequest_Should_NotBeRedirected()
    {
        await using var server = await StartServer();
        using var httpClient = CreateRedirectInspectingHttpClient(server);

        using var response = await httpClient.GetAsync("/?no-prerender=true", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The service worker's app shell request must be served in place.");
    }

    private async Task<AppTestServer> StartServer()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("Culture url canonicalization does not exist on an invariant globalization build.");
        }

        var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        return server;
    }

    /// <summary>
    /// A `new HttpClient` rather than the DI one, for two stated reasons: the redirect IS the subject, so following it
    /// automatically (every default handler does) would hide the very response under test - and the DI client's
    /// <c>RequestHeadersDelegatingHandler</c> adds an <c>Accept-Language</c> of its own from the test process's
    /// culture, which would shadow the header each test sends deliberately. Cookies are off for the same reason: the
    /// culture cookie every pre-rendered response writes must not leak from one request into the next assertion.
    /// </summary>
    private HttpClient CreateRedirectInspectingHttpClient(AppTestServer server) =>
        new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { BaseAddress = server.WebAppServerAddress };

    private static void AssertRedirectsTo(string expectedLocation, HttpResponseMessage response)
    {
        // 302 rather than 301/308: the target depends on the caller's cookie and Accept-Language, so no client or
        // cache may remember it.
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode,
            $"'{response.RequestMessage?.RequestUri?.PathAndQuery}' should have been redirected onto its canonical culture url.");
        Assert.IsNotNull(response.Headers.Location);
        Assert.AreEqual(expectedLocation, response.Headers.Location.OriginalString);
    }
}
