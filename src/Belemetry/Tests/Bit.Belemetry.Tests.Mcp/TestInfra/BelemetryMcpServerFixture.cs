using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting.Server;
using ModelContextProtocol.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Belemetry.Tests.Mcp.TestInfra;

/// <summary>
/// The demo's own <c>Program.cs</c>, hosted in memory, with a real MCP client connected to it over the HTTP
/// transport, and the plain HTTP client for the GET mirrors and the OTLP receiver.
/// <para>
/// The method-level tests call the tools as C#. That leaves the part an agent depends on untested: the
/// attributes have to be discovered, the DTOs have to become schemas, the transport has to be mapped, and every
/// argument has to survive a round trip through JSON - all decided by the wiring in Program.cs.
/// </para>
/// </summary>
internal sealed class BelemetryMcpServerFixture : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    private BelemetryMcpServerFixture(WebApplicationFactory<Program> factory, HttpClient http, McpClient client)
    {
        _factory = factory;
        Http = http;
        Client = client;
    }

    /// <summary>An MCP client talking to the server the way a coding agent does.</summary>
    public McpClient Client { get; }

    /// <summary>The same in-memory host over plain HTTP.</summary>
    public HttpClient Http { get; }

    /// <summary>The host's services, e.g. the <see cref="TelemetryStore"/> the receiver files into.</summary>
    public IServiceProvider Services => _factory.Services;

    public static async Task<BelemetryMcpServerFixture> StartAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                // The host's own console logging would otherwise write a line per JSON-RPC message.
                services.AddLogging(logging => logging.ClearProviders());

                // The demo's server pipeline exports to its own receiver over loopback. In memory there is no
                // socket to reach, so its HTTP traffic is routed into the test server itself - which also makes
                // the server-side half of the live viewer testable end to end.
                services.AddSingleton(sp => new BelemetryHttpMessageHandlerProvider(((TestServer)sp.GetRequiredService<IServer>()).CreateHandler()));
            }));

        var http = factory.CreateClient();

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, "/mcp"),
                // The server maps the streamable HTTP transport; saying so skips the probe for the older SSE endpoint.
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            http,
            ownsHttpClient: false);

        McpClient client;

        try
        {
            client = await McpClient.CreateAsync(transport);
        }
        catch
        {
            http.Dispose();
            await factory.DisposeAsync();
            throw;
        }

        return new BelemetryMcpServerFixture(factory, http, client);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        Http.Dispose();
        await _factory.DisposeAsync();
    }
}
