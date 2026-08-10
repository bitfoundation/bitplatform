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
        // Public (anonymous) pages are listed...
        Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, PageUrls.Terms)}</loc>", siteMap);
        Assert.Contains($"<loc>{new Uri(server.WebAppServerAddress, PageUrls.PrivacyPolicy)}</loc>", siteMap);

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            // ...along with their culture-prefixed SEO variants.
            Assert.Contains($"fa-IR{PageUrls.Terms}", siteMap);
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

        Assert.DoesNotContain("#if", siteMapIndex);
        Assert.DoesNotContain("#endif", siteMapIndex);

        var strayText = XDocument.Parse(siteMapIndex).Root!.Nodes()
            .OfType<XText>()
            .Select(text => text.Value.Trim())
            .Where(text => string.IsNullOrEmpty(text) is false)
            .ToArray();

        Assert.IsEmpty(strayText, $"<sitemapindex> may only contain <sitemap> elements, but it also carries: {string.Join(" | ", strayText)}");
    }
}
