namespace Boilerplate.Tests.Features.Products;

/// <summary>
/// A product url that names no existing product has to answer <b>404 with the not-found page rendered into it</b>.
/// <para>
/// Both halves are easy to lose and neither shows up in a normal debug run. Non-streaming pre-rendering writes a body
/// only while the status code is still 200 - it re-executes for the exception handler middleware alone
/// (aspnet/aspnetcore#51203) - so a page that reports its 404 by letting an exception reach
/// <c>WebServerExceptionHandler</c> gets its rendered html thrown away and answers an EMPTY 404.
/// <c>ProductPage</c> therefore reports it through <c>NavigationManager.NotFound()</c>, which the framework and
/// Brouter understand: the router's <c>NotFound</c> content renders and the status code survives.
/// </para>
/// <para>
/// The configuration below is the point of the test: <c>EnableOutputCaching</c> is what turns streaming OFF, and a
/// buffered render is the only one whose status code can still be changed. With streaming on, the response has already
/// started and the page answers 200 - so a test without these two settings passes no matter what.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("PreRendering")]
public partial class ProductNotFoundPreRenderTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AMissingProduct_Should_Answer404_WithTheNotFoundPageInTheBody()
    {
        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration =>
            {
                configuration["WebAppRender:PrerenderEnabled"] = "true";
                configuration["ResponseCaching:EnableOutputCaching"] = "true";
            }).Start(TestContext.CancellationToken);

        // Redirects are followed, so this works whether or not the build is culture-prefixed (See UseCultureUrlRedirection).
        using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        using var response = await visitorHttpClient.GetAsync("/product/777777", TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            "A product url that names no product has to answer 404, so crawlers do not index it as a real page.");

        // The artwork NotFoundContent renders. Asserting on the body at all is the point: the status code alone used
        // to be all the caller got.
        Assert.Contains("404.svg", body,
            "The 404 carried no not-found page - the pre-rendered html was discarded, leaving the visitor a blank page.");

        // A faulted response must never be stored, or the 404 outlives the product's creation.
        var isPubliclyCacheable = response.Headers.CacheControl?.Public is true;

        Assert.IsFalse(isPubliclyCacheable,
            "A 404 must not be offered to shared caches.");
    }
}
