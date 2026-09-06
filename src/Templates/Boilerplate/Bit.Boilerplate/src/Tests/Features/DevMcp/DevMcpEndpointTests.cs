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

    /// <summary>
    /// The endpoint names two policies, and naming two must mean AND, not OR: a global admin who has not turned 2FA on
    /// is exactly the power user this endpoint is being kept away from, and 2FA alone grants nothing.
    /// </summary>
    [TestMethod]
    public async Task DevMcp_Should_RequireTheFeatureAndTwoFactorTogether()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using (var featureOnlyScope = server.WebApp.Services.CreateAsyncScope())
        {
            var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, featureOnlyScope, TestContext.CancellationToken);
            await using var grant = await TestAccountUtils.MakeGlobalAdmin(server, featureOnlyScope, userId, TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.Forbidden,
                await DevMcpTestUtils.ProbeInitialize(server.WebAppServerAddress, "dev-mcp",
                    await DevMcpTestUtils.AccessToken(featureOnlyScope), TestContext.CancellationToken),
                "A global admin without 2FA holds the feature and must still be refused; two policies on one endpoint are ANDed.");
        }

        await using (var twoFactorOnlyScope = server.WebApp.Services.CreateAsyncScope())
        {
            var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, twoFactorOnlyScope, TestContext.CancellationToken);
            await DevMcpTestUtils.EnableTwoFactorAndSignInWithIt(server, twoFactorOnlyScope, email, userId, TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.Forbidden,
                await DevMcpTestUtils.ProbeInitialize(server.WebAppServerAddress, "dev-mcp",
                    await DevMcpTestUtils.AccessToken(twoFactorOnlyScope), TestContext.CancellationToken),
                "2FA on its own grants nothing; the System feature is still required.");
        }

        await using (var bothScope = server.WebApp.Services.CreateAsyncScope())
        {
            var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, bothScope, TestContext.CancellationToken);
            await using var _ = grant;

            await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(bothScope), "dev-mcp", TestContext.CancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);

            Assert.IsNotEmpty(tools, "Holding both must actually get in, or the two assertions above pass for the wrong reason.");
        }
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

        Assert.Contains("GetDeploymentInfo", devTools);
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
        Assert.DoesNotContain("GetDeploymentInfo", chatbotTools);
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

        var config = await DevMcpTestUtils.CallText(client, "GetDeploymentInfo", [], TestContext.CancellationToken);
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
