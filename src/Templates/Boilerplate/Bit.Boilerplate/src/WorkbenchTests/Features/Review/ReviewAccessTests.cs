using System.Net.WebSockets;

namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// Only this machine can talk to the agent: the agent's endpoints answer only local requests, a review opened from
/// anywhere else is read-only, and the document's relay takes only the Workbench's own connections.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ReviewAccessTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow("GET", "api/review")]
    [DataRow("GET", "api/review/wait?cursor=0")]
    [DataRow("POST", "api/review/abc/reply")]
    [DataRow("POST", "api/review/abc/status")]
    [DataRow("PUT", "api/review/abc/tasks")]
    [DataRow("GET", "api/review/abc/screenshot")]
    [DataRow("POST", "api/review/pages/theme/status")]
    [DataRow("POST", "api/review/flows")]
    [DataRow("POST", "mcp")]
    [DataRow("GET", "mcp")]
    public async Task TheAgentsEndpoints_Should_NotExistForARemoteRequest(string method, string url)
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = server.CreateClient();

        using var throughTunnel = WorkbenchTestServer.Remote(new(method), url, method is "GET" ? null : new { text = "hi", status = "done" });
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.SendAsync(throughTunnel, TestContext.CancellationToken)).StatusCode);

        using var publicHost = new HttpRequestMessage(new(method), url);
        publicHost.Headers.Host = "workbench.example.com";
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.SendAsync(publicHost, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("workbench/buildings")]
    [DataRow("preview/buildings")]
    [DataRow("flows")]
    public async Task ThePages_Should_StillOpenForARemoteRequest(string url)
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = server.CreateClient();

        using var request = WorkbenchTestServer.Remote(HttpMethod.Get, url);

        Assert.AreEqual(HttpStatusCode.OK, (await client.SendAsync(request, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task ARemoteReviewer_Should_ReadTheReviewButChangeNothing()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;

        await using var local = await WorkbenchTestReviewer.Open(server);
        await using var remote = await WorkbenchTestReviewer.Open(server, isLocal: false, name: "Visitor");

        Assert.IsTrue(local.Session.CanWrite);
        Assert.IsFalse(remote.Session.CanWrite);

        var id = await local.Comment(server, "assistant", "Seen from afar");

        var seen = await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(remote.Session.Threads.FirstOrDefault(t => t.Id == id)), t => t is not null);

        Assert.AreEqual("Seen from afar", seen!.Messages[0].Text);

        Assert.IsNull(await remote.Session.AddThread("assistant", "", new("body", null, null, 0, 0), "Visitor", "Let me in", null));
        Assert.IsNull(remote.Session.CreateFlow("Visitor's flow"));

        remote.Session.Reply(id, "Visitor", "Me too");
        remote.Session.SetStatus(id, WorkbenchReviewStatus.Done);
        remote.Session.SetPageStatus("assistant", WorkbenchPageStatus.Built, "Visitor");
        remote.Session.Delete(id);

        local.Session.Reply(id, "Reviewer", "A marker after the visitor's attempts");

        var thread = await WorkbenchTestReviewer.WaitFor(() => server.Agent.GetThreads(TestContext.CancellationToken), threads => threads.Single(t => t.Id == id).Messages.Length == 2);
        var result = thread.Single(t => t.Id == id);

        Assert.AreEqual(WorkbenchReviewStatus.Open, result.Status);
        Assert.DoesNotContain(m => m.Author == "Visitor", result.Messages);

        var pages = await server.Agent.GetPages(TestContext.CancellationToken);

        Assert.AreNotEqual<WorkbenchPageStatus?>(WorkbenchPageStatus.Built, pages.TryGetValue("assistant", out var page) ? page.Status : null);
    }

    [TestMethod]
    public async Task TheRelay_Should_RefuseAConnectionWithoutTheWorkbenchsSecret()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        var address = new UriBuilder(server.Address) { Scheme = "ws", Path = $"ws/review/{WorkbenchReviewDocument.Id}" }.Uri;

        using (var stranger = new ClientWebSocket())
        {
            await Assert.ThrowsAsync<WebSocketException>(() => stranger.ConnectAsync(address, TestContext.CancellationToken));
        }

        using (var guesser = new ClientWebSocket())
        {
            guesser.Options.SetRequestHeader(WorkbenchReviewRelay.SecretHeader, new string('0', 64));
            await Assert.ThrowsAsync<WebSocketException>(() => guesser.ConnectAsync(address, TestContext.CancellationToken));
        }

        using (var otherDocument = new ClientWebSocket())
        {
            otherDocument.Options.SetRequestHeader(WorkbenchReviewRelay.SecretHeader, WorkbenchReviewRelay.Secret);
            await Assert.ThrowsAsync<WebSocketException>(() => otherDocument.ConnectAsync(new UriBuilder(address) { Path = "ws/review/another-document" }.Uri, TestContext.CancellationToken));
        }

        using var workbench = new ClientWebSocket();

        workbench.Options.SetRequestHeader(WorkbenchReviewRelay.SecretHeader, WorkbenchReviewRelay.Secret);
        await workbench.ConnectAsync(address, TestContext.CancellationToken);

        Assert.AreEqual(WebSocketState.Open, workbench.State);
    }
}
