using System.Xml.Linq;

namespace Boilerplate.Tests.Features.Seo;

/// <summary>
/// What a search engine or an AI agent meets besides a page's own tags: <c>llms.txt</c> and <c>sitemap.xml</c> list the
/// public pages only, each under a url that answers without a redirect; the links of a public page carry its culture; a
/// page has one canonical address and an alternate per language; and <c>/.well-known/security.txt</c> is served once a
/// contact is configured.
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("SEO")]
public partial class CrawlerTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>The template's public pages: anonymous, indexable and more than a form to sign in.</summary>
    private static readonly string[] PublicPages = [PageUrls.Home, PageUrls.Terms, PageUrls.PrivacyPolicy, PageUrls.About];

    /// <summary>Pages that only exist to sign in, or that need an account to show anything.</summary>
    private static readonly string[] AccountPages = [PageUrls.SignIn, PageUrls.SignUp, PageUrls.Settings, PageUrls.Users, PageUrls.Roles];

    [TestMethod]
    public async Task LlmsTxt_Should_NameTheAppAsConfigured_AndListOnlyThePublicPages()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestConfiguration: configuration =>
        {
            configuration["LlmsTxt:Name"] = "Contoso Bikes";
            configuration["LlmsTxt:Summary"] = "Contoso Bikes rents bikes by the hour.";
        }).Start(TestContext.CancellationToken);

        using var httpClient = NoRedirectClient(server);

        using var response = await httpClient.GetAsync("llms.txt", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var llms = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.StartsWith("# Contoso Bikes\n\n> Contoso Bikes rents bikes by the hour.\n", llms, "llms.txt opens with the configured name and summary (https://llmstxt.org).");

        var links = PageLine().Matches(llms).ToArray();
        var listed = links.Select(link => link.Groups["path"].Value).ToArray();

        foreach (var page in PublicPages)
        {
            Assert.Contains(page, listed, $"llms.txt lists {page}.");
        }
        foreach (var page in AccountPages)
        {
            Assert.DoesNotContain(page, listed, $"llms.txt leaves {page} out: an agent can't use it without an account.");
        }
        Assert.DoesNotContain("Requires sign-in", llms, "llms.txt lists only pages a reader can open.");

        foreach (var link in links)
        {
            var url = new Uri(link.Groups["url"].Value);

            Assert.AreEqual(server.WebAppAddress.Authority, url.Authority, $"llms.txt links {url}.");
            if (CultureInfoManager.InvariantGlobalization is false)
            {
                Assert.StartsWith($"/{CultureInfoManager.DefaultCulture.Name}/", url.AbsolutePath, $"{url} carries its culture, so an agent meets no redirect.");
            }

            using var page = await httpClient.GetAsync(url, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, page.StatusCode, $"{url}, listed in llms.txt, answered {(int)page.StatusCode} {page.Headers.Location}.");
        }
    }

    /// <summary>
    /// The sitemap leaves out sign-in and sign-up, which stay indexable (See NotInSitemapAttribute), and every address it
    /// lists answers as it is: a sitemap entry that redirects is one a crawler flags rather than indexes.
    /// </summary>
    [TestMethod]
    public async Task TheSitemap_Should_ListThePublicPages_UnderAddressesThatAnswerWithoutARedirect()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        using var httpClient = NoRedirectClient(server);

        var sitemap = XDocument.Parse(await httpClient.GetStringAsync("sitemap.xml", TestContext.CancellationToken));
        var locs = sitemap.Descendants(XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9")).Select(loc => new Uri(loc.Value)).ToArray();
        var pages = locs.Select(loc => BarePath(loc.AbsolutePath)).Distinct().ToArray();

        foreach (var page in PublicPages)
        {
            Assert.Contains(page, pages, $"The sitemap lists {page}.");
        }
        foreach (var page in AccountPages)
        {
            Assert.DoesNotContain(page, pages, $"The sitemap leaves {page} out.");
        }

        string[] cultures = CultureInfoManager.InvariantGlobalization ? [] : [CultureInfoManager.DefaultCulture.Name, "fa-IR"];
        foreach (var loc in locs.Where(loc => cultures.Length is 0 || cultures.Any(culture => loc.AbsolutePath.StartsWith($"/{culture}/", StringComparison.Ordinal))))
        {
            using var response = await httpClient.GetAsync(loc, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{loc}, listed in the sitemap, answered {(int)response.StatusCode} {response.Headers.Location}.");
        }
    }

    /// <summary>
    /// A link on a public page carries the page's culture. A culture-less one redirects a crawler by its own language,
    /// usually to the default culture, so the Persian page's links led it to English pages, through a redirect.
    /// </summary>
    [TestMethod, TestCategory("PreRendering")]
    public async Task TheLinksOfAPublicPage_Should_CarryItsCulture()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("The urls of an invariant globalization build carry no culture.");
            return;
        }

        await using var server = await StartPrerenderingServer();
        using var httpClient = NoRedirectClient(server);

        foreach (var culture in new[] { CultureInfoManager.DefaultCulture.Name, "fa-IR" })
        {
            foreach (var page in PublicPages)
            {
                var path = page is "/" ? $"/{culture}/" : $"/{culture}{page}";

                using var response = await httpClient.GetAsync(path, TestContext.CancellationToken);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{path} answered {(int)response.StatusCode} {response.Headers.Location}.");
                var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

                var links = Regex.Matches(html, "<a\\b[^>]*?\\bhref=\"(?<href>/[^\"]*)\"")
                    .Select(match => WebUtility.HtmlDecode(match.Groups["href"].Value))
                    .Where(href => href.StartsWith("//", StringComparison.Ordinal) is false && Path.HasExtension(href.Split('?', '#')[0]) is false)
                    .Distinct()
                    .ToArray();
                Assert.IsNotEmpty(links, $"{path} has links of the app's own.");

                foreach (var link in links)
                {
                    Assert.IsTrue(link.StartsWith($"/{culture}/", StringComparison.Ordinal),
                        $"{path} links to {link}, which a crawler would be redirected from.");
                }
            }
        }
    }

    /// <summary>
    /// A page answers with a trailing slash too, as routing ignores it, but names the address without it as canonical, so
    /// it has one address. The home page keeps its slash. Each language also has a language-only hreflang, so a Spanish
    /// reader in Mexico gets the Spanish page rather than x-default's.
    /// </summary>
    [TestMethod, TestCategory("PreRendering")]
    public async Task APage_Should_HaveOneAddress_AndAnAlternatePerLanguage()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("An invariant globalization build has no alternates.");
            return;
        }

        await using var server = await StartPrerenderingServer();
        using var httpClient = NoRedirectClient(server);
        var origin = server.WebAppAddress.ToString().TrimEnd('/');

        foreach (var (path, canonical, bare) in new[]
        {
            ($"/en-US{PageUrls.Terms}/", $"{origin}/en-US{PageUrls.Terms}", PageUrls.Terms),
            ($"/en-US{PageUrls.Terms}", $"{origin}/en-US{PageUrls.Terms}", PageUrls.Terms),
            ("/en-US", $"{origin}/en-US/", PageUrls.Home),
            ("/en-US/", $"{origin}/en-US/", PageUrls.Home)
        })
        {
            using var response = await httpClient.GetAsync(path, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{path} answered {(int)response.StatusCode} {response.Headers.Location}.");
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

            Assert.AreEqual(canonical, Regex.Match(html, "<link rel=\"canonical\" href=\"(?<href>[^\"]*)\"").Groups["href"].Value, $"{path}'s canonical");

            var alternates = Regex.Matches(html, "<link rel=\"alternate\" hreflang=\"(?<lang>[^\"]+)\" href=\"(?<href>[^\"]+)\"")
                .ToDictionary(match => match.Groups["lang"].Value, match => match.Groups["href"].Value);

            foreach (var (language, culture) in new[] { ("en", "en-US"), ("nl", "nl-NL"), ("fa", "fa-IR"), ("es", "es-ES"), ("ar", "ar-SA"), ("zh", "zh-CN") })
            {
                Assert.AreEqual($"{origin}/{culture}{bare}", alternates.GetValueOrDefault(language), $"{path}'s {language} alternate");
            }
            Assert.AreEqual($"{origin}/en-GB{bare}", alternates.GetValueOrDefault("en-GB"), $"{path}'s en-GB alternate");
            Assert.AreEqual($"{origin}{bare}", alternates.GetValueOrDefault("x-default"), $"{path}'s x-default");
        }
    }

    [TestMethod]
    public async Task SecurityTxt_Should_BeServed_OnlyOnceAContactIsConfigured()
    {
        await using (var server = new AppTestServer())
        {
            await server.Build().Start(TestContext.CancellationToken);
            using var httpClient = NoRedirectClient(server);

            using var response = await httpClient.GetAsync(".well-known/security.txt", TestContext.CancellationToken);
            // With no contact there is nobody to name, so the address is an unknown one, sent to the not-found page
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode, "With no contact there is nobody to name, so there is no security.txt.");
            Assert.IsNotNull(response.Headers.Location);
            Assert.StartsWith($"{PageUrls.NotFound}?", response.Headers.Location.OriginalString);
        }

        await using (var server = new AppTestServer())
        {
            await server.Build(configureTestConfiguration: configuration =>
            {
                configuration["SecurityTxt:Contact:0"] = "mailto:security@contoso.com";
                configuration["SecurityTxt:PreferredLanguages"] = "en, nl";
            }).Start(TestContext.CancellationToken);
            using var httpClient = NoRedirectClient(server);

            using var response = await httpClient.GetAsync(".well-known/security.txt", TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsNotNull(response.Content.Headers.ContentType);
            Assert.AreEqual("text/plain", response.Content.Headers.ContentType.MediaType);

            var fields = (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split(": ", 2))
                .ToDictionary(field => field[0], field => field[1]);

            Assert.AreEqual("mailto:security@contoso.com", fields["Contact"]);
            Assert.AreEqual("en, nl", fields["Preferred-Languages"]);
            Assert.AreEqual(new Uri(server.WebAppAddress, ".well-known/security.txt").ToString(), fields["Canonical"]);

            // RFC 9116: required, and less than a year ahead
            var expires = DateTimeOffset.Parse(fields["Expires"], CultureInfo.InvariantCulture);
            Assert.IsGreaterThan(DateTimeOffset.UtcNow, expires, "security.txt has expired.");
            Assert.IsLessThan(DateTimeOffset.UtcNow.AddYears(1), expires, "Expires is less than a year ahead.");
        }
    }

    private async Task<AppTestServer> StartPrerenderingServer()
    {
        var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.FakeExternalStatistics(),
            configureTestConfiguration: configuration => configuration["WebAppRender:PrerenderEnabled"] = "true"
        ).Start(TestContext.CancellationToken);

        return server;
    }

    /// <summary>
    /// A bare client that follows no redirect: a redirect is what these tests look for, and what a crawler reads is what
    /// the server wrote. Without cookies or Accept-Language, a culture-less url resolves to the default culture.
    /// </summary>
    private static HttpClient NoRedirectClient(AppTestServer server) =>
        new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { BaseAddress = server.WebAppAddress };

    /// <summary>The path without its culture segment: /en-US/terms is /terms, and /en-US/ is /.</summary>
    private static string BarePath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var rest = segments.Length > 0 && CultureInfoManager.GetCultureInfo(segments[0]) is not null ? segments[1..] : segments;
        return $"/{string.Join('/', rest)}";
    }

    /// <summary>"- [/terms](https://myapp.com/en-US/terms): Legal terms, ..." (See UseSiteMap's llms.txt).</summary>
    [GeneratedRegex(@"^- \[(?<path>[^\]]+)\]\((?<url>[^)]+)\): (?<description>.+)$", RegexOptions.Multiline)]
    private static partial Regex PageLine();
}
