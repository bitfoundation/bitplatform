using Microsoft.AspNetCore.Mvc.Testing;
using ModelContextProtocol.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion.Tests.Mcp.TestInfra;

/// <summary>
/// The demo's own <c>Program.cs</c>, hosted in memory, with a real MCP client connected to it over
/// the HTTP transport.
/// <para>
/// Everything above this point in the test suite calls the tool methods as C#. That leaves the part
/// an agent actually depends on untested: the attributes have to be discovered, the DTOs have to
/// become output schemas, the transport has to be mapped, and every argument has to survive a
/// round trip through JSON. None of that is exercised by a method call, and all of it is decided by
/// the wiring in Program.cs.
/// </para>
/// </summary>
internal sealed class BmotionMcpServerFixture : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    private BmotionMcpServerFixture(WebApplicationFactory<Program> factory, HttpClient httpClient, McpClient client)
    {
        _factory = factory;
        _httpClient = httpClient;
        Client = client;
    }

    /// <summary>An MCP client talking to the server the way a coding agent does.</summary>
    public McpClient Client { get; }

    /// <summary>The same in-memory host over plain HTTP, for the tools that are also GET endpoints.</summary>
    public HttpClient Http => _httpClient;

    public static async Task<BmotionMcpServerFixture> StartAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                // The host's own logging would otherwise write a request line per JSON-RPC message.
                services.AddLogging(logging => logging.ClearProviders());
            }));

        var httpClient = factory.CreateClient();

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
                // The server maps the streamable HTTP transport; saying so skips the client's probe
                // for the older SSE endpoint, which this server does not have.
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            httpClient,
            ownsHttpClient: false);

        McpClient client;

        try
        {
            client = await McpClient.CreateAsync(transport);
        }
        catch
        {
            // Nothing is returned to dispose, so the host and its client would outlive the failure
            // and hold the in-memory server open for the rest of the run.
            httpClient.Dispose();
            await factory.DisposeAsync();

            throw;
        }

        return new BmotionMcpServerFixture(factory, httpClient, client);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();

        _httpClient.Dispose();

        await _factory.DisposeAsync();
    }
}
