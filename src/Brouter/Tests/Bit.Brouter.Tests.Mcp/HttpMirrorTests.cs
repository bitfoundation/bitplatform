using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The same tools as plain HTTP GETs under <c>/api/mcp/...</c>, which is what makes each of them
/// inspectable from a browser without an MCP client in the way.
/// <para>
/// They are one <c>[ApiController]</c> with the tool methods on it, so the risk is not that a tool
/// misbehaves here - it is that the two surfaces drift: an argument renamed for one, a route
/// convention changed, a tool that answers over the protocol and 404s in a browser.
/// </para>
/// </summary>
[TestClass]
public class HttpMirrorTests
{
    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query)[] calls =
        [
            ("SearchBrouter", "?query=guard&limit=3"),
            ("GetBrouterSetupGuide", "?renderMode=server"),
            ("GetBrouterDocsPage", "?slug=guards"),
            ("GetBrouterGuideSection", "?heading=Async%20guards"),
            ("GetBrouterApi", "?typeName=Broute"),
            ("GetBrouterRouteConstraints", ""),
            ("InspectBrouterRouteTemplates", "?templates=/users/{id:int}"),
            ("GetBrouterSourceFile", "?path=Demo/Client/AppRouter.razor"),
        ];

        CollectionAssert.AreEquivalent(McpToolSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query) in calls)
        {
            var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"GET /api/mcp/{tool} answered {(int)response.StatusCode}.");

            var body = await response.Content.ReadAsStringAsync();

            Assert.IsFalse(string.IsNullOrWhiteSpace(body), $"GET /api/mcp/{tool} answered with an empty body.");
        }

        // The reference tools answer with the index of what there is when their key is left out, and
        // that is a browser's way in: /api/mcp/GetBrouterApi with no query string is the type list.
        foreach (var tool in new[] { "GetBrouterApi", "GetBrouterDocsPage", "GetBrouterGuideSection", "GetBrouterSourceFile" })
        {
            var body = await http.GetStringAsync($"/api/mcp/{tool}");

            StringAssert.Contains(body, "# Bit.Brouter", $"GET /api/mcp/{tool} with no argument did not answer with an index.");
        }
    }

    [TestMethod]
    public async Task The_http_answer_is_the_same_material_as_the_tool_call()
    {
        using var http = McpTestHost.CreateHttpClient();

        var overHttp = await http.GetStringAsync("/api/mcp/GetBrouterGuideSection?heading=Async%20guards");
        var overMcp = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Async guards" });

        Assert.AreEqual(overMcp, overHttp);
    }

    [TestMethod]
    public async Task A_structured_tool_answers_the_browser_with_the_same_json_it_puts_on_the_wire()
    {
        using var http = McpTestHost.CreateHttpClient();

        using var document = JsonDocument.Parse(await http.GetStringAsync("/api/mcp/InspectBrouterRouteTemplates?templates=/users/{id:int}"));

        var route = document.RootElement.GetProperty("routes").EnumerateArray().Single();

        Assert.IsTrue(route.GetProperty("isValid").GetBoolean());
        Assert.AreEqual("users/{id:int}", route.GetProperty("normalizedTemplate").GetString());

        // Shape is the router's comparison key; it is only meaningful next to another template, and
        // is deliberately kept out of a single template's answer.
        Assert.IsFalse(route.TryGetProperty("shape", out _),
            "The inspection carried a collision key with nothing to collide with - and a member with nothing in it " +
            "should not be serialized at all, which is how the protocol answers the same call.");
    }

    [TestMethod]
    public async Task The_mcp_endpoint_and_the_apps_own_pages_share_the_host_without_colliding()
    {
        using var http = McpTestHost.CreateHttpClient();

        // The app's host page is a catch-all ("/{*path}"), so every literal route it shares the host
        // with has to win over it. The protocol arrives by POST, and has to be answered by the MCP
        // endpoint rather than by a prerendered page.
        var ping = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"ping"}""", System.Text.Encoding.UTF8, "application/json")
        };

        // Both are what the streamable HTTP transport requires of a client; a request without them
        // is refused by the endpoint, which is itself proof that the endpoint is the one answering.
        ping.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        ping.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var mcp = await http.SendAsync(ping);

        Assert.AreEqual(HttpStatusCode.OK, mcp.StatusCode);
        Assert.AreNotEqual("text/html", mcp.Content.Headers.ContentType?.MediaType,
            "POST /mcp was answered by the catch-all host page instead of by the MCP endpoint.");
        StringAssert.Contains(await mcp.Content.ReadAsStringAsync(), "\"jsonrpc\":\"2.0\"");

        // While a deep link that nothing else claims still gets the app back, which is what makes
        // every URL a Brouter route rather than a 404.
        var deepLink = await http.GetAsync("/docs/guards");
        Assert.AreEqual(HttpStatusCode.OK, deepLink.StatusCode);
        Assert.AreEqual("text/html", deepLink.Content.Headers.ContentType?.MediaType);
    }
}
