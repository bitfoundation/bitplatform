namespace Boilerplate.LandingPage.Tests.Features;

[TestClass]
public class NotFoundTests
{
    private static LandingTestServer server = default!;
    private static HttpClient client = default!;

    public static IEnumerable<object[]> MissingPages
    {
        get
        {
            foreach (var site in LandingSites.All)
            {
                foreach (var (_, culture) in LandingCultures.Supported)
                {
                    yield return [LandingCultures.Localize($"{site.Path}/no/such/page", culture), $"lp-{site.Key}"];
                }
            }

            if (LandingSites.All.Any(site => site.Path.Length is 0) is false)
            {
                yield return ["/no-such-page", "lp-gallery"];
            }
        }
    }

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
    [DynamicData(nameof(MissingPages))]
    public async Task AMissingPage_Should_Answer404InTheLayoutItBelongsTo(string path, string layout)
    {
        using var response = await client.GetAsync(path, TestContext.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual(1, Regex.Count(html, "class=\"lp-site "), $"{path} renders more than one layout");
        Assert.Contains($"class=\"lp-site {layout}\"", html);
        Assert.Contains("name=\"robots\" content=\"noindex\"", html);
    }

    [TestMethod]
    public async Task ACultureInAnotherForm_Should_RedirectPermanently()
    {
        if (LandingCultures.InvariantGlobalization)
        {
            Assert.Inconclusive("The landing page is in English only.");
            return;
        }

        var path = LandingSites.All[0].Path;

        await AssertRedirect($"/{LandingCultures.Default.Name}{path}", path.Length is 0 ? LandingRoutes.Home : path);

        foreach (var (_, culture) in LandingCultures.Supported.Where(supported => supported.Culture.Name != LandingCultures.Default.Name))
        {
            await AssertRedirect($"/{culture.Name.ToUpperInvariant()}{path}", $"/{culture.Name}{path}");
        }
    }

    private static async Task AssertRedirect(string from, string to)
    {
        using var response = await client.GetAsync(from);

        Assert.AreEqual(HttpStatusCode.MovedPermanently, response.StatusCode, from);
        Assert.AreEqual(to, new Uri(server.Address, response.Headers.Location!).AbsolutePath, from);
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
