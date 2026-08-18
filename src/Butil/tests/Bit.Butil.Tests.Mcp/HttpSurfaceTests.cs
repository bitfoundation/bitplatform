using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using NUnit.Framework;
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
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class HttpSurfaceTests : McpTestBase
{
    private static HttpClient Http => McpServerFixture.Http;

    [Test]
    public async Task Every_tool_is_also_a_plain_GET_anyone_can_open_in_a_browser()
    {
        // "The same methods are exposed as plain HTTP GET endpoints under /api/mcp/..., which makes
        // each of them inspectable from a browser." Every tool, or the claim is not true.
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SearchButil"] = "?query=clipboard",
            ["GetButilSetupGuide"] = "?hostingModel=wasm",
            ["GetButilApiDetails"] = "?typeName=Clipboard",
            ["InspectButilApi"] = "?name=Clipboard",
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

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"GET /api/mcp/{tool} answered {(int)response.StatusCode}.");
                Assert.That(body, Is.Not.Empty, $"GET /api/mcp/{tool} answered with nothing.");
            });
        }
    }

    [Test]
    public async Task The_GET_mirror_answers_the_same_data_as_the_tool()
    {
        // The mirror is the same method, so a difference here means one of the two paths is doing
        // something the other is not - a filter, a cache, a different origin. Held against the tool
        // itself rather than against a hand-written expectation, which is the only way the two can
        // be shown not to have drifted.
        using var response = await Http.GetAsync(McpServerFixture.Url("api/mcp/GetButilBrowserSupport"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var overHttp = JsonSerializer.Deserialize<Capability[]>(await response.Content.ReadAsStringAsync(), JsonOptions);
        var overMcp = await CallStructuredAsync<Capability[]>("GetButilBrowserSupport");

        Assert.Multiple(() =>
        {
            Assert.That(overHttp, Is.Not.Null.And.Not.Empty);
            Assert.That(overHttp!.Select(capability => capability.Api), Does.Contain("Clipboard"));

            // Re-serialized rather than compared as records: the payload carries arrays, and record
            // equality on those is by reference.
            Assert.That(JsonSerializer.Serialize(overHttp, JsonOptions), Is.EqualTo(JsonSerializer.Serialize(overMcp, JsonOptions)),
                "The GET mirror and the tool answered with different data.");
        });
    }

    [Test]
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

        Assert.Multiple(() =>
        {
            Assert.That((int)response.StatusCode, Is.LessThan(400), $"The preflight was answered with {(int)response.StatusCode}.");
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True, "The preflight carries no Access-Control-Allow-Origin.");
            Assert.That(response.Headers.GetValues("Access-Control-Allow-Origin"), Does.Contain("*"));

            var allowedMethods = string.Join(",", response.Headers.TryGetValues("Access-Control-Allow-Methods", out var methods) ? methods : []);
            Assert.That(allowedMethods, Does.Contain("POST"));
        });
    }

    [Test]
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

        Assert.Multiple(() =>
        {
            // First, or a 500 that happens to carry the headers reads as a pass.
            Assert.That(response.IsSuccessStatusCode, Is.True, $"The cross-origin tools/list was answered with {(int)response.StatusCode}.");
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True, "The response carries no Access-Control-Allow-Origin.");
            Assert.That(exposed, Does.Contain("MCP-Protocol-Version").IgnoreCase);
            Assert.That(exposed, Does.Contain("WWW-Authenticate").IgnoreCase);

            // AllowAnyOrigin and credentials are mutually exclusive by design, and that is the right
            // way round: no cookie of this site's should ride along on a cross-origin tool call.
            Assert.That(response.Headers.Contains("Access-Control-Allow-Credentials"), Is.False);
        });
    }

    [Test]
    public async Task The_api_mirror_is_reachable_from_another_origin_too()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, McpServerFixture.Url("api/mcp/GetButilApiList"));
        request.Headers.Add("Origin", "https://example.test");

        using var response = await Http.SendAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True);
        });
    }

    [Test]
    public async Task The_endpoint_is_anonymous()
    {
        // Everything behind these routes is public read-only documentation. A 401 here would mean
        // an MCP client is being asked to authenticate to read a library's docs.
        using var response = await Http.GetAsync(McpServerFixture.Url("api/mcp/GetButilOverview"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Discovery_files_point_at_the_endpoint()
    {
        using var robots = await Http.GetAsync(McpServerFixture.Url("robots.txt"));
        var robotsBody = await robots.Content.ReadAsStringAsync();

        using var llms = await Http.GetAsync(McpServerFixture.Url("llms.txt"));
        var llmsBody = await llms.Content.ReadAsStringAsync();

        using var sitemap = await Http.GetAsync(McpServerFixture.Url("sitemap.xml"));
        var sitemapBody = await sitemap.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(robots.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(robotsBody, Does.Contain("Sitemap: "));
            Assert.That(robotsBody, Does.Contain("/sitemap.xml"));

            Assert.That(llms.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(llmsBody, Does.StartWith("# Bit.Butil"));
            Assert.That(llmsBody, Does.Contain("/mcp"), "llms.txt is where an assistant learns the MCP endpoint exists.");
            Assert.That(llmsBody, Does.Contain("/api/mcp/"));

            Assert.That(sitemap.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(sitemapBody, Does.StartWith("<?xml"));
            Assert.That(sitemapBody, Does.Contain("<urlset"));
        });
    }

    [Test]
    public async Task The_discovery_files_are_generated_from_the_same_nav_the_tools_use()
    {
        // All three are generated from DocsNav rather than written by hand, so a page added to the
        // nav is a page that appears here. A checked-in copy would silently rot, and the way that
        // shows up is a slug the tools know about and the sitemap does not.
        using var response = await Http.GetAsync(McpServerFixture.Url("api/mcp/GetButilDocsList"));
        var pages = JsonSerializer.Deserialize<DocsPage[]>(await response.Content.ReadAsStringAsync(), McpTestBase.JsonOptions)!;

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

        Assert.Multiple(() =>
        {
            Assert.That(missingFromSitemap.Select(page => page.Slug), Is.Empty);
            Assert.That(missingFromLlms.Select(page => page.Slug), Is.Empty);
        });
    }

    [Test]
    public async Task The_endpoint_the_discovery_files_advertise_is_the_one_that_answers()
    {
        // llms.txt tells an assistant where the server is. A relative move of the route would leave
        // that sentence pointing at a 404 that nothing else in the suite would notice.
        var llms = await (await Http.GetAsync(McpServerFixture.Url("llms.txt"))).Content.ReadAsStringAsync();

        Assert.That(llms, Does.Contain($"{McpServerFixture.BaseUrl}/mcp"),
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

        Assert.Multiple(() =>
        {
            Assert.That((int)response.StatusCode, Is.LessThan(500), $"POST /mcp answered {(int)response.StatusCode}.");
            Assert.That(body, Does.Contain("\"jsonrpc\""), $"POST /mcp did not answer with JSON-RPC: {body[..Math.Min(300, body.Length)]}");
            Assert.That(body, Does.Contain("GetButilOverview"), "POST /mcp did not list this server's tools.");
        });
    }
}
