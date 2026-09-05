using System.Text.Json.Nodes;

namespace Boilerplate.Tests.Features.DevMcp;

[TestClass, TestCategory("IntegrationTest")]
public class DevMcpSchemaTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task GetDatabaseSchema_Should_DescribeUser_IncludingQueryFiltersAndKeys()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        var text = await DevMcpTestUtils.CallText(client, "GetDatabaseSchema", new() { ["entityName"] = "User" }, TestContext.CancellationToken);
        var json = JsonNode.Parse(text)!;
        var user = json["entities"]![0]!;

        Assert.AreEqual("User", user["entity"]!.GetValue<string>());
        Assert.AreEqual("Users", user["table"]!.GetValue<string>());
        Assert.Contains(property => property!["name"]!.GetValue<string>() == "Email", user["properties"]!.AsArray());
        Assert.IsNotNull(user["queryFilters"], "The filters QueryEntity bypasses are what this field is for.");
    }

    [TestMethod]
    public async Task GetAppliedMigrations_Should_ListTheLatestMigration()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        var text = await DevMcpTestUtils.CallText(client, "GetAppliedMigrations", [], TestContext.CancellationToken);
        var json = JsonNode.Parse(text)!;

        Assert.IsNotNull(json["applied"]);
        Assert.IsNotNull(json["pending"]);
        Assert.IsTrue(json["canConnect"]!.GetValue<bool>());
    }

    [TestMethod]
    public async Task GetDatabaseSchema_Should_RejectUnknownEntities_AndMarkHangfireStorage()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        var unknown = JsonNode.Parse(await DevMcpTestUtils.CallText(client, "GetDatabaseSchema", new() { ["entityName"] = "NotAnEntity" }, TestContext.CancellationToken))!;
        Assert.Contains("Unknown entity", unknown["error"]!.GetValue<string>());

        var all = JsonNode.Parse(await DevMcpTestUtils.CallText(client, "GetDatabaseSchema", new() { ["entityName"] = (string?)null }, TestContext.CancellationToken))!;
        Assert.Contains(entity => entity!["hangfireStorage"]?.GetValue<bool>() is true, all["entities"]!.AsArray(),
            "Hangfire's jobs schema must be listed so the assistant uses the Hangfire tools instead of QueryEntity.");
    }
}
