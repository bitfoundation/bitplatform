namespace Boilerplate.Tests.Features.Pwa;

/// <summary>
/// An installed app updates through its service worker, which downloads every asset it lists before the new version takes
/// over. One listed asset that the server answers 404 for, and the new version is never installed: whoever has the app
/// keeps the old one, with none of the pages added since.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ServiceWorkerAssetsTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The build lists most assets itself. The few it can't know are written by hand in service-worker.published.js, and a
    /// hand-written list goes stale without a sign: remove the last .razor.scss of Client.Web and its bundle is no longer
    /// built, while the list still names it. The other way round is as silent: a bundle that Server.Web's stylesheet
    /// imports and the list leaves out is missing once the app is offline.
    /// </summary>
    [TestMethod]
    public async Task TheServiceWorkersOwnList_Should_NameOnlyStyleBundlesTheServerServes_AndEveryOneItsStylesheetImports()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = server.WebAppAddress };

        var worker = await httpClient.GetStringAsync("/service-worker.published.js", TestContext.CancellationToken);
        var list = Regex.Match(worker, @"self\.externalAssets\s*=\s*\[(?<list>.*?)\];", RegexOptions.Singleline);
        Assert.IsTrue(list.Success, "service-worker.published.js sets self.externalAssets.");

        var listed = Regex.Matches(list.Groups["list"].Value, @"url""?\s*:\s*""(?<url>[^""]+\.css)""")
            .Select(match => match.Groups["url"].Value)
            .ToArray();
        Assert.IsNotEmpty(listed, "The list names the server's own stylesheet.");

        foreach (var url in listed)
        {
            using var response = await httpClient.GetAsync($"/{url}", TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"The service worker lists {url}, so the server has to serve it.");
            Assert.IsNotNull(response.Content.Headers.ContentType);
            Assert.AreEqual("text/css", response.Content.Headers.ContentType.MediaType, $"{url} is a stylesheet.");
        }

        // What the stylesheet imports from _content is a referenced project's bundle, which the build lists itself
        var stylesheet = listed.Single(url => url.EndsWith("Server.Web.styles.css", StringComparison.Ordinal));
        var imports = Regex.Matches(await httpClient.GetStringAsync($"/{stylesheet}", TestContext.CancellationToken), @"@import\s+'(?<url>[^']+)'")
            .Select(match => match.Groups["url"].Value)
            .Where(url => url.StartsWith("_content/", StringComparison.Ordinal) is false);

        foreach (var import in imports)
        {
            Assert.Contains(import, listed, $"{stylesheet} imports {import}, which only the service worker's own list can name.");
        }
    }
}
