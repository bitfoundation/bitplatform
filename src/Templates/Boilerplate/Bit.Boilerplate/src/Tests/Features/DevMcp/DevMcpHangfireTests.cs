using Hangfire;
using System.Text.Json.Nodes;
using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Tests.Features.DevMcp;

[TestClass, TestCategory("IntegrationTest")]
public class DevMcpHangfireTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task HangfireTools_Should_ReportStats_ListRecurringJobs_AndFindAMailJobByRecipient()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var recipient = $"dev-mcp-{Guid.NewGuid():N}@example.com";
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var jobId = jobs.Enqueue<EmailServiceJobsRunner>(runner =>
            runner.SendEmailJob(recipient, "Dev MCP", "Invitation", "<p>body</p>"));

        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        var stats = JsonNode.Parse(await DevMcpTestUtils.CallText(client, "GetHangfireStats", [], TestContext.CancellationToken))!;
        Assert.IsNotNull(stats["enqueued"] ?? stats["scheduled"] ?? stats["processing"] ?? stats["succeeded"]);
        Assert.IsFalse(string.IsNullOrWhiteSpace(stats["jobExpiration"]?.GetValue<string>()));

        var recurringJson = await DevMcpTestUtils.CallText(client, "ListHangfireRecurringJobs", [], TestContext.CancellationToken);
        Assert.IsFalse(string.IsNullOrWhiteSpace(recurringJson), "ListHangfireRecurringJobs must return a JSON array, even if this host has not registered recurring jobs yet.");

        var listed = JsonNode.Parse(await DevMcpTestUtils.CallText(client, "ListHangfireJobs", new()
        {
            ["state"] = "enqueued",
            ["argumentContains"] = recipient
        }, TestContext.CancellationToken))!;
        Assert.IsNotNull(listed["jobs"]);

        var details = JsonNode.Parse(await DevMcpTestUtils.CallText(client, "GetHangfireJob", new() { ["jobId"] = jobId }, TestContext.CancellationToken))!;
        Assert.IsTrue(details["found"]!.GetValue<bool>());
        var arguments = string.Join(" ", details["arguments"]!.AsArray().Select(argument => argument!.ToString()));
        Assert.Contains(recipient, arguments);
        Assert.Contains("Invitation", arguments);
        Assert.Contains("<p>body</p>", arguments);
    }

    [TestMethod]
    public async Task HangfireTools_Should_RefuseUnknownState_AndReportMissingJobs()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        await using var client = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(scope), "dev-mcp", TestContext.CancellationToken);

        var invalid = await DevMcpTestUtils.CallText(client, "ListHangfireJobs", new() { ["state"] = "running" }, TestContext.CancellationToken);
        Assert.Contains("succeeded", invalid, StringComparison.OrdinalIgnoreCase);

        var missing = JsonNode.Parse(await DevMcpTestUtils.CallText(client, "GetHangfireJob", new() { ["jobId"] = "no-such-job" }, TestContext.CancellationToken))!;
        Assert.IsFalse(missing["found"]!.GetValue<bool>());
    }
}
