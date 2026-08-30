using System.Xml.Linq;

namespace Boilerplate.Tests.Features.Seo;

[TestClass, TestCategory("IntegrationTest")]
public partial class IntegrationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// A sitemap is a statement about which pages are canonical, indexable content, and <c>UseSiteMap</c> derives it by
    /// elimination - every routable page that carries no <c>[Authorize]</c>, has no route parameter, and is not on an
    /// explicit exclusion list. So this test asserts both directions: the public pages are listed, and the pages that
    /// must never be listed are not. The negative half is the one that matters, because a page silently qualifies for
    /// the sitemap the moment someone adds it without <c>[Authorize]</c> - and this document is anonymous and cached
    /// for seven days.
    /// </summary>
    [TestMethod, TestCategory("SEO")]
    public async Task SiteMap_Should_ListPublicPageUrls()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var siteMap = await httpClient.GetStringAsync("sitemap.xml", TestContext.CancellationToken);

        Assert.Contains("<urlset", siteMap);

        if (CultureInfoManager.InvariantGlobalization)
        {
            // Public (anonymous) pages are listed under their bare urls.
            Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, PageUrls.Terms)}</loc>", siteMap);
            Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, PageUrls.PrivacyPolicy)}</loc>", siteMap);
        }
        else
        {
            // On a multilingual build a page is only ever served under its culture-prefixed url - the bare url 302s
            // to it (See UseCultureUrlRedirection) - so public pages are listed once per supported culture and the
            // always-redirecting bare form is not advertised at all.
            Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, $"en-US{PageUrls.Terms}")}</loc>", siteMap);
            Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, $"fa-IR{PageUrls.Terms}")}</loc>", siteMap);
            Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, $"fa-IR{PageUrls.PrivacyPolicy}")}</loc>", siteMap);
            Assert.DoesNotContain($"<loc>{new Uri(server.WebAppServerAddress, PageUrls.Terms)}</loc>", siteMap);
        }

        // Authenticated pages are excluded because their type carries an [Authorize] attribute, which is an unwritten
        // convention until something asserts it: a new admin page whose authorization lives anywhere else - a layout, a
        // policy on AuthorizeRouteView, or a menu that simply does not link to it - would be published here.
        Assert.DoesNotContain(PageUrls.Users, siteMap);
        Assert.DoesNotContain(PageUrls.Roles, siteMap);

    }

    /// <summary>
    /// The sitemap index is built from a <c>List&lt;string&gt;</c> rather than from one xml literal with template
    /// directives inside it, because a directive is line based: the engine would strip it from a generated project, but
    /// in this repo's own tree - where no engine runs - those lines are ordinary string content and would be served as
    /// character data inside <c>&lt;sitemapindex&gt;</c>, which the sitemaps.org schema does not allow. This test is
    /// therefore only ever red in the template's own tree, which is exactly where that mistake can be made.
    /// </summary>
    [TestMethod, TestCategory("SEO")]
    public async Task SiteMapIndex_Should_ContainNoTemplateDirectives()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var siteMapIndex = await httpClient.GetStringAsync("sitemap_index.xml", TestContext.CancellationToken);

        // Non-vacuity first: the document has to actually list the sitemap, or the assertions below prove nothing.
        Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, "sitemap.xml")}</loc>", siteMapIndex);

        //-:cnd:noEmit
        // Conditional processing is off for these two lines. The template engine scans for the text
        // "#if" without regard for C# string literals, and with no condition after it the expression
        // parser throws and aborts the whole `dotnet new` run - so the marker below is what keeps this
        // file, and the generation of every project that carries it, working. Do not remove it.
        Assert.DoesNotContain("#if", siteMapIndex);
        Assert.DoesNotContain("#endif", siteMapIndex);
        //+:cnd:noEmit

        var strayText = XDocument.Parse(siteMapIndex).Root!.Nodes()
            .OfType<XText>()
            .Select(text => text.Value.Trim())
            .Where(text => string.IsNullOrWhiteSpace(text) is false)
            .ToArray();

        Assert.IsEmpty(strayText, $"<sitemapindex> may only contain <sitemap> elements, but it also carries: {string.Join(" | ", strayText)}");
    }

    /// <summary>
    /// With pre-rendering on, the home page's html carries its content before any interactivity starts - which is the
    /// whole point of pre-rendering for a crawler, and the reason this asserts on the response body rather than on a
    /// rendered page. Reading the response is also all a crawler ever does, so there is nothing a browser would add here.
    /// </summary>
    [TestMethod, TestCategory("SEO"), TestCategory("PreRendering")]
    public async Task Prerendering_HomePage_Should_RenderHomeMessage()
    {
        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true"
        ).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var html = await httpClient.GetStringAsync(PageUrls.Home, TestContext.CancellationToken);

        var homeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), CultureInfo.InvariantCulture)!;

        Assert.Contains(homeMessage, html);
    }

    /// <summary>
    /// Enabling output caching makes HttpRequestExtensions.IsStreamPrerenderingSuppressed() return true,
    /// because a streamed response may not be stored in the output/CDN cache. As a result the server fully
    /// pre-renders the page and returns it as a single, complete (non-streamed) response.
    /// </summary>
    [TestMethod, TestCategory("SEO"), TestCategory("PreRendering"), TestCategory("Caching")]
    public async Task Prerendering_WithOutputCaching_Should_ReturnCompleteNonStreamedHomePage()
    {
        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfigurations: configuration =>
            {
                configuration["WebAppRender:PrerenderEnabled"] = "true";
                configuration["ResponseCaching:EnableOutputCaching"] = "true";
            }
        ).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        // Reading the first complete response ourselves is what makes the streaming assertion below meaningful: a client
        // that waits for a streamed response to finish cannot tell whether streaming happened at all.
        using var response = await httpClient.GetAsync(PageUrls.Home, HttpCompletionOption.ResponseHeadersRead, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        var homeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), CultureInfo.InvariantCulture)!;

        // The complete (non-streamed) pre-rendered response already contains the message.
        Assert.Contains(homeMessage, html);

        // Streaming SSR appends its incremental updates inside <blazor-ssr> elements; a suppressed/complete pre-render never does.
        Assert.IsFalse(html.Contains("<blazor-ssr", StringComparison.OrdinalIgnoreCase),
            "Streaming pre-rendering must be suppressed while output caching is enabled.");

        // The App-Cache-Response header proves the shared (output) cache path handled this request,
        // which is exactly the condition that suppresses streaming pre-rendering.
        Assert.IsTrue(response.Headers.TryGetValues("App-Cache-Response", out var appCacheResponse));
        var appCacheResponseValue = string.Concat(appCacheResponse!);
        Assert.Contains("Output:", appCacheResponseValue);
        Assert.DoesNotContain("Output:-1", appCacheResponseValue, "Output caching should be active for this request.");
    }

    [TestMethod, TestCategory("SEO"), TestCategory("PreRendering"), TestCategory("Localization")]
    public async Task Prerendering_FaCulture_HomePage_Should_RenderLocalizedHomeMessage()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("A culture-prefixed url resolves to no culture on an invariant globalization build.");
            return;
        }

        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true"
        ).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        // "fa-IR/" is the exact culture-prefixed home URL advertised in the sitemap.
        var html = await httpClient.GetStringAsync($"{PageUrls.Home}fa-IR/", TestContext.CancellationToken);
        // Decode so the assertion holds whether the non-ASCII (Persian) text is emitted as raw UTF-8 or HTML entities.
        html = System.Net.WebUtility.HtmlDecode(html);

        // Read the expected translation from the resx resources for the fa-IR culture instead of hard-coding it.
        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;
        var faHomeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), faCulture)!;
        var defaultHomeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), CultureInfo.InvariantCulture)!;

        Assert.DoesNotContain(defaultHomeMessage, html);
        Assert.Contains(faHomeMessage, html);

        // The document must SAY it is Persian: without lang a screen reader guesses the voice for this fully
        // pre-rendered page, and without dir the first paint lays the whole page out left-to-right until the Bit
        // components' own dir attributes hydrate (See App.razor's html tag).
        Assert.Contains("lang=\"fa-IR\"", html, "The pre-rendered document must declare the language it is rendered in.");
        Assert.Contains("dir=\"rtl\"", html, "A right-to-left culture's document must declare its directionality.");

        // hreflang: every culture of this page is advertised as a translation of one document, and x-default names
        // the culture-less url whose 302 acts as the language chooser (See App.razor's alternate links).
        Assert.Contains($"hreflang=\"en-US\" href=\"{new Uri(server.WebAppServerAddress, "en-US/")}\"", html,
            "Each supported culture's url must be advertised as an alternate of this page.");
        Assert.Contains($"hreflang=\"x-default\" href=\"{server.WebAppServerAddress}\"", html,
            "The culture-less (redirecting) url must be advertised as the x-default alternate.");
    }

    /// <summary>
    /// A browser can advertise the <b>neutral</b> culture "fa" (rather than the specific "fa-IR") in its Accept-Language
    /// header. AppAcceptLanguageRequestCultureProvider maps that neutral name up to the supported specific culture
    /// "fa-IR", so requesting the culture-less home page must still end in Persian: UseCultureUrlRedirection resolves
    /// the redirect target from that very header, 302s onto /fa-IR/, and the followed redirect renders the Persian page.
    /// <para>
    /// <b>Why a bare <see cref="HttpClient"/> here</b>, unlike the tests above: the header IS the input, and the app's own
    /// <c>RequestHeadersDelegatingHandler</c> adds an Accept-Language of its own (from <c>CurrentUICulture</c>) to every
    /// request, so the DI-resolved client would send the value under test joined with that one.
    /// </para>
    /// </summary>
    [TestMethod, TestCategory("SEO"), TestCategory("PreRendering"), TestCategory("Localization")]
    public async Task Prerendering_FaAcceptLanguageHeader_HomePage_Should_RenderLocalizedHomeMessage()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("Accept-Language is not honoured on an invariant globalization build.");
            return;
        }

        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true"
        ).Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
        // The neutral "fa", never "fa-IR": mapping it up to the supported specific culture is the behavior under test.
        httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("fa"));

        var html = await httpClient.GetStringAsync(PageUrls.Home, TestContext.CancellationToken);
        // Decode so the assertion holds whether the non-ASCII (Persian) text is emitted as raw UTF-8 or HTML entities.
        html = System.Net.WebUtility.HtmlDecode(html);

        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;
        var faHomeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), faCulture)!;

        Assert.Contains(faHomeMessage, html);
    }
}
