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
        HashSet<string> previews = [];

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

            if (Regex.Match(html, "<meta property=\"og:image\" content=\"([^\"]+)\"") is { Success: true } preview && previews.Add(preview.Groups[1].Value))
            {
                using var image = await client.GetAsync(preview.Groups[1].Value);

                if (image.StatusCode is not HttpStatusCode.OK || image.Content.Headers.ContentType?.MediaType is not "image/png")
                    failures.Add($"{url} has a preview image that doesn't load: {preview.Groups[1].Value}");
            }

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
    public async Task EverySample_Should_BeInstallableAsItself()
    {
        foreach (var site in LandingSites.All)
        {
            foreach (var (_, culture) in LandingCultures.Supported)
            {
                var home = LandingCultures.Localize(site.Path.Length is 0 ? LandingRoutes.Home : site.Path, culture);
                var link = Regex.Match(await client.GetStringAsync(home), "<link rel=\"manifest\" href=\"([^\"]+)\"");

                Assert.IsTrue(link.Success, $"{home} links no manifest");

                using var manifest = JsonDocument.Parse(await client.GetStringAsync(link.Groups[1].Value));

                Assert.AreEqual(site.Name, manifest.RootElement.GetProperty("name").GetString(), home);
                Assert.AreEqual(home, manifest.RootElement.GetProperty("start_url").GetString(), home);
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
    public async Task TheGallery_Should_OfferTheTourWithoutLoadingIt()
    {
        var html = await client.GetStringAsync(LandingRoutes.Home);

        Assert.DoesNotContain("Bonboarding", html, "The tour's code and styles download only when someone starts it.");

        if (html.Contains("gallery-hero", StringComparison.Ordinal) is false)
        {
            Assert.Inconclusive("The home page is a sample, not the gallery.");
            return;
        }

        Assert.Contains("class=\"gallery-tour\"", html);
    }

    [TestMethod]
    public async Task EveryChatToggle_Should_SayWhetherItIsOpen()
    {
        List<string> failures = [];
        var toggles = 0;

        foreach (var site in LandingSites.All)
        {
            var html = await client.GetStringAsync(site.Path.Length is 0 ? LandingRoutes.Home : site.Path);

            foreach (Match toggle in Regex.Matches(html, "<button[^>]*class=\"lp-chat-(bubble|concierge-tab)[^>]*>"))
            {
                toggles++;

                if (toggle.Value.Contains("aria-expanded=\"false\"", StringComparison.Ordinal) is false)
                {
                    failures.Add($"{site.Key}: {toggle.Value}");
                }
            }
        }

        Assert.AreNotEqual(0, toggles, "No sample has a chat bubble or side tab to check.");
        Assert.IsEmpty(failures, $"A screen reader can't tell these chats are closed: {string.Join(Environment.NewLine, failures)}");
    }

    [TestMethod]
    public async Task EveryLazyImage_Should_SayItIsLazyBeforeItsSource()
    {
        HashSet<string> failures = [];
        var lazy = 0;

        foreach (var url in await SitemapUrls())
        {
            var html = await client.GetStringAsync(url);

            foreach (Match tag in Regex.Matches(html, "<(img|iframe)\\b[^>]*\\bloading=\"lazy\"[^>]*>"))
            {
                lazy++;

                if (tag.Value.IndexOf(" src=", StringComparison.Ordinal) < tag.Value.IndexOf(" loading=", StringComparison.Ordinal))
                {
                    failures.Add(tag.Value);
                }
            }
        }

        Assert.AreNotEqual(0, lazy, "No page has a lazy image to check.");
        Assert.IsEmpty(failures, $"Once the page hydrates, these download right away, because Blazor sets their src before their loading: {string.Join(Environment.NewLine, failures)}");
    }

    [TestMethod]
    public async Task TheDownloadQr_Should_NotBeHiddenOnAnyScreen()
    {
        var styles = await client.GetStringAsync("Boilerplate.LandingPage.Client.bundle.scp.css");

        Assert.Contains(".lp-qr", styles);
        Assert.DoesNotMatchRegex(new Regex(@"\.lp-qr[^{,]*\{[^}]*display:\s*none"), styles);
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
