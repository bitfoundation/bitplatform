using System.Xml.Linq;

namespace Boilerplate.LandingPage.Tests.Features;

[TestClass]
public class PageTests
{
    private static readonly XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly string[] headTags = ["<title>", "name=\"description\"", "rel=\"canonical\"", "property=\"og:title\"", "name=\"twitter:card\"", .. LandingCultures.InvariantGlobalization ? Array.Empty<string>() : ["hreflang=\"x-default\""]];

    private static LandingTestServer server = default!;
    private static HttpClient client = default!;

    [ClassInitialize]
    public static async Task Start(TestContext testContext)
    {
        server = await new LandingTestServer().Start();
        client = server.CreateClient();
    }

    [ClassCleanup]
    public static async Task Stop()
    {
        client.Dispose();
        await server.DisposeAsync();
    }

    [TestMethod]
    public async Task EveryPageInTheSitemap_Should_BePrerenderedWithItsOwnHead()
    {
        List<string> failures = [];

        foreach (var url in await SitemapUrls())
        {
            using var response = await client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                failures.Add($"{url} answered {(int)response.StatusCode}");
                continue;
            }

            failures.AddRange(headTags.Where(tag => html.Contains(tag, StringComparison.Ordinal) is false).Select(tag => $"{url} has no {tag}"));

            var persian = new Uri(url).AbsolutePath.StartsWith("/fa-IR", StringComparison.Ordinal);
            if (html.Contains(persian ? "<html lang=\"fa-IR\" dir=\"rtl\"" : "<html lang=\"en-US\" dir=\"ltr\"", StringComparison.Ordinal) is false)
                failures.Add($"{url} has the wrong language or direction");

            if (Regex.Count(html, "<h1[ >]") is not 1)
                failures.Add($"{url} has {Regex.Count(html, "<h1[ >]")} h1 elements");

            if (Regex.Count(html, "class=\"lp-site ") is not 1)
                failures.Add($"{url} renders {Regex.Count(html, "class=\"lp-site ")} layouts");

            foreach (Match script in Regex.Matches(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline))
            {
                try
                {
                    JsonDocument.Parse(script.Groups[1].Value).Dispose();
                }
                catch (JsonException exp)
                {
                    failures.Add($"{url} has structured data that isn't JSON: {exp.Message}");
                }
            }
        }

        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public async Task EverySample_Should_BeInTheSitemapInEveryLanguage()
    {
        var paths = (await SitemapUrls()).Select(url => new Uri(url).AbsolutePath.TrimEnd('/')).ToHashSet(StringComparer.Ordinal);

        foreach (var site in LandingSites.All)
        {
            foreach (var (_, culture) in LandingCultures.Supported)
            {
                var path = LandingCultures.Localize(site.Path.Length is 0 ? LandingRoutes.Home : site.Path, culture).TrimEnd('/');

                Assert.Contains(path, paths, $"{path} isn't in the sitemap");
            }
        }
    }

    [TestMethod]
    public async Task TheSitemap_Should_LeaveOutWhatSearchEnginesShouldNotList()
    {
        var urls = await SitemapUrls();

        Assert.DoesNotContain(url => url.Contains("/waitlist/", StringComparison.Ordinal), urls, "The waitlist's confirm and unsubscribe pages are noindex.");
        Assert.DoesNotContain(url => url.EndsWith(LandingRoutes.NotFound, StringComparison.Ordinal), urls);
    }

    [TestMethod]
    public async Task OutsideProduction_Should_TellSearchEnginesNotToIndex()
    {
        using var response = await client.GetAsync(LandingSites.All[0].Path);

        Assert.AreEqual("noindex, nofollow", string.Join(", ", response.Headers.GetValues("X-Robots-Tag")));
    }

    [TestMethod]
    public async Task ThePublicAddress_Should_BuildTheCanonicalLinksAndTheSitemap()
    {
        await using var configured = await new LandingTestServer().Start(before: builder =>
        {
            builder.Configuration["Site:Url"] = "https://www.example.com/";
            builder.Configuration["Site:AllowIndexing"] = "true";
        });
        using var configuredClient = configured.CreateClient();
        var path = LandingSites.All[0].Path.Length is 0 ? LandingRoutes.Home : LandingSites.All[0].Path;

        using var response = await configuredClient.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        var sitemap = await configuredClient.GetStringAsync("sitemap.xml");

        Assert.Contains($"rel=\"canonical\" href=\"https://www.example.com{path}\"", html);
        Assert.IsFalse(response.Headers.Contains("X-Robots-Tag"));
        Assert.IsTrue(XDocument.Parse(sitemap).Descendants(sitemapNamespace + "loc").All(loc => loc.Value.StartsWith("https://www.example.com/", StringComparison.Ordinal)));
    }

    private static async Task<string[]> SitemapUrls()
    {
        var sitemap = XDocument.Parse(await client.GetStringAsync("sitemap.xml"));

        return [.. sitemap.Descendants(sitemapNamespace + "loc").Select(loc => loc.Value)];
    }
}
