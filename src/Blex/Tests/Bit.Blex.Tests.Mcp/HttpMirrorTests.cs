using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// The same tools as plain HTTP GETs under <c>/api/mcp/...</c>, which is what makes each of them
/// inspectable from a browser. They are one <c>[ApiController]</c> with the tool methods on it, so the
/// risk is that the two surfaces drift: an argument renamed for one, a tool that answers over the
/// protocol and 404s in a browser.
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
            ("SearchBlex", "?query=effect&limit=3"),
            ("GetBlexSetupGuide", "?hostingModel=server"),
            ("GetBlexDocsPage", "?slug=effects"),
            ("GetBlexGuideSection", "?heading=Persistence"),
            ("GetBlexApi", "?typeName=BlexOptions"),
            ("GetBlexDiagnostics", "?code=BLEX001"),
            ("InspectBlexStore", $"?source={Uri.EscapeDataString("[BlexStore] public partial class S { [BlexState] private int _n; }")}"),
            ("GetBlexSourceFile", "?path=Demo/Client/Stores/CartStore.cs"),
        ];

        CollectionAssert.AreEquivalent(McpToolSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query) in calls)
        {
            var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"GET /api/mcp/{tool} answered {(int)response.StatusCode}.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(await response.Content.ReadAsStringAsync()), $"GET /api/mcp/{tool} answered with an empty body.");
        }

        // The reference tools answer with the index of what there is when their key is left out, and that
        // is a browser's way in: /api/mcp/GetBlexApi with no query string is the type list.
        foreach (var (tool, _) in McpToolSurfaceTests.KeyedTools)
        {
            var body = await http.GetStringAsync($"/api/mcp/{tool}");

            StringAssert.Contains(body, "# Bit.Blex", $"GET /api/mcp/{tool} with no argument did not answer with an index.");
        }
    }

    [TestMethod]
    public async Task The_http_answer_is_the_same_material_as_the_tool_call()
    {
        using var http = McpTestHost.CreateHttpClient();

        var overHttp = await http.GetStringAsync("/api/mcp/GetBlexGuideSection?heading=Persistence");
        var overMcp = await McpCall.TextAsync("GetBlexGuideSection", new() { ["heading"] = "Persistence" });

        Assert.AreEqual(overMcp, overHttp);
    }

    [TestMethod]
    public async Task A_structured_tool_answers_the_browser_with_the_same_json_it_puts_on_the_wire()
    {
        using var http = McpTestHost.CreateHttpClient();

        var source = Uri.EscapeDataString("[BlexStore] public partial class S { [BlexState] private int _n; }");

        using var document = JsonDocument.Parse(await http.GetStringAsync($"/api/mcp/InspectBlexStore?source={source}"));

        Assert.IsTrue(document.RootElement.GetProperty("isValid").GetBoolean());
        Assert.AreEqual("N", document.RootElement.GetProperty("stores")[0].GetProperty("members")[0].GetProperty("name").GetString());

        // A member with nothing in it is not serialized at all, which is how the protocol answers the same call.
        Assert.IsFalse(document.RootElement.TryGetProperty("generatedCode", out _),
            "The generated code was not asked for, and a null member should not be serialized.");
    }

    [TestMethod]
    public async Task The_mcp_endpoint_and_the_apps_own_pages_share_the_host_without_colliding()
    {
        using var http = McpTestHost.CreateHttpClient();

        var ping = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"ping"}""", System.Text.Encoding.UTF8, "application/json")
        };

        ping.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        ping.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var mcp = await http.SendAsync(ping);

        Assert.AreEqual(HttpStatusCode.OK, mcp.StatusCode);
        Assert.AreNotEqual("text/html", mcp.Content.Headers.ContentType?.MediaType, "POST /mcp was answered by a page instead of by the MCP endpoint.");
        StringAssert.Contains(await mcp.Content.ReadAsStringAsync(), "\"jsonrpc\":\"2.0\"");

        // While a docs page is still the prerendered app.
        var page = await http.GetAsync("/effects");
        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode);
        Assert.AreEqual("text/html", page.Content.Headers.ContentType?.MediaType);
    }
}
