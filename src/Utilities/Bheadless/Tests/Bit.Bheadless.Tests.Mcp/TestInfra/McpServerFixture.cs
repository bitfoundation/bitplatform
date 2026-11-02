using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Bit.Bheadless.Tests.Mcp.TestInfra;

/// <summary>
/// The demo server's own <c>Program.cs</c>, hosted in memory, with a real MCP client connected to it over the
/// streamable HTTP transport - and a plain HttpClient for the GET mirror of every tool.
/// </summary>
internal sealed class McpServerFixture : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    private McpServerFixture(WebApplicationFactory<Program> factory, HttpClient http, McpClient client)
    {
        _factory = factory;
        Http = http;
        Client = client;
    }

    /// <summary>An MCP client talking to the server the way a coding agent does.</summary>
    public McpClient Client { get; }

    /// <summary>The same in-memory host over plain HTTP, for the /api/mcp/... mirror of the tools.</summary>
    public HttpClient Http { get; }

    public static async Task<McpServerFixture> StartAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                // The host's own logging would otherwise write a request line per JSON-RPC message.
                services.AddLogging(logging => logging.ClearProviders());
            }));

        var http = factory.CreateClient();

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, "/mcp"),
                // The server maps the streamable HTTP transport; saying so skips the probe for the older SSE one.
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            http,
            ownsHttpClient: false);

        try
        {
            return new McpServerFixture(factory, http, await McpClient.CreateAsync(transport));
        }
        catch
        {
            // Nothing is returned to dispose, so the host would otherwise outlive the failure.
            http.Dispose();
            await factory.DisposeAsync();

            throw;
        }
    }

    /// <summary>Calls a tool over MCP and returns its text, failing the test when the call reports an error.</summary>
    public async Task<string> CallAsync(string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await Client.CallToolAsync(tool, arguments ?? []);
        var text = string.Join('\n', result.Content.OfType<TextContentBlock>().Select(block => block.Text));

        Assert.IsFalse(result.IsError is true, $"'{tool}' answered with an error: {text}");

        return text;
    }

    /// <summary>Calls the GET mirror of a tool, failing the test on anything but a 200.</summary>
    public async Task<string> GetAsync(string tool, string query = "")
    {
        using var response = await Http.GetAsync($"/api/mcp/{tool}{(query.Length > 0 ? "?" + query : "")}");
        var text = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(response.IsSuccessStatusCode, $"GET {tool} answered {(int)response.StatusCode}: {text}");

        return text;
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        Http.Dispose();
        await _factory.DisposeAsync();
    }
}
