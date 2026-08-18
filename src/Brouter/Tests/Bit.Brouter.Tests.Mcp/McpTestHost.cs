using Microsoft.AspNetCore.Mvc.Testing;
using ModelContextProtocol.Client;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The demo server, booted once for the whole test run, and one MCP client connected to it.
/// <para>
/// The app is started from its own <c>Program.cs</c> rather than from a re-registration of the MCP
/// pieces, so what the tests exercise is the wiring that ships: the same <c>AddMcpServer</c>
/// options, the same <c>MapMcp("/mcp")</c>, the same DI container the tools resolve out of. And the
/// tests talk to it through the protocol - initialize, tools/list, tools/call - instead of calling
/// the tool methods, which is the only way to catch a tool that is written correctly but never
/// reaches the wire: a missing attribute, a schema a client rejects, a return value that does not
/// serialize.
/// </para>
/// </summary>
[TestClass]
public static class McpTestHost
{
    private static WebApplicationFactory<Program>? _factory;
    private static McpClient? _client;

    /// <summary>The running app's service provider, for the services a tool resolves per request.</summary>
    public static IServiceProvider Services => (_factory ?? throw NotStarted()).Services;

    /// <summary>The connected MCP client. One session serves every test - the server is read-only.</summary>
    public static McpClient Client => _client ?? throw NotStarted();

    /// <summary>A plain HTTP client against the same app, for the /api/mcp/... endpoints that mirror the tools.</summary>
    public static HttpClient CreateHttpClient() => (_factory ?? throw NotStarted()).CreateClient();

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>();

        // The handshake happens here: a failure to connect fails the whole run loudly, which is the
        // right report for "the MCP server does not come up" - far better than 200 identical
        // per-test failures.
        _client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri("http://localhost/mcp"),
            // Pinned rather than auto-detected: the app maps the streamable HTTP transport, and a
            // silent fallback to the deprecated SSE one would hide exactly that going missing.
            TransportMode = HttpTransportMode.StreamableHttp,
            // Nothing here is server-initiated, and the standalone stream would otherwise hold a
            // request open for the lifetime of the test run.
            EnableStandaloneGetStream = false,
            Name = "bit-brouter-tests"
        }, _factory.CreateClient(), loggerFactory: null, ownsHttpClient: true), cancellationToken: context.CancellationToken);
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (_client is not null) await _client.DisposeAsync();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    private static InvalidOperationException NotStarted() => new("The MCP test host has not been started yet.");
}
