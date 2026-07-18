namespace Boilerplate.Tests.Features.Seo;

[TestClass, TestCategory("IntegrationTest")]
public partial class IntegrationTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod, TestCategory("SEO")]
    public async Task SiteMap_Should_ListPublicPageUrls()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var siteMap = await httpClient.GetStringAsync("sitemap.xml", TestContext.CancellationToken);

        Assert.Contains("<urlset", siteMap);
        // Public (anonymous) pages are listed...
        Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, PageUrls.Terms)}</loc>", siteMap);
        Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, PageUrls.PrivacyPolicy)}</loc>", siteMap);

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            // ...along with their culture-prefixed SEO variants.
            Assert.Contains($"fa-IR{PageUrls.Terms}", siteMap);
        }
    }
}
