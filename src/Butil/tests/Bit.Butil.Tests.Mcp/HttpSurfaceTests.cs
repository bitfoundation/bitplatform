using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// The plain HTTP surface around the MCP server: the GET mirror of every tool, the CORS the
/// browser-based clients need, and the discovery files that point an assistant at the endpoint in
/// the first place.
/// <para>
/// None of this is reachable through an MCP client, and every piece of it is a way the server can
/// be perfectly correct and still unusable: an endpoint a browser is not allowed to read, a tool
/// nobody can inspect without wiring up a client, a /mcp nothing advertises. It is also the part
/// most easily broken by an unrelated change to the app's middleware, which is the argument for
/// testing it against the real pipeline rather than a hand-built one.
/// </para>
/// </summary>
[TestClass]
public class HttpSurfaceTests : McpTestBase
{
    private static HttpClient Http => McpServerFixture.Http;

    [TestMethod]
    public async Task Every_tool_is_also_a_plain_GET_anyone_can_open_in_a_browser()
    {
        // "The same methods are exposed as plain HTTP GET endpoints under /api/mcp/..., which makes
        // each of them inspectable from a browser." Every tool, or the claim is not true.
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SearchButil"] = "?query=clipboard",
            ["GetButilSetupGuide"] = "?hostingModel=wasm",
            ["GetButilApiDetails"] = "?typeName=Clipboard",
            ["PlanButilFeature"] = "?apis=Clipboard",
            ["GetButilDocsPage"] = "?slug=clipboard",
            ["GetButilGuideSection"] = "?heading=Getting%20started",
            ["GetButilSourceFile"] = "?path=Demo/Client/Pages/ClipboardPage.razor",
        };

        foreach (var tool in ButilMcp.Tools.Keys)
        {
            var query = arguments.GetValueOrDefault(tool, string.Empty);

            using var response = await Http.GetAsync(McpServerFixture.Url($"api/mcp/{tool}{query}"));
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.Scope())
            {
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"GET /api/mcp/{tool} answered {(int)response.StatusCode}.");
                Assert.IsNotEmpty(body, $"GET /api/mcp/{tool} answered with nothing.");
            }
        }
    }

    [TestMethod]
    public async Task The_GET_mirror_answers_the_same_data_as_the_tool()
    {
        // The mirror is the same method, so a difference here means one of the two paths is doing
        // something the other is not - a filter, a cache, a different origin. Held against the tool
        // itself rather than against a hand-written expectation, which is the only way the two can
        // be shown not to have drifted.
        using var response = await Http.GetAsync(McpServerFixture.Url("api/mcp/GetButilApiDetails?typeName=Clipboard"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var overHttp = JsonSerializer.Deserialize<ApiDetailsResult>(await response.Content.ReadAsStringAsync(), JsonOptions);
        var overMcp = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "Clipboard" });

        using (Assert.Scope())
        {
            Assert.IsNotNull(overHttp);
            Assert.AreEqual("Clipboard", overHttp!.Details?.Name);

            // Re-serialized rather than compared as records: the payload carries arrays, and record
            // equality on those is by reference.
            Assert.AreEqual(JsonSerializer.Serialize(overMcp, JsonOptions), JsonSerializer.Serialize(overHttp, JsonOptions),
                "The GET mirror and the tool answered with different data.");
        }
    }

    [TestMethod]
    public async Task A_browser_may_read_the_mcp_endpoint_from_another_origin()
    {
        // Browser-based MCP clients call /mcp with fetch from another origin, and a browser will not
        // hand them the response unless the server says so. Without this the endpoint is simply
        // unreachable from where a growing share of clients run.
        using var request = new HttpRequestMessage(HttpMethod.Options, McpServerFixture.Url("mcp"));
        request.Headers.Add("Origin", "https://claude.ai");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await Http.SendAsync(request);

        using (Assert.Scope())
        {
            Assert.IsLessThan(400, (int)response.StatusCode, $"The preflight was answered with {(int)response.StatusCode}.");
            Assert.IsTrue(response.Headers.Contains("Access-Control-Allow-Origin"), "The preflight carries no Access-Control-Allow-Origin.");
            Assert.Contains("*", response.Headers.GetValues("Access-Control-Allow-Origin"));

            var allowedMethods = string.Join(",", response.Headers.TryGetValues("Access-Control-Allow-Methods", out var methods) ? methods : []);
            Assert.Contains("POST", allowedMethods);
        }
    }

    [TestMethod]
    public async Task The_negotiated_protocol_version_is_readable_across_origins()
    {
        // Expose-Headers rides on the actual response, not on the preflight - so this has to be a
        // real cross-origin request. Without it a browser client can see the body and not the
        // protocol revision the transport answered with.
        using var request = new HttpRequestMessage(HttpMethod.Post, McpServerFixture.Url("mcp"))
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Origin", "https://claude.ai");
        request.Headers.Add("Accept", "application/json, text/event-stream");

        using var response = await Http.SendAsync(request);

        var exposed = string.Join(",", response.Headers.TryGetValues("Access-Control-Expose-Headers", out var values) ? values : []);

        using (Assert.Scope())
        {
            // First, or a 500 that happens to carry the headers reads as a pass.
            Assert.IsTrue(response.IsSuccessStatusCode, $"The cross-origin tools/list was answered with {(int)response.StatusCode}.");
            Assert.IsTrue(response.Headers.Contains("Access-Control-Allow-Origin"), "The response carries no Access-Control-Allow-Origin.");
            Assert.Contains("MCP-Protocol-Version", exposed, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("WWW-Authenticate", exposed, StringComparison.OrdinalIgnoreCase);

            // AllowAnyOrigin and credentials are mutually exclusive by design, and that is the right
            // way round: no cookie of this site's should ride along on a cross-origin tool call.
            Assert.IsFalse(response.Headers.Contains("Access-Control-Allow-Credentials"));
        }
    }

    [TestMethod]
    public async Task The_api_mirror_is_reachable_from_another_origin_too()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, McpServerFixture.Url("api/mcp/GetButilApiDetails"));
        request.Headers.Add("Origin", "https://example.test");

        using var response = await Http.SendAsync(request);

        using (Assert.Scope())
        {
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(response.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }

    [TestMethod]
    public async Task The_endpoint_is_anonymous()
    {
        // Everything behind these routes is public read-only documentation. A 401 here would mean
        // an MCP client is being asked to authenticate to read a library's docs.
        using var response = await Http.GetAsync(McpServerFixture.Url("api/mcp/GetButilSetupGuide?hostingModel=wasm"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Discovery_files_point_at_the_endpoint()
    {
        using var robots = await Http.GetAsync(McpServerFixture.Url("robots.txt"));
        var robotsBody = await robots.Content.ReadAsStringAsync();

        using var llms = await Http.GetAsync(McpServerFixture.Url("llms.txt"));
        var llmsBody = await llms.Content.ReadAsStringAsync();

        using var sitemap = await Http.GetAsync(McpServerFixture.Url("sitemap.xml"));
        var sitemapBody = await sitemap.Content.ReadAsStringAsync();

        using (Assert.Scope())
        {
            Assert.AreEqual(HttpStatusCode.OK, robots.StatusCode);
            Assert.Contains("Sitemap: ", robotsBody);
            Assert.Contains("/sitemap.xml", robotsBody);

            Assert.AreEqual(HttpStatusCode.OK, llms.StatusCode);
            Assert.StartsWith("# Bit.Butil", llmsBody);
            Assert.Contains("/mcp", llmsBody, "llms.txt is where an assistant learns the MCP endpoint exists.");
            Assert.Contains("/api/mcp/", llmsBody);

            Assert.AreEqual(HttpStatusCode.OK, sitemap.StatusCode);
            Assert.StartsWith("<?xml", sitemapBody);
            Assert.Contains("<urlset", sitemapBody);
        }
    }

    [TestMethod]
    public async Task The_discovery_files_are_generated_from_the_same_nav_the_tools_use()
    {
        // All three are generated from DocsNav rather than written by hand, so a page added to the
        // nav is a page that appears here. A checked-in copy would silently rot, and the way that
        // shows up is a slug the tools know about and the sitemap does not.
        // Both comparisons below are "no page is missing", which an index of no pages satisfies -
        // so the index having arrived at all is asserted before anything is held against it.
        using var response = await Http.GetAsync(McpServerFixture.Url("api/mcp/GetButilDocsPage"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The docs index did not answer over HTTP.");

        var pages = DocsIndexRow.ParseAll(await response.Content.ReadAsStringAsync());
        Assert.IsNotEmpty(pages, "The docs index answered with no rows, so there is nothing to hold the discovery files to.");

        var sitemap = await (await Http.GetAsync(McpServerFixture.Url("sitemap.xml"))).Content.ReadAsStringAsync();
        var llms = await (await Http.GetAsync(McpServerFixture.Url("llms.txt"))).Content.ReadAsStringAsync();

        // The sitemap is parsed rather than searched: a substring test on "/{slug}<" is also
        // satisfied by any longer URL that happens to end in the same slug.
        var sitemapped = XDocument.Parse(sitemap)
            .Descendants()
            .Where(element => element.Name.LocalName == "loc")
            .Select(element => new Uri(element.Value).AbsolutePath.Trim('/'))
            .ToHashSet(StringComparer.Ordinal);

        var missingFromSitemap = pages.Where(page => sitemapped.Contains(page.Slug.Trim('/')) is false).ToArray();
        var missingFromLlms = pages.Where(page => llms.Contains($"/{page.Slug})", StringComparison.Ordinal) is false).ToArray();

        using (Assert.Scope())
        {
            Assert.IsEmpty(missingFromSitemap.Select(page => page.Slug));
            Assert.IsEmpty(missingFromLlms.Select(page => page.Slug));
        }
    }

    [TestMethod]
    public async Task The_endpoint_the_discovery_files_advertise_is_the_one_that_answers()
    {
        // llms.txt tells an assistant where the server is. A relative move of the route would leave
        // that sentence pointing at a 404 that nothing else in the suite would notice.
        var llms = await (await Http.GetAsync(McpServerFixture.Url("llms.txt"))).Content.ReadAsStringAsync();

        Assert.Contains($"{McpServerFixture.BaseUrl}/mcp", llms,
            "llms.txt should advertise the endpoint on the origin the request arrived on.");

        // And that endpoint answers MCP rather than the app's router: a JSON-RPC envelope back from
        // a JSON-RPC request, not the site's HTML or its 404 page.
        using var request = new HttpRequestMessage(HttpMethod.Post, McpServerFixture.Url("mcp"))
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Accept", "application/json, text/event-stream");

        using var response = await Http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        using (Assert.Scope())
        {
            Assert.IsLessThan(500, (int)response.StatusCode, $"POST /mcp answered {(int)response.StatusCode}.");
            Assert.Contains("\"jsonrpc\"", body, $"POST /mcp did not answer with JSON-RPC: {body[..Math.Min(300, body.Length)]}");
            Assert.Contains("SearchButil", body, "POST /mcp did not list this server's tools.");
        }
    }
}
