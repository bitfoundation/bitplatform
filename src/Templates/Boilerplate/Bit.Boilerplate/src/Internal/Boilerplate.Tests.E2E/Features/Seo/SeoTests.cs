using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Boilerplate.Tests.E2E.Features.Seo;

/// <summary>
/// What Sales publishes for crawlers - Client.Web's wwwroot/robots.txt and Server.Web's UseSiteMap (sitemap_index.xml,
/// sitemap.xml, products.xml, llms.txt) - and what a product page, the one the catalogue sitemap advertises, says about
/// itself in its head.
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class SeoTests
{
    private static readonly XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static readonly Uri sales = new(DeployedApps.Sales);

    private static readonly string[] cultures = [.. CultureInfoManager.SupportedCultures.Select(sc => sc.Culture.Name)];

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task RobotsTxt_Should_LetCrawlersInAndPointThemAtTheSitemapIndex()
    {
        var body = await GetText("robots.txt", "text/plain");

        Assert.Contains("User-agent: *", body);
        Assert.Contains($"Sitemap: {new Uri(sales, "sitemap_index.xml")}", body);
    }

    [TestMethod]
    public async Task EverySitemap_Should_ListOnlyCulturePrefixedUrlsThatAnswer()
    {
        var index = XDocument.Parse(await GetText("sitemap_index.xml", "application/xml"));
        var sitemapUrls = index.Root!.Elements(sitemapNamespace + "sitemap").Select(sitemap => (string)sitemap.Element(sitemapNamespace + "loc")!).ToArray();

        // In UseSiteMap's order: the pages, then (Sales only) the catalogue.
        Assert.AreEqual($"{new Uri(sales, "sitemap.xml")} {new Uri(sales, "products.xml")}", string.Join(' ', sitemapUrls), "sitemap_index.xml's sitemaps.");

        foreach (var sitemapUrl in sitemapUrls)
        {
            var locs = await ReadSitemap(new Uri(sitemapUrl));

            Assert.IsNotEmpty(locs, $"{sitemapUrl} lists no url.");
            Assert.IsLessThanOrEqualTo(50_000, locs.Length, $"{sitemapUrl} is over the sitemaps.org limit of 50,000 urls.");

            foreach (var loc in locs)
            {
                Assert.AreEqual(sales.Host, loc.Host, $"{sitemapUrl} lists {loc}.");
                // The bare url 302s to a culture (See UseCultureUrlRedirection), and an entry that always redirects is one a crawler drops.
                Assert.Contains(loc.Segments[1].TrimEnd('/'), cultures, $"{sitemapUrl} lists {loc}, which names no culture.");
            }

            // Every page once in every culture.
            var perPage = locs.GroupBy(PathWithoutCulture).ToArray();
            Assert.IsTrue(perPage.All(page => page.Count() == cultures.Length),
                $"{sitemapUrl}: {string.Join(", ", perPage.Where(page => page.Count() != cultures.Length).Take(3).Select(page => $"{page.Key} x{page.Count()}"))} - each page should be listed once per culture ({cultures.Length}).");

            // A few of them, as a crawler fetches them: no redirect, no error.
            foreach (var loc in locs.Take(3))
            {
                using var response = await Send(loc);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{loc}, listed in {sitemapUrl}, answered {(int)response.StatusCode} {response.Headers.Location}.");
            }
        }
    }

    [TestMethod]
    public async Task LlmsTxt_Should_ListThePagesWithTheirDescriptions()
    {
        var body = await GetText("llms.txt", "text/plain");

        Assert.IsTrue(body.StartsWith("# ", StringComparison.Ordinal), "llms.txt starts with the app's name as its title (https://llmstxt.org).");
        Assert.Contains("## Pages", body);

        var pages = PageLine().Matches(body);
        Assert.IsGreaterThanOrEqualTo(3, pages.Count, $"llms.txt lists {pages.Count} pages.");

        foreach (Match page in pages)
        {
            Assert.AreEqual(sales.Host, new Uri(page.Groups["url"].Value).Host, $"llms.txt links {page.Groups["url"].Value}.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.Groups["description"].Value), $"{page.Groups["path"].Value} has no description in llms.txt.");
        }
    }

    /// <summary>The first product products.xml advertises, in English: what a search result and a shared link are built from.</summary>
    [TestMethod]
    public async Task AProductPage_Should_DescribeItselfToCrawlers()
    {
        var product = (await ReadSitemap(new Uri(sales, "products.xml"))).First(loc => loc.Segments[1].TrimEnd('/') == "en-US");

        using var response = await Send(product);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{product} answered {(int)response.StatusCode}.");

        Assert.IsFalse(string.IsNullOrWhiteSpace(Tag(html, "<title>", "</title>")), $"{product} has no title.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(MetaContent(html, "name", "description")), $"{product} has no meta description.");
        Assert.AreEqual(product.ToString(), LinkHref(html, "canonical"), $"{product}'s canonical url.");

        // One alternate per culture, and x-default for the url that picks one.
        foreach (var culture in cultures)
        {
            Assert.AreEqual(new Uri(sales, $"{culture}{PathWithoutCulture(product)}").ToString(), AlternateHref(html, culture), $"{product}'s hreflang for {culture}.");
        }
        Assert.AreEqual(new Uri(sales, PathWithoutCulture(product).TrimStart('/')).ToString(), AlternateHref(html, "x-default"), $"{product}'s x-default.");

        // What a shared link is previewed with.
        Assert.AreEqual("product", MetaContent(html, "property", "og:type"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(MetaContent(html, "property", "og:title")), $"{product} has no og:title.");
        Assert.AreEqual(product.ToString(), MetaContent(html, "property", "og:url"));
        Assert.AreEqual(sales.Host, new Uri(MetaContent(html, "property", "og:image")!).Host, "og:image is served by Sales itself.");
        Assert.AreEqual("summary_large_image", MetaContent(html, "name", "twitter:card"));
    }

    private async Task<Uri[]> ReadSitemap(Uri sitemap)
    {
        var document = XDocument.Parse(await GetText(sitemap.PathAndQuery.TrimStart('/'), "application/xml"));

        return [.. document.Root!.Elements(sitemapNamespace + "url").Select(url => new Uri((string)url.Element(sitemapNamespace + "loc")!))];
    }

    private async Task<string> GetText(string path, string mediaType)
    {
        using var response = await Send(new Uri(sales, path));
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{path} answered {(int)response.StatusCode}.");
        Assert.IsNotNull(response.Content.Headers.ContentType, $"{path} answered with no Content-Type.");
        Assert.AreEqual(mediaType, response.Content.Headers.ContentType.MediaType, $"{path}'s Content-Type.");

        return body;
    }

    /// <summary>Redirects are not followed: a crawler sees them, and so should the test.</summary>
    private async Task<HttpResponseMessage> Send(Uri uri)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(2) };

        return await httpClient.GetAsync(uri, TestContext.CancellationToken);
    }

    private static string PathWithoutCulture(Uri uri) => uri.AbsolutePath[(uri.Segments[1].TrimEnd('/').Length + 1)..];

    private static string? Tag(string html, string open, string close)
    {
        var start = html.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        var end = html.IndexOf(close, start, StringComparison.OrdinalIgnoreCase);
        return end < 0 ? null : WebUtility.HtmlDecode(html[(start + open.Length)..end]);
    }

    private static string? MetaContent(string html, string attribute, string name)
    {
        var match = Regex.Match(html, $"""<meta {attribute}="{Regex.Escape(name)}" content="(?<content>[^"]*)""", RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["content"].Value) : null;
    }

    private static string? LinkHref(string html, string rel)
    {
        var match = Regex.Match(html, $"""<link rel="{Regex.Escape(rel)}" href="(?<href>[^"]*)""", RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["href"].Value) : null;
    }

    private static string? AlternateHref(string html, string hreflang)
    {
        var match = Regex.Match(html, $"""<link rel="alternate" hreflang="{Regex.Escape(hreflang)}" href="(?<href>[^"]*)""", RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["href"].Value) : null;
    }

    /// <summary>"- [/terms](https://sales.bitplatform.dev/terms): Legal terms, ..." (See UseSiteMap's llms.txt).</summary>
    [GeneratedRegex(@"^- \[(?<path>[^\]]+)\]\((?<url>[^)]+)\): (?<description>.*)$", RegexOptions.Multiline)]
    private static partial Regex PageLine();
}
