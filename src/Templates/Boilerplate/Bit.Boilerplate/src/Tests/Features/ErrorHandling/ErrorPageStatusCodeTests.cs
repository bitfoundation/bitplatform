namespace Boilerplate.Tests.Features.ErrorHandling;

/// <summary>
/// Server.Web's Handle40XStatusCodes redirects Blazor's bodiless 401/404 to NotAuthorizedPage / NotFoundPage and gives
/// those pages the status code back. It has to do so without taking their body away: a 404 already on the response
/// when the component endpoint runs makes RazorComponentEndpointInvoker drop the rendered page, and what a visitor
/// without WebAssembly - a crawler, a link preview, a cold first visit - then gets is an empty 404.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class ErrorPageStatusCodeTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task UnknownRoute_Should_Answer404_WithTheNotFoundPagesMarkup()
    {
        await using var server = new AppTestServer();
        // Prerendered, like the deployments: with it off the server renders no page to redirect to or to take a body from.
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true")
            .Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        using var response = await httpClient.GetAsync("/e2e/route/that-does-not-exist", TestContext.CancellationToken);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.EndsWith(PageUrls.NotFound, response.RequestMessage!.RequestUri!.AbsolutePath, "The 404 is served from NotFoundPage's own url.");
        Assert.Contains(AppStrings.NotFoundText, html, "The 404 carries no NotFoundPage markup - a status code with nothing to show.");
    }

    [TestMethod]
    public async Task SignedInOnlyPage_Should_Answer401_WithTheNotAuthorizedPage_ForAnAnonymousRequest()
    {
        await using var server = new AppTestServer();
        // Prerendered, like the deployments: with it off the server renders no page to redirect to or to take a body from.
        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices(),
            configureTestConfigurations: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true")
            .Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        using var response = await httpClient.GetAsync(PageUrls.Settings, TestContext.CancellationToken);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.EndsWith(PageUrls.NotAuthorized, response.RequestMessage!.RequestUri!.AbsolutePath);
        Assert.Contains($"<title>{AppStrings.NotAuthorizedPageTitle}</title>", html, "The 401 carries no NotAuthorizedPage markup.");
    }
}
