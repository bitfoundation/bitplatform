using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bit.Bswup.Demo.Server.Controllers;
using Bit.Bswup.Tests.Mcp.TestInfra;
using ModelContextProtocol.Server;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// The plain HTTP mirror of the tools, under <c>/api/mcp/...</c>. It exists so every tool is
/// inspectable from a browser without an MCP client, which makes it the first thing anyone uses to
/// check a deployment - so a route that quietly stopped existing is a debugging aid that is gone
/// exactly when it is needed. The two tools whose input is a whole file also answer to POST,
/// because a service-worker file does not fit in a query string.
/// </summary>
[TestClass]
public class HttpMirrorTests
{
    private static McpTestServer _server = null!;

    [ClassInitialize]
    public static async Task StartAsync(TestContext _) => _server = await McpTestServer.StartAsync();

    [ClassCleanup]
    public static async Task StopAsync() => await _server.DisposeAsync();

    /// <summary>Every method the MCP server publishes as a tool, read off the controller itself.</summary>
    private static string[] ToolNames =>
    [
        .. typeof(McpController).GetMethods()
            .Select(method => method.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: true).FirstOrDefault())
            .OfType<McpServerToolAttribute>()
            .Select(attribute => attribute.Name!)
    ];

    [TestMethod]
    public async Task EveryTool_HasAnHttpRoute()
    {
        foreach (var tool in ToolNames)
        {
            var response = await _server.Http.GetAsync($"/api/mcp/{tool}");

            // A tool needing arguments answers 400, which still proves the route is there; only a
            // 404 means the mirror lost it.
            Assert.AreNotEqual(HttpStatusCode.NotFound, response.StatusCode, tool);
        }
    }

    [TestMethod]
    public async Task ProseTools_AnswerAsText()
    {
        var response = await _server.Http.GetAsync("/api/mcp/GetBswupOverview");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "# bit Bswup");
    }

    [TestMethod]
    public async Task DataTools_AnswerAsJson()
    {
        var options = await _server.GetJsonAsync("/api/mcp/GetBswupScriptOptions");

        Assert.AreEqual(JsonValueKind.Array, options.ValueKind);
        Assert.IsTrue(options.EnumerateArray().Any(option => option.GetProperty("name").GetString() == "stallTimeout"));
    }

    [TestMethod]
    public async Task QueryStringArguments_AreBound()
    {
        var hits = await _server.GetJsonAsync("/api/mcp/SearchBswup?query=externalAssets&limit=3");

        Assert.IsTrue(hits.GetArrayLength() is > 0 and <= 3);

        var guide = await _server.Http.GetStringAsync("/api/mcp/GetBswupSetupGuide?hostingModel=standalone-wasm");

        StringAssert.Contains(guide, "## Checklist");

        var page = await _server.Http.GetStringAsync("/api/mcp/GetBswupDocsPage?slug=events");

        StringAssert.Contains(page, "bit Bswup documentation page: /events");
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_TakesAWholeFileInThePostBody()
    {
        var report = await _server.PostJsonAsync("/api/mcp/InspectBswupServiceWorker",
            new { script = ServiceWorkerFixtures.SettingAfterImport });

        var problems = string.Join("\n", report.GetProperty("problems").EnumerateArray().Select(problem => problem.GetString()));

        StringAssert.Contains(problems, "AFTER the importScripts line");
    }

    [TestMethod]
    public async Task AnalyzeBswupAssetCaching_TakesAWholeFileInThePostBody()
    {
        var analysis = await _server.PostJsonAsync("/api/mcp/AnalyzeBswupAssetCaching",
            new { script = ServiceWorkerFixtures.Clean, assetUrls = "css/app.css\nservice-worker.js" });

        var assets = analysis.GetProperty("assets").EnumerateArray()
            .ToDictionary(asset => asset.GetProperty("url").GetString()!, asset => asset.GetProperty("cached").GetBoolean());

        Assert.IsTrue(assets["css/app.css"]);
        Assert.IsFalse(assets["service-worker.js"]);
    }

    [TestMethod]
    public async Task PostAndGet_AnswerIdenticallyForTheSameFile()
    {
        // The POST form only exists so a bigger input fits; it must not be a second implementation.
        const string script = "self.isPassive = false;\nself.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');";

        var fromQuery = await _server.Http.GetStringAsync($"/api/mcp/InspectBswupServiceWorker?script={Uri.EscapeDataString(script)}");

        var response = await _server.Http.PostAsJsonAsync("/api/mcp/InspectBswupServiceWorker", new { script });
        var fromBody = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(fromQuery, fromBody);
    }

    [TestMethod]
    public async Task PostWithoutTheRequiredProperty_IsRejected()
    {
        var response = await _server.Http.PostAsJsonAsync("/api/mcp/InspectBswupServiceWorker", new { notScript = "x" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task TheSitesOwnRoutesAreNotShadowedByTheApi()
    {
        // /api/... and /mcp are literal routes no page owns, and the site still answers on its own.
        var sitemap = await _server.Http.GetAsync("/sitemap.xml");

        Assert.AreEqual(HttpStatusCode.OK, sitemap.StatusCode);
        Assert.AreEqual("application/xml", sitemap.Content.Headers.ContentType?.MediaType);

        var xml = await sitemap.Content.ReadAsStringAsync();

        StringAssert.Contains(xml, "<urlset");
        StringAssert.Contains(xml, "https://bswup.bitplatform.dev/service-worker");
    }

    [TestMethod]
    public async Task Sitemap_LeavesOutThePagesMarkedNoIndex()
    {
        var xml = await _server.Http.GetStringAsync("/sitemap.xml");

        foreach (var url in Bit.Bswup.Demo.Client.SiteMetadata.NoIndexUrls)
        {
            Assert.IsFalse(xml.Contains($"<loc>https://bswup.bitplatform.dev{url}</loc>", StringComparison.Ordinal), url);
        }
    }

    [TestMethod]
    public async Task Sitemap_ListsEveryIndexablePage()
    {
        var xml = await _server.Http.GetStringAsync("/sitemap.xml");

        var expected = Bit.Bswup.Demo.Client.DocsCatalog.AllPages
            .Select(page => page.Url)
            .Where(url => Bit.Bswup.Demo.Client.SiteMetadata.NoIndexUrls.Contains(url) is false);

        foreach (var url in expected)
        {
            var absolute = Bit.Bswup.Demo.Client.SiteMetadata.AbsoluteUrl(url);

            StringAssert.Contains(xml, $"<loc>{absolute}</loc>", url);
        }
    }
}
