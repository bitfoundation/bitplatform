using System.Text.Json.Nodes;

namespace Boilerplate.Tests.Features.DevMcp;

[TestClass, TestCategory("IntegrationTest")]
public class DevMcpConfigurationTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task GetDeploymentInfo_Should_ReturnLiveValues_AndNeverSecrets()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);
        var text = await DevMcpTestUtils.CallText(client, "GetDeploymentInfo", [], TestContext.CancellationToken);
        var json = JsonNode.Parse(text)!;

        Assert.AreEqual("Development", json["hosting"]!["environmentName"]!.GetValue<string>());
        // The issuer is the origin the caller reached, which is what every token minted there carries.
        Assert.AreEqual(server.WebAppServerAddress.ToString().TrimEnd('/'), json["identity"]!["issuer"]!.GetValue<string>());
        Assert.IsTrue(json["identity"]!["requireConfirmedAccount"]!.GetValue<bool>());
        Assert.IsFalse(json["backgroundJobs"]!["useIsolatedStorage"]!.GetValue<bool>());

        Assert.IsNull(json["request"], "What this process sees of the inbound call belongs to GetDiagnosticReport; a deployment's configuration is not a per-request thing.");

        var payload = text.ToLowerInvariant();
        // Nothing about the request is in here at all, so neither is anything the request carried.
        Assert.DoesNotContain("authorization", payload);
        Assert.DoesNotContain("bearer ", payload);
        Assert.DoesNotContain("apikey", payload);
        Assert.DoesNotContain("chatapikey", payload);
        Assert.DoesNotContain("privatekey", payload);
        Assert.DoesNotContain("clientsecret", payload);
        Assert.DoesNotContain("twilioautotoken", payload);
        Assert.DoesNotContain("ethereal.email", payload);
        Assert.DoesNotContain("z8gyymezgeuvcdmqru", payload);

        Assert.IsTrue(json["capabilities"]!["smtp"]!.GetValue<bool>());
        Assert.IsFalse(json["capabilities"]!["twilioSms"]!.GetValue<bool>());
    }

    [TestMethod]
    public async Task GetHealth_Should_ReturnPerCheckStatus()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);
        var text = await DevMcpTestUtils.CallText(client, "GetHealth", [], TestContext.CancellationToken);
        var json = JsonNode.Parse(text)!;

        Assert.IsNotNull(json["status"]);
        var checks = json["checks"]!.AsArray();
        Assert.IsGreaterThan(0, checks.Count, "GetHealth must return the same checks /health runs, not an empty list.");
        Assert.Contains(check => check!["name"]!.GetValue<string>() == "AppDbContext", checks,
            "The EF Core DbContext check is registered in AddServerApiHealthChecks.");
    }
}
