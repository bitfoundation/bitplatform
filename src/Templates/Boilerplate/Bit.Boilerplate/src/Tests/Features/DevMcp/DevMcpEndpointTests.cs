//+:cnd:noEmit
using ModelContextProtocol.Client;

namespace Boilerplate.Tests.Features.DevMcp;

[TestClass, TestCategory("IntegrationTest")]
public class DevMcpEndpointTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task DevMcp_Should_RejectAnonymousAndNonAdminCallers()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        Assert.AreEqual(HttpStatusCode.Unauthorized,
            await DevMcpTestUtils.ProbeInitialize(server.WebAppServerAddress, "dev-mcp", accessToken: null, TestContext.CancellationToken),
            "/dev-mcp must reject an anonymous caller.");

        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userToken = await DevMcpTestUtils.AccessToken(scope);
        var status = await DevMcpTestUtils.ProbeInitialize(server.WebAppServerAddress, "dev-mcp", userToken, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, status,
            "/dev-mcp is a System feature, so a signed-in non-admin must be refused.");
    }

    [TestMethod]
    public async Task DevMcp_Should_NotAdvertiseChatbotTools_AndChatbotMcpShouldNotAdvertiseDevTools()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        var accessToken = await DevMcpTestUtils.AccessToken(scope);

        await using var devClient = await DevMcpTestUtils.Connect(server, accessToken, "dev-mcp", TestContext.CancellationToken);
        var devTools = (await devClient.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).Select(tool => tool.Name).ToArray();

        Assert.Contains("GetEffectiveConfiguration", devTools);
        Assert.Contains("QueryEntity", devTools);
        Assert.Contains("ListHangfireJobs", devTools);
        Assert.DoesNotContain("GetCurrentDateTime", devTools,
            "/dev-mcp must not leak chatbot tools.");

        //#if (signalR == true)
        await using var chatbotClient = await DevMcpTestUtils.Connect(server, accessToken, "mcp", TestContext.CancellationToken);
        var chatbotTools = (await chatbotClient.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).Select(tool => tool.Name).ToArray();
        Assert.Contains("GetCurrentDateTime", chatbotTools);
        Assert.DoesNotContain("QueryEntity", chatbotTools,
            "/mcp must not leak Dev MCP tools.");
        Assert.DoesNotContain("GetEffectiveConfiguration", chatbotTools);
        //#endif
    }

    [TestMethod]
    public async Task DevMcpTools_Should_WorkWithoutElevatedAccess()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        var accessToken = await DevMcpTestUtils.AccessToken(scope);

        await using var client = await DevMcpTestUtils.Connect(server, accessToken, "dev-mcp", TestContext.CancellationToken);

        var config = await DevMcpTestUtils.CallText(client, "GetEffectiveConfiguration", [], TestContext.CancellationToken);
        Assert.Contains("issuer", config, StringComparison.OrdinalIgnoreCase);

        var query = await DevMcpTestUtils.CallText(client, "QueryEntity", new()
        {
            ["entity"] = "User",
            ["select"] = new[] { "Id", "Email" },
            ["take"] = 1
        }, TestContext.CancellationToken);
        Assert.Contains("rows", query, StringComparison.OrdinalIgnoreCase);

        var stats = await DevMcpTestUtils.CallText(client, "GetHangfireStats", [], TestContext.CancellationToken);
        Assert.Contains("jobExpiration", stats, StringComparison.OrdinalIgnoreCase);

        var listed = await DevMcpTestUtils.CallText(client, "ListHangfireJobs", new() { ["state"] = "any", ["take"] = 1 }, TestContext.CancellationToken);
        Assert.Contains("jobs", listed, StringComparison.OrdinalIgnoreCase);
    }
}
