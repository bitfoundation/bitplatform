using System.Net;
using System.Net.Http.Json;
using Bit.Bswup.Demo.Server.Controllers;
using Bit.Bswup.Tests.Mcp.TestInfra;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// The MCP endpoints and their HTTP mirror are the only routes here that do real work per request -
/// rendering a page, parsing a file a caller pasted in - and anyone with the URL can drive them in
/// a loop. The window is what keeps one agent from being everyone else's outage, and the site's own
/// pages are deliberately outside it.
/// <para>
/// What is asserted below is as much about the SHAPE of the refusal as the fact of it. A rejection
/// that carries no body is re-executed by UseStatusCodePagesWithReExecute through the Blazor app,
/// which turned a throttled GET into the 16 KB /not-found page and a throttled POST - every MCP
/// call - into a 400 from the antiforgery middleware. A client that should back off would instead
/// have concluded its request was malformed.
/// </para>
/// <para>
/// This class hosts its own application: the limiter's state lives in the app, so sharing one with
/// another test class would mean each could push the other over the limit.
/// </para>
/// </summary>
[TestClass]
public class RateLimitTests
{
    private const int PermitLimit = 240;

    private static WebApplicationFactory<McpController> _app = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static async Task StartAsync(TestContext _)
    {
        _app = new QuietApp();
        _client = _app.CreateClient();

        // The limiter partitions on the caller's address; through the test host every request
        // shares one bucket, which is also what happens behind a proxy.
        for (int request = 1; request <= PermitLimit; request++)
        {
            var response = await _client.GetAsync("/api/mcp/GetBswupJsApi?name=version");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"request {request} of the window was rejected");
        }
    }

    [ClassCleanup]
    public static void Stop() => _app.Dispose();

    [TestMethod]
    public async Task AGetPastTheLimit_IsRefusedWith429()
    {
        var response = await _client.GetAsync("/api/mcp/GetBswupJsApi?name=version");

        Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [TestMethod]
    public async Task APostPastTheLimit_IsRefusedWith429RatherThanAnAntiforgeryFailure()
    {
        var mirror = await _client.PostAsJsonAsync("/api/mcp/InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.Clean });

        Assert.AreEqual(HttpStatusCode.TooManyRequests, mirror.StatusCode, await mirror.Content.ReadAsStringAsync());

        var mcp = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method = "tools/list" })
        };

        mcp.Headers.Accept.ParseAdd("application/json, text/event-stream");

        var response = await _client.SendAsync(mcp);

        Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task ARefusalSaysHowLongToWait()
    {
        var response = await _client.GetAsync("/api/mcp/GetBswupJsApi?name=version");

        Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.IsTrue(response.Headers.TryGetValues("Retry-After", out var values), "a throttled client is told nothing about when to come back");
        Assert.IsTrue(int.TryParse(values!.Single(), out var seconds) && seconds is > 0 and <= 60, values!.Single());
    }

    [TestMethod]
    public async Task ARefusalIsASmallMachineReadableBody_NotTheSites404Page()
    {
        var response = await _client.GetAsync("/api/mcp/GetBswupJsApi?name=version");
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.IsTrue(body.Length < 500, $"the refusal came back as {body.Length} characters");
        Assert.IsFalse(body.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase), "the refusal was re-executed through the app");
        StringAssert.Contains(body, "too_many_requests");
        StringAssert.Contains(body, PermitLimit.ToString());
    }

    [TestMethod]
    public async Task TheSitesOwnRoutesAreNotInTheWindow()
    {
        var sitemap = await _client.GetAsync("/sitemap.xml");

        Assert.AreEqual(HttpStatusCode.OK, sitemap.StatusCode);
    }

    private sealed class QuietApp : WebApplicationFactory<McpController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }
}
