using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bit.Bswup.Demo.Server.Controllers;
using Bit.Bswup.Tests.Mcp.TestInfra;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// The cap on request bodies to the MCP endpoints and their HTTP mirror, and the answer a body past
/// it gets. A refusal that reached the exception handler was logged as a server failure and, on
/// /mcp, came back with no JSON-RPC error and no reason - so what is asserted here is the shape of
/// the refusal as much as the fact of it, and that the cap leaves room for everything a tool reads.
/// <para>
/// This class hosts its own application on Kestrel rather than the in-memory test server: the cap
/// is applied through the server's IHttpMaxRequestBodySizeFeature, which the test server does not
/// implement. There every body is read in full, and each of these tests would pass with no cap at all.
/// </para>
/// </summary>
[TestClass]
public class RequestSizeLimitTests
{
    // One byte past McpMaxRequestBodySize in Program.cs.
    private const int PastTheCap = 1_000_001;

    private static WebApplicationFactory<McpController> _app = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void Start(TestContext _)
    {
        _app = new QuietApp();
        _app.UseKestrel(0);
        _client = _app.CreateClient();
    }

    [ClassCleanup]
    public static void Stop() => _app.Dispose();

    [TestMethod]
    public async Task TheMirror_RefusesABodyPastTheCap_WithProblemDetailsThatSayWhatToDo()
    {
        var response = await _client.PostAsync("/api/mcp/InspectBswupServiceWorker", Json(new { script = ServiceWorkerFixtures.Clean, assetUrls = new string('a', PastTheCap) }));

        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        StringAssert.Contains(problem.GetProperty("detail").GetString(), "Pass the asset URLs in smaller batches");
    }

    [TestMethod]
    public async Task TheMcpTransport_RefusesABodyPastTheCap_WithAJsonRpcErrorThatSaysWhatToDo()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = Json(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new { name = "InspectBswupServiceWorker", arguments = new { script = ServiceWorkerFixtures.Clean, assetUrls = new string('a', PastTheCap) } }
            })
        };

        request.Headers.Accept.ParseAdd("application/json, text/event-stream");

        var response = await _client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, response.StatusCode, await response.Content.ReadAsStringAsync());

        var error = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error");

        Assert.AreEqual(-32600, error.GetProperty("code").GetInt32(), "an invalid request, in JSON-RPC's terms");
        StringAssert.Contains(error.GetProperty("message").GetString(), "Pass the asset URLs in smaller batches");
    }

    [TestMethod]
    public async Task EverythingTheToolReads_FitsUnderTheCap_EvenWithEveryCharacterEscaped()
    {
        // The default JSON encoder writes '<' as < - six bytes a character, the worst case. A
        // script and an asset-URL list each at the tool's own limit still have to arrive whole.
        var body = Json(new { script = new string('<', 64_000), assetUrls = new string('<', 64_000) });

        Assert.IsTrue(body.Headers.ContentLength > 760_000, "the fixture has to be the worst case for this to test anything");

        var response = await _client.PostAsync("/api/mcp/InspectBswupServiceWorker", body);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task AListTheToolCuts_StillGetsTheToolsOwnNote()
    {
        // Far past the tool's 64,000-character cut, still under the cap: the agent hears "ask again
        // with the rest" from the tool, not a transport error.
        var urls = string.Join("\n", Enumerable.Range(0, 30_000).Select(index => $"_framework/asset{index}.dll"));

        Assert.IsTrue(urls.Length > 700_000, "the fixture has to be far past the tool's own cut for this to test anything");

        var response = await _client.PostAsync("/api/mcp/InspectBswupServiceWorker", Json(new { script = ServiceWorkerFixtures.Clean, assetUrls = urls }));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());

        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        var notes = string.Join("\n", report.GetProperty("assets").GetProperty("notes").EnumerateArray().Select(note => note.GetString()));

        StringAssert.Contains(notes, "Ask again with the rest");
    }

    /// <summary>A JSON body with its length known up front, the way most clients send one.</summary>
    private static StringContent Json(object body) => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private sealed class QuietApp : WebApplicationFactory<McpController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }
}
