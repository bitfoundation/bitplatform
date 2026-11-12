using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.WorkbenchTests.Features.Progress;

/// <summary>
/// The agents' tasks can hold a server's address, a domain or a decision, so only this machine and an invited reviewer see them:
/// their endpoints answer only local requests in dev mode, and anyone else gets the samples, which reach no agent.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ProgressAccessTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow("GET", "api/progress")]
    [DataRow("POST", "api/progress")]
    [DataRow("GET", "api/progress/abc")]
    [DataRow("PATCH", "api/progress/abc")]
    [DataRow("DELETE", "api/progress/abc")]
    [DataRow("GET", "api/progress/abc/wait?cursor=0")]
    [DataRow("GET", "api/workbench")]
    public async Task TheAgentsEndpoints_Should_NotExistForARemoteRequestOrOutsideDevMode(string method, string url)
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = server.CreateClient();

        using var throughTunnel = WorkbenchTestServer.Remote(new(method), url, method is "GET" or "DELETE" ? null : new { title = "From afar" });
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.SendAsync(throughTunnel, TestContext.CancellationToken)).StatusCode);

        var readOnly = await WorkbenchTestsInitializer.ReadOnlyServer;
        using var readOnlyClient = readOnly.CreateClient();

        using var local = new HttpRequestMessage(new(method), url) { Content = method is "GET" or "DELETE" ? null : JsonContent.Create(new { title = "No agent here" }) };
        Assert.AreEqual(HttpStatusCode.NotFound, (await readOnlyClient.SendAsync(local, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task AVisitor_Should_SeeOnlyTheSamplesAndReachNobody()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        var agent = server.Services.GetRequiredService<WorkbenchProgressAgent>();
        var (id, _) = await agent.Create("Claude Code", new(null, "Private deploy", null, null, null, null, null, [new("ip", "Which server?", null, null, null, null)], null), TestContext.CancellationToken);

        await using var visitor = await WorkbenchTestReviewer.Open(server, isLocal: false, name: "Visitor");

        Assert.IsFalse(visitor.Session.CanSeeTasks);
        Assert.IsTrue(visitor.Session.ShowsSampleTasks);
        Assert.IsTrue(visitor.Session.ShownTasks.All(t => t.IsSample));
        Assert.AreEqual(0, visitor.Session.NeedsYouCount);

        var sample = visitor.Session.ShownTasks.Single(t => t.Id == WorkbenchSampleTasks.DeployId);
        var ask = sample.OpenAsks.First(a => a.Kind is WorkbenchProgressAskKind.Choice);

        await visitor.Change(session => Assert.IsTrue(session.Answer(sample, ask.Id, [ask.Options[0]], null, "Visitor")));

        Assert.IsFalse(visitor.Session.ShownTasks.Single(t => t.Id == sample.Id).Asks.Single(a => a.Id == ask.Id).IsOpen, "A sample takes the answer, in this circuit only.");
        Assert.IsNull(await agent.GetTask(sample.Id, TestContext.CancellationToken), "No sample reaches the review.");

        var real = (await agent.GetTask(id!, TestContext.CancellationToken))!;

        await visitor.Change(session =>
        {
            Assert.IsFalse(session.Answer(real, "ip", [], "203.0.113.9", "Visitor"));
            Assert.IsFalse(session.SendMessage(real, "Let me in", "Visitor"));
        });

        Assert.IsTrue((await agent.GetTask(id!, TestContext.CancellationToken))!.Asks.Single().IsOpen);
    }

    [TestMethod]
    public async Task AnInvitedReviewer_Should_AnswerTheRealTasks()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        var agent = server.Services.GetRequiredService<WorkbenchProgressAgent>();
        var (id, _) = await agent.Create("Claude Code", new(null, "Deploy from the phone", null, null, null, null, null, [new("go", "Go ahead?", null, "confirm", null, null)], null), TestContext.CancellationToken);

        await using var invited = await WorkbenchTestReviewer.Open(server, isLocal: false, name: "Sarah on her phone", isInvited: true);

        Assert.IsTrue(invited.Session.CanSeeTasks);
        Assert.IsFalse(invited.Session.CanDelete((await agent.GetTask(id!, TestContext.CancellationToken))!), "Only this machine removes a task.");

        var task = await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(invited.Session.Tasks.FirstOrDefault(t => t.Id == id)), t => t is not null);

        await invited.Change(session => Assert.IsTrue(session.Answer(task!, "go", [WorkbenchProgressDocument.Yes], null, "Sarah")));

        await WorkbenchTestReviewer.WaitFor(() => agent.GetTask(id!, TestContext.CancellationToken), t => t!.Asks.Single().Answer is not null);
    }

    [TestMethod]
    public async Task ASecret_Should_BeTakenOnlyOnThisMachineAndASamplesNowhere()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        var agent = server.Services.GetRequiredService<WorkbenchProgressAgent>();
        var (id, _) = await agent.Create("Claude Code", new(null, "Deploy", null, null, null, null, null, [new("token", "Your Cloudflare token", null, "secret", null, null, Secret: new("file", null, null))], null), TestContext.CancellationToken);

        await using var invited = await WorkbenchTestReviewer.Open(server, isLocal: false, name: "Sarah on her phone", isInvited: true);

        var task = await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(invited.Session.Tasks.FirstOrDefault(t => t.Id == id)), t => t is not null);

        Assert.IsTrue(invited.Session.CanAnswer(task!), "The reviewer sees the question.");
        Assert.IsFalse(invited.Session.CanStoreSecrets(task!), "Only the computer gh runs on in the user's name takes the secret.");
        Assert.IsNotNull(await invited.Change(session => session.StoreSecret(task!, task!.Asks.Single(), "from afar", "Sarah", TestContext.CancellationToken)));
        Assert.IsFalse(File.Exists(WorkbenchSecrets.FilePath(id!, "token")));
        Assert.IsTrue((await agent.GetTask(id!, TestContext.CancellationToken))!.Asks.Single().IsOpen);

        await using var visitor = await WorkbenchTestReviewer.Open(server, isLocal: false, name: "Visitor");

        var sample = visitor.Session.ShownTasks.Single(t => t.Id == WorkbenchSampleTasks.DeployId);
        var sampleSecret = sample.OpenAsks.Single(a => a.Kind is WorkbenchProgressAskKind.Secret);

        Assert.IsNull(await visitor.Change(session => session.StoreSecret(sample, sampleSecret, "a real key by mistake", "Visitor", TestContext.CancellationToken)));
        Assert.IsFalse(visitor.Session.ShownTasks.Single(t => t.Id == sample.Id).Asks.Single(a => a.Id == sampleSecret.Id).IsOpen, "A sample takes it, and keeps nothing of it.");
        Assert.IsFalse(File.Exists(WorkbenchSecrets.FilePath(sample.Id, sampleSecret.Id)));
    }
}
