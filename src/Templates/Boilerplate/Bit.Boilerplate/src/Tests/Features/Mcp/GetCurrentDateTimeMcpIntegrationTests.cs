using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Time.Testing;
using Boilerplate.Tests.Features.Identity;
using Boilerplate.Client.Core.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.Features.Mcp;

[TestClass, TestCategory("IntegrationTest")]
public partial class GetCurrentDateTimeMcpIntegrationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Proves the server's Model Context Protocol endpoint is working by driving it with a real MCP client. The server
    /// maps it via <c>app.MapMcp("/mcp").RequireAuthorization()</c> (See <c>Program.Middlewares</c>) and advertises the
    /// chatbot tools registered by <c>AddMcpServer().WithHttpTransport().WithToolsFromAssembly()</c>. The test connects an
    /// <see cref="McpClient"/> over the Streamable HTTP transport, authenticating with a bearer token, then lists the
    /// tools and invokes <c>GetCurrentDateTime</c> (See <c>AppChatbot.Tools.cs</c>) for the UTC timezone.
    /// <para>
    /// <c>GetCurrentDateTime</c> reads the DI <see cref="TimeProvider"/>, so this test replaces it with a
    /// <see cref="FakeTimeProvider"/> to make the tool's output deterministic - the fake time is not strictly needed for
    /// the endpoint to work, but it lets the test assert on an exact, controlled instant rather than the wall clock. The
    /// fake is seeded from the real current time so every other time-dependent component (e.g. bearer-token lifetime
    /// validation) stays free of clock skew while still returning a frozen, assertable value.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task McpEndpoint_Should_InvokeGetCurrentDateTimeTool()
    {
        // Freeze time at a fixed, known instant (seeded from now, so it does not skew token validation).
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();

            // Even though GetCurrentDateTime works fine with the real clock, fake the TimeProvider so the tool returns
            // an instant we control and can assert on exactly.
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(fakeTimeProvider));
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        // The /mcp endpoint is behind RequireAuthorization(), so sign in with the seeded default account first and reuse
        // the resulting bearer token to authenticate the MCP transport.
        await scope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        var accessToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");
        Assert.IsNotNull(accessToken, "Sign-in should have stored an access token to authenticate the MCP request.");

        // Connect a real MCP client to the server's Streamable HTTP endpoint, carrying the bearer token.
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(server.WebAppServerAddress, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {accessToken}"
            }
        });

        await using var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: TestContext.CancellationToken);

        // The GetCurrentDateTime tool must be advertised by the server (See [McpServerTool] in AppChatbot.Tools.cs).
        var tools = await mcpClient.ListToolsAsync(cancellationToken: TestContext.CancellationToken);
        Assert.IsTrue(tools.Any(t => t.Name == "GetCurrentDateTime"), "The MCP server must expose the GetCurrentDateTime tool.");

        // Invoke it for the UTC timezone; the tool converts the (faked) UtcNow into that timezone and echoes it back as text.
        var result = await mcpClient.CallToolAsync("GetCurrentDateTime",
            new Dictionary<string, object?> { ["timeZoneId"] = "UTC" },
            cancellationToken: TestContext.CancellationToken);

        var text = result.Content.OfType<TextContentBlock>().First().Text;

        Assert.IsTrue(result.IsError is not true, $"The GetCurrentDateTime tool call returned an error. Result: '{text}'.");

        // The tool formats the instant with the round-trip ("o") format. Assert on a second-precision prefix of the faked
        // UTC time so the check is immune to sub-second/offset formatting differences, plus that it names the timezone.
        var expectedUtc = TimeZoneInfo.ConvertTime(fakeTimeProvider.GetUtcNow(), TimeZoneInfo.Utc);
        Assert.IsTrue(text.Contains(expectedUtc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
            $"Tool result did not contain the faked current date/time. Result: '{text}'.");
        Assert.IsTrue(text.Contains("UTC"), $"Tool result did not mention the requested timezone. Result: '{text}'.");
    }
}
