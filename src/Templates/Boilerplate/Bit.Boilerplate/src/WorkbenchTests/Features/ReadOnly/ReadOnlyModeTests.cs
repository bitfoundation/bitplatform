namespace Boilerplate.WorkbenchTests.Features.ReadOnly;

/// <summary>
/// Outside dev mode, like in a published demo or the plain run bit CLI starts, nothing can change, not even from this
/// machine, and no agent can connect. The pages, screenshots and checks don't need any of that, so they still work.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ReadOnlyModeTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow("GET", "api/review")]
    [DataRow("GET", "api/review/wait?cursor=0")]
    [DataRow("POST", "api/review/flows")]
    [DataRow("POST", "mcp")]
    public async Task TheAgentsEndpoints_Should_NotExistEvenOnThisMachine(string method, string url)
    {
        var server = await WorkbenchTestsInitializer.ReadOnlyServer;
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(new(method), url);

        Assert.AreEqual(HttpStatusCode.NotFound, (await client.SendAsync(request, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task AReviewerOnThisMachine_Should_ChangeNothing()
    {
        var server = await WorkbenchTestsInitializer.ReadOnlyServer;

        await using var reviewer = await WorkbenchTestReviewer.Open(server);

        Assert.IsFalse(reviewer.Session.CanWrite);
        Assert.IsNull(await reviewer.Session.AddThread("assistant", "", new("body", null, null, 0, 0), reviewer.Name, "Let me in", null));
        Assert.IsNull(reviewer.Session.CreateFlow("A flow"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("workbench/buildings")]
    [DataRow("preview/buildings")]
    [DataRow("flows")]
    public async Task ThePages_Should_Open(string url)
    {
        var server = await WorkbenchTestsInitializer.ReadOnlyServer;
        using var client = server.CreateClient();

        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(url, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task AVisitorsScreenshotAndChecks_Should_StillWork()
    {
        var server = await WorkbenchTestsInitializer.ReadOnlyServer;
        using var client = server.CreateClient();

        using var screenshotRequest = WorkbenchTestServer.Remote(HttpMethod.Get, "api/screenshots/shifts");
        using var screenshot = await client.SendAsync(screenshotRequest, TestContext.CancellationToken);

        var contentType = screenshot.Content.Headers.ContentType;

        Assert.AreEqual(HttpStatusCode.OK, screenshot.StatusCode);
        Assert.IsNotNull(contentType);
        Assert.AreEqual("image/png", contentType.MediaType);

        using var checksRequest = WorkbenchTestServer.Remote(HttpMethod.Get, "api/checks/shifts");
        using var checks = await client.SendAsync(checksRequest, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, checks.StatusCode);
    }
}
