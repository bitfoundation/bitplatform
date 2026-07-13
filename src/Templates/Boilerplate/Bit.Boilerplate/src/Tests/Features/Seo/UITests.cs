namespace Boilerplate.Tests.Features.Seo;

[TestClass, TestCategory("UITest")]
public partial class UITests : AppPageTest
{
    [TestMethod, TestCategory("PreRendering")]
    public async Task Streaming_Prerender_Enabled_HomePage_Should_RenderHomeMessage()
    {
        await using var server = new AppTestServer(Context);

        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true"
        ).Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        await Page.GotoAsync(serverAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var homeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), CultureInfo.InvariantCulture)!;

        await Expect(Page.GetByText(homeMessage)).ToBeVisibleAsync();
    }

    [TestMethod, TestCategory("SEO"), TestCategory("PreRendering"), TestCategory("Caching")]
    public async Task Prerendering_WithOutputCaching_Should_ReturnCompleteNonStreamedHomePage()
    {
        await using var server = new AppTestServer(Context);

        // Enabling output caching makes HttpRequestExtensions.IsStreamPrerenderingSuppressed() return true,
        // because a streamed response may not be stored in the output/CDN cache. As a result the server fully
        // pre-renders the page and returns it as a single, complete (non-streamed) response.
        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfigurations: configuration =>
            {
                configuration["WebAppRender:PrerenderEnabled"] = "true";
                configuration["ResponseCaching:EnableOutputCaching"] = "true";
            }
        ).Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        // We deliberately use a raw HttpClient instead of Playwright here: Playwright would transparently wait
        // for a streamed response to finish, hiding whether streaming happened. By reading the first complete
        // response ourselves, we can assert the pre-rendered content is already present without any streaming.
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
        await using var server = new AppTestServer(Context);

        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true"
        ).Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        // "fa-IR/" is the exact culture-prefixed home URL advertised in the sitemap. Using an HttpClient is a
        // little faster than Playwright and enough to validate the pre-rendered, localized content.
        var html = await httpClient.GetStringAsync($"{PageUrls.Home}fa-IR/", TestContext.CancellationToken);
        // Decode so the assertion holds whether the non-ASCII (Persian) text is emitted as raw UTF-8 or HTML entities.
        html = System.Net.WebUtility.HtmlDecode(html);

        // Read the expected translation from the resx resources for the fa-IR culture instead of hard-coding it.
        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;
        var faHomeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), faCulture)!;
        var defaultHomeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), CultureInfo.InvariantCulture)!;

        Assert.DoesNotContain(defaultHomeMessage, html);
        Assert.Contains(faHomeMessage, html);
    }

    [TestMethod, TestCategory("SEO"), TestCategory("PreRendering"), TestCategory("Localization")]
    public async Task Prerendering_FaAcceptLanguageHeader_HomePage_Should_RenderLocalizedHomeMessage()
    {
        // A browser can advertise the *neutral* culture "fa" (rather than the specific "fa-IR") in its Accept-Language
        // header. AppAcceptLanguageRequestCultureProvider maps that neutral name up to the supported specific culture
        // "fa-IR", so the (culture-less) home page must still be served in Persian. Unlike the test above, this drives a
        // real browser to prove the neutral -> specific mapping happens purely from the Accept-Language header, with no
        // culture in the URL.
        var contextOptions = ContextOptions();
        contextOptions.Locale = "fa"; // Playwright turns Locale into the Accept-Language request header (here just "fa", not "fa-IR").
        await using var faContext = await Browser.NewContextAsync(contextOptions);

        await using var server = new AppTestServer(faContext);

        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true"
        ).Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        var faPage = await faContext.NewPageAsync();
        faPage.SetDefaultTimeout((float)TimeSpan.FromMinutes(1).TotalMilliseconds);

        // Navigate to the root (culture-less) URL; the culture is resolved solely from the "fa" Accept-Language header.
        await faPage.GotoAsync(serverAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Read the expected translation from the resx resources for the fa-IR culture instead of hard-coding it.
        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;
        var faHomeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), faCulture)!;

        await Expect(faPage.GetByText(faHomeMessage)).ToBeVisibleAsync();
    }
}
