using System.Xml.Linq;
using Boilerplate.LandingPage.Client.Infrastructure.Services;

namespace Boilerplate.LandingPage.Tests.Features;

[TestClass]
public class PageTests
{
    private static readonly Regex scriptTagWithSource = new(@"<script[^>]*\ssrc=");
    private static readonly XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly string[] headTags = ["<title>", "name=\"description\"", "rel=\"canonical\"", "property=\"og:title\"", "name=\"twitter:card\"", .. LandingCultures.InvariantGlobalization ? Array.Empty<string>() : ["hreflang=\"x-default\""]];

    private const string googlebot = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
    private const string lighthouse = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Chrome-Lighthouse";
    private const string pageSpeed = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko; Google Page Speed Insights) Chrome/75.0.3777.100 Safari/537.36 Chrome-Lighthouse";
    private const string browser = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
    private const string duckDuckGoBrowser = "Mozilla/5.0 (Linux; Android 13; K) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/116.0.0.0 Mobile DuckDuckGo/5 Safari/537.36";

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
    [DataRow(googlebot)]
    [DataRow(lighthouse)]
    [DataRow(pageSpeed)]
    public async Task ACrawlerOrABenchmark_Should_GetThePageWithoutItsScripts(string userAgent)
    {
        using var response = await GetAs(client, userAgent, SamplePath());
        var html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1", html);
        Assert.DoesNotMatchRegex(scriptTagWithSource, html);
        Assert.DoesNotContain("Blazor.start", html);
    }

    [TestMethod]
    [DataRow(browser)]
    [DataRow(duckDuckGoBrowser)]
    public async Task AVisitor_Should_GetThePageWithItsScripts(string userAgent)
    {
        using var response = await GetAs(client, userAgent, SamplePath());
        var html = await response.Content.ReadAsStringAsync();

        Assert.MatchesRegex(scriptTagWithSource, html);
        Assert.Contains("Blazor.start", html);
    }

    [TestMethod]
    public async Task ACrawler_Should_GetTheScripts_WhenNothingIsPrerendered()
    {
        await using var unrendered = await new LandingTestServer().Start(before: builder => builder.Configuration["WebAppRender:PrerenderEnabled"] = "false");
        using var unrenderedClient = unrendered.CreateClient();

        using var response = await GetAs(unrenderedClient, googlebot, SamplePath());

        Assert.MatchesRegex(scriptTagWithSource, await response.Content.ReadAsStringAsync(), "With no page to read, the scripts are the only way to fill it in.");
    }

    [TestMethod]
    [DataRow(googlebot)]
    [DataRow(lighthouse)]
    [DataRow(pageSpeed)]
    public async Task AScriptLessPage_Should_NeverBeOfferedToACdn(string userAgent)
    {
        await using var production = await new LandingTestServer().Start(environment: "Production");
        using var productionClient = production.CreateClient();

        using var scriptLess = await GetAs(productionClient, userAgent, SamplePath());
        using var visitors = await GetAs(productionClient, browser, SamplePath());

        Assert.IsTrue(scriptLess.Headers.CacheControl!.Private, scriptLess.Headers.CacheControl.ToString());
        Assert.IsFalse(scriptLess.Headers.CacheControl.Public, scriptLess.Headers.CacheControl.ToString());
        Assert.IsNull(scriptLess.Headers.CacheControl.SharedMaxAge, scriptLess.Headers.CacheControl.ToString());

        Assert.IsTrue(visitors.Headers.CacheControl!.Public, "A visitor's page stays cacheable, so the check above isn't just caching being off.");
        Assert.IsNotNull(visitors.Headers.CacheControl.SharedMaxAge);
    }

    [TestMethod]
    public async Task ACrawlersSitemap_Should_CacheLikeAnyoneElses()
    {
        await using var production = await new LandingTestServer().Start(environment: "Production");
        using var productionClient = production.CreateClient();

        using var crawlers = await GetAs(productionClient, googlebot, "sitemap.xml");
        using var visitors = await GetAs(productionClient, browser, "sitemap.xml");

        Assert.AreEqual(HttpStatusCode.OK, crawlers.StatusCode);
        var expected = visitors.Headers.CacheControl?.ToString();
        var actual = crawlers.Headers.CacheControl?.ToString();

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task OutsideProduction_Should_TellSearchEnginesNotToIndex()
    {
        using var response = await client.GetAsync(LandingSites.All[0].Path);

        Assert.AreEqual("noindex, nofollow", string.Join(", ", response.Headers.GetValues("X-Robots-Tag")));
    }

    [TestMethod]
    public async Task ThePublicAddress_Should_ComeFromTheRequest()
    {
        await using var indexed = await new LandingTestServer().Start(before: builder =>
        {
            builder.Configuration["Site:Url"] = "https://ignored.example.com/";
            builder.Configuration["Site:AllowIndexing"] = "true";
        });
        using var indexedClient = indexed.CreateClient();
        var path = SamplePath();

        using var response = await GetFrom(indexedClient, "www.example.com", path);
        using var sitemapResponse = await GetFrom(indexedClient, "www.example.com", "sitemap.xml");
        var html = await response.Content.ReadAsStringAsync();
        var sitemap = await sitemapResponse.Content.ReadAsStringAsync();

        Assert.Contains($"rel=\"canonical\" href=\"http://www.example.com{path}\"", html);
        Assert.IsFalse(response.Headers.Contains("X-Robots-Tag"));
        Assert.IsTrue(XDocument.Parse(sitemap).Descendants(sitemapNamespace + "loc").All(loc => loc.Value.StartsWith("http://www.example.com/", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task TheSeoFiles_Should_ComeFromTheOutputCache_ForEachHost()
    {
        await using var cached = await new LandingTestServer().Start();
        using var cachedClient = cached.CreateClient();

        foreach (var file in new[] { "robots.txt", "sitemap.xml", "llms.txt" })
        {
            using var first = await GetFrom(cachedClient, "one.example.com", file);
            using var second = await GetFrom(cachedClient, "one.example.com", file);
            using var otherHost = await GetFrom(cachedClient, "two.example.com", file);

            Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
            Assert.IsFalse(first.Headers.Age.HasValue, $"{file} should be rendered the first time.");
            Assert.IsTrue(second.Headers.Age.HasValue, $"{file} should come from the output cache the second time.");
            Assert.Contains("two.example.com", await otherHost.Content.ReadAsStringAsync(), $"{file} must not hand one host's addresses to another.");
        }
    }

    [TestMethod]
    public async Task APage_Should_SayItsLanguageInTheResponse()
    {
        using var english = await client.GetAsync(SamplePath());

        Assert.AreEqual("en-US", english.Content.Headers.ContentLanguage.Single());

        if (LandingCultures.InvariantGlobalization)
        {
            Assert.Inconclusive("The landing page is in English only.");
            return;
        }

        using var persian = await client.GetAsync($"/fa-IR{SamplePath()}");

        Assert.AreEqual("fa-IR", persian.Content.Headers.ContentLanguage.Single());
    }

    [TestMethod]
    public async Task TheHealthEndpoint_Should_SayDegraded_WithoutAnSmtpServer()
    {
        using var response = await client.GetAsync("health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Degraded", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task TheHealthEndpoint_Should_FailWhenTheSmtpServerDoes_AndLeaveAliveAlone()
    {
        await using var noMail = await new LandingTestServer().Start(before: builder => builder.Configuration["ConnectionStrings:smtp"] = "Endpoint=smtp://127.0.0.1:1");
        using var noMailClient = noMail.CreateClient();

        using var health = await noMailClient.GetAsync("health");
        using var alive = await noMailClient.GetAsync("alive");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        Assert.AreEqual("Unhealthy", await health.Content.ReadAsStringAsync(), "The answer stays the bare status: this endpoint is public.");
        Assert.AreEqual(HttpStatusCode.OK, alive.StatusCode, "Deployments check /alive, and a mail outage must not restart the site.");
    }

    [TestMethod]
    [DataRow("Email:DefaultFromEmail", "not-an-email")]
    [DataRow("Email:DefaultFromName", "")]
    [DataRow("AI:OpenAI:ChatModel", "")]
    public async Task InvalidSettings_Should_StopTheStart(string key, string value)
    {
        await using var invalid = new LandingTestServer();

        await Assert.ThrowsAsync<Microsoft.Extensions.Options.OptionsValidationException>(() => invalid.Start(before: builder => builder.Configuration[key] = value));
    }

    [TestMethod]
    public async Task AFailureWhilePrerendering_Should_AnswerAnErrorThatNoCdnKeeps()
    {
        if (LandingSites.Find("ecommerce") is not { } sample)
        {
            Assert.Inconclusive("The sample that shows the rating is gone.");
            return;
        }

        await using var failing = await new LandingTestServer().Start(after: services => services.AddScoped<IStoreListingClient, FailingStoreListingClient>(), environment: "Production");
        using var failingClient = failing.CreateClient();

        using var response = await failingClient.GetAsync(sample.Path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Something went wrong on our side", html);
        Assert.DoesNotContain("lp-error", html, "The plain page, not the panel of a page answering 200.");
        Assert.IsFalse(response.Headers.CacheControl?.Public ?? false, "A CDN must not keep it.");
    }

    [TestMethod]
    public async Task TheHome_Should_ShowItsScoresWithTheirSource()
    {
        var html = await client.GetStringAsync(LandingRoutes.Home);

        if (html.Contains("gallery-hero", StringComparison.Ordinal) is false)
        {
            Assert.Inconclusive("The home page is a sample, not the gallery.");
            return;
        }

        Assert.AreEqual(4, Regex.Count(html, "class=\"score-ring\""));
        Assert.Contains("https://pagespeed.web.dev", html);
        Assert.Contains("4 November 2026", html);
    }

    private sealed class FailingStoreListingClient : IStoreListingClient
    {
        public Task<Client.Infrastructure.Models.StoreListing?> GetAppStoreListingAsync(string appleAppId, CancellationToken cancellationToken) => throw new InvalidOperationException("The App Store lookup failed.");
    }

    private static string SamplePath() => LandingSites.All[0].Path.Length is 0 ? LandingRoutes.Home : LandingSites.All[0].Path;

    private static async Task<HttpResponseMessage> GetFrom(HttpClient httpClient, string host, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Host = host;

        return await httpClient.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetAs(HttpClient httpClient, string userAgent, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

        return await httpClient.SendAsync(request);
    }

    private static async Task<string[]> SitemapUrls()
    {
        var sitemap = XDocument.Parse(await client.GetStringAsync("sitemap.xml"));

        return [.. sitemap.Descendants(sitemapNamespace + "loc").Select(loc => loc.Value)];
    }
}
