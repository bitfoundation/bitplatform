using System.Net.Http.Json;
using System.Text.Json;
using Bit.Bswup.Demo.Server.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Bit.Bswup.Tests.Mcp.TestInfra;

/// <summary>
/// The real <c>Bit.Bswup.Demo.Server</c> application, hosted in-process, with an actual MCP
/// client connected to <c>/mcp</c> over the streamable-HTTP transport.
/// <para>
/// Nothing here is a stand-in: the tests drive the same Program.cs pipeline a deployment runs -
/// its routing, its rate limiter, its MCP transport, its DI container - so a tool that answers in
/// a unit test but throws once the ASP.NET Core plumbing is involved (a scoped HtmlRenderer that
/// cannot resolve, a serializer that cannot describe a DTO) fails here rather than in production.
/// </para>
/// <para>
/// One instance per test class rather than one per test: booting costs about a second, and the
/// server's rate limiter is a fixed window over the whole app, so a class that starts fresh
/// cannot be pushed over the limit by the class that ran before it.
/// </para>
/// </summary>
public sealed class McpTestServer : IAsyncDisposable
{
    private readonly BswupApp _app;
    private readonly HttpClientTransport _transport;

    private McpTestServer(BswupApp app, HttpClient http, HttpClientTransport transport, McpClient mcp)
    {
        _app = app;
        _transport = transport;
        Http = http;
        Mcp = mcp;
    }

    /// <summary>The plain HTTP client, for the <c>/api/mcp/...</c> mirror and the site's own routes.</summary>
    public HttpClient Http { get; }

    /// <summary>A connected MCP client - the same one an agent would use.</summary>
    public McpClient Mcp { get; }

    public static async Task<McpTestServer> StartAsync()
    {
        var app = new BswupApp();
        var http = app.CreateClient();

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            // The tests are request/response only; a standalone GET stream would just be a
            // long-poll hanging off the test host for the lifetime of every class.
            EnableStandaloneGetStream = false
        }, http);

        var mcp = await McpClient.CreateAsync(transport);

        return new McpTestServer(app, http, transport, mcp);
    }

    /// <summary>Calls a tool and returns its text content - what a client puts in front of a model.</summary>
    public async Task<string> CallTextAsync(string tool, object? arguments = null)
    {
        var result = await CallAsync(tool, arguments);

        Assert.IsTrue(result.IsError is not true, $"'{tool}' answered with an error: {TextOf(result)}");

        return TextOf(result);
    }

    public Task<CallToolResult> CallAsync(string tool, object? arguments = null)
    {
        return Mcp.CallToolAsync(tool, ToArguments(arguments)).AsTask();
    }

    /// <summary>The concatenated text blocks of a tool result.</summary>
    public static string TextOf(CallToolResult result)
    {
        return string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));
    }

    /// <summary>The structured (JSON) payload of a tool result, which is what a typed client reads.</summary>
    public static JsonElement StructuredOf(CallToolResult result, string tool)
    {
        Assert.IsNotNull(result.StructuredContent, $"'{tool}' declares structured content but returned none.");

        return result.StructuredContent.Value;
    }

    /// <summary>Reads a resource and returns its text, the way a client attaching it to a conversation would.</summary>
    public async Task<string> ReadResourceTextAsync(string uri)
    {
        var result = await Mcp.ReadResourceAsync(uri);

        Assert.IsTrue(result.Contents.Count > 0, $"'{uri}' returned no contents.");

        return string.Join("\n", result.Contents.OfType<TextResourceContents>().Select(content => content.Text));
    }

    /// <summary>A GET against the plain HTTP mirror of a tool, parsed as JSON.</summary>
    public async Task<JsonElement> GetJsonAsync(string path)
    {
        var response = await Http.GetAsync(path);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode,
            $"GET {path} answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>A POST against the plain HTTP mirror of a tool, parsed as JSON.</summary>
    public async Task<JsonElement> PostJsonAsync(string path, object body)
    {
        var response = await Http.PostAsJsonAsync(path, body);

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode,
            $"POST {path} answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Dictionary<string, object?> ToArguments(object? arguments)
    {
        if (arguments is null) return [];
        if (arguments is Dictionary<string, object?> map) return map;

        return arguments.GetType()
                        .GetProperties()
                        .ToDictionary(property => property.Name, property => property.GetValue(arguments));
    }

    public async ValueTask DisposeAsync()
    {
        await Mcp.DisposeAsync();
        await _transport.DisposeAsync();
        _app.Dispose();
    }

    /// <summary>
    /// The application under test. Only the log level is overridden - every service, endpoint and
    /// middleware is the one Program.cs registers, or these tests would be checking a different
    /// server than the one that ships.
    /// </summary>
    private sealed class BswupApp : WebApplicationFactory<McpController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Warning, not None: a docs page that fails to render logs an error and answers with a
            // placeholder, and that error is the only place the reason is written down.
            builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        }
    }
}
