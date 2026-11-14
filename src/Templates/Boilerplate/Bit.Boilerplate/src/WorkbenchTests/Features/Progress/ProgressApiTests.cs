using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.WorkbenchTests.Features.Progress;

/// <summary>
/// The Progress page's HTTP endpoints, the way an agent uses them: it starts a task and changes it over HTTP, and the user
/// answers from a session through the relay, as from a browser. Each test keeps a task of its own, as each chat does.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ProgressApiTests
{
    public TestContext TestContext { get; set; } = default!;

    private WorkbenchTestServer server = default!;
    private HttpClient client = default!;
    private WorkbenchTestReviewer user = default!;

    [TestInitialize]
    public async Task Initialize()
    {
        server = await WorkbenchTestsInitializer.SharedServer;
        client = server.CreateClient();
        user = await WorkbenchTestReviewer.Open(server, name: "Sarah");
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        client.Dispose();
        await user.DisposeAsync();
    }

    private async Task<string> Start(object task)
    {
        var response = await client.PostAsJsonAsync("api/progress", task, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        var created = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken))!;
        var id = (string)created["id"]!;

        Assert.AreEqual($"/progress/{id}", (string?)created["page"]);

        return id;
    }

    private async Task<JsonNode> Read(string id) => JsonNode.Parse(await client.GetStringAsync($"api/progress/{id}", TestContext.CancellationToken))!;

    /// <summary>The user's copy of the task, once the relay has brought it.</summary>
    private async Task<WorkbenchProgressTask> Seen(string id, Func<WorkbenchProgressTask, bool>? condition = null)
    {
        return (await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(user.Session.Tasks.FirstOrDefault(t => t.Id == id)), t => t is not null && (condition?.Invoke(t) ?? true)))!;
    }

    [TestMethod]
    public async Task ATask_Should_ReachTheUserAsTheAgentSentIt()
    {
        var id = await Start(new
        {
            agent = "Claude Code",
            title = "Deploy to Test",
            steps = new object[] { new { id = "vm", text = "Get a server", owner = "you", status = "doing" }, new { id = "docker", text = "Install Docker" } },
            asks = new object[] { new { id = "region", question = "Which region?", kind = "choice", options = new[] { "Europe", "US" } } }
        });

        var task = await Seen(id);

        Assert.AreEqual(("Deploy to Test", "Claude Code", 2), (task.Title, task.Agent, task.NeedsYouCount));
        Assert.IsFalse(task.IsSample);
        Assert.IsTrue(user.Session.CanSeeTasks);
        Assert.IsFalse(user.Session.ShowsSampleTasks, "A real task takes the samples' place.");

        var read = await Read(id);

        Assert.AreEqual("working", (string?)read["status"]);
        Assert.AreEqual("you", (string?)read["steps"]![0]!["owner"]);
        Assert.AreEqual("open", (string?)read["asks"]![0]!["status"]);
    }

    [TestMethod]
    public async Task AnUpdate_Should_ChangeOnlyWhatItSends()
    {
        var id = await Start(new { title = "Feature", steps = new object[] { new { id = "a", text = "First", status = "doing" }, new { id = "b", text = "Second" } } });

        var response = await client.PatchAsJsonAsync($"api/progress/{id}", new { agent = "Codex", steps = new object[] { new { id = "a", status = "done" } }, log = "First is done" }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var task = await Seen(id, t => t.CompletedCount == 1);

        Assert.AreEqual(("Feature", "Codex", 2), (task.Title, task.Agent, task.Steps.Length));
        Assert.AreEqual("First is done", task.Feed.Last().Text);
    }

    [TestMethod]
    public async Task AReportAndAMarkedLine_Should_ReachTheUserAndReadBack()
    {
        var id = await Start(new { title = "Test the platform" });

        var response = await client.PatchAsJsonAsync($"api/progress/{id}", new { log = "CI is green", logKind = "success", report = "| Stage | Result |\n| --- | --- |\n| CI | Green |" }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var task = await Seen(id, t => t.Report is not null);

        Assert.AreEqual(WorkbenchProgressNoteKind.Success, task.Feed[^1].Tone);
        Assert.StartsWith("| Stage | Result |", (string)(await Read(id))["report"]!);
    }

    [TestMethod]
    public async Task AnUpdateThatDoesNotFitOrATaskThatIsGone_Should_BeRefused()
    {
        var id = await Start(new { title = "Task" });

        var unsafeLink = await client.PatchAsJsonAsync($"api/progress/{id}", new { links = new[] { new { label = "Run me", url = "javascript:alert(1)" } } }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, unsafeLink.StatusCode);
        Assert.Contains("http(s)", await unsafeLink.Content.ReadAsStringAsync(TestContext.CancellationToken));

        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/progress", new { summary = "No title" }, TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync("api/progress/missing", new { log = "Hi" }, TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("api/progress/missing/wait?cursor=0", TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task Wait_Should_AnswerAsSoonAsTheUserAnswers()
    {
        var id = await Start(new { agent = "Claude Code", title = "Ask", asks = new object[] { new { id = "domain", question = "Which domain?" } } });
        var task = await Seen(id);
        var stopwatch = Stopwatch.StartNew();

        var wait = client.GetStringAsync($"api/progress/{id}/wait?cursor={(long)(await Read(id))["cursor"]!}&agent=Claude%20Code", TestContext.CancellationToken);

        await user.Change(session => Assert.IsTrue(session.Answer(task, "domain", [], "test.example.com", "Sarah")));

        var answer = JsonNode.Parse(await wait.WaitAsync(TimeSpan.FromSeconds(20), TestContext.CancellationToken))!;
        var answered = answer["events"]!.AsArray().Single()!;

        Assert.AreEqual(("answer", "domain", "test.example.com", "Sarah"), ((string?)answered["kind"], (string?)answered["ask"], (string?)answered["text"], (string?)answered["by"]));
        Assert.IsLessThan(TimeSpan.FromSeconds(20), stopwatch.Elapsed, "The wait answers when the user does, not when it times out.");
        Assert.AreEqual("answered", (string?)(await Read(id))["asks"]![0]!["status"]);
    }

    [TestMethod]
    public async Task ASecret_Should_GoWhereTheQuestionSaysAndOnlyItsPlaceReachTheAgent()
    {
        const string secret = "cf_setup_token_4f9a";

        var id = await Start(new { agent = "Claude Code", title = "Deploy", asks = new object[] { new { id = "token", question = "Your Cloudflare token", secret = new { to = "file" } } } });
        var task = await Seen(id);
        var file = WorkbenchSecrets.FilePath(id, "token");

        Assert.AreEqual(("secret", file), ((string?)(await Read(id))["asks"]![0]!["kind"], (string?)(await Read(id))["asks"]![0]!["secret"]!["file"]));
        Assert.IsTrue(user.Session.CanStoreSecrets(task));

        var wait = client.GetStringAsync($"api/progress/{id}/wait?cursor={(long)(await Read(id))["cursor"]!}&agent=Claude%20Code", TestContext.CancellationToken);

        Assert.IsNull(await user.Change(session => session.StoreSecret(task, task.Asks.Single(), $"  {secret} ", "Sarah", TestContext.CancellationToken)));

        var stored = JsonNode.Parse(await wait.WaitAsync(TimeSpan.FromSeconds(20), TestContext.CancellationToken))!["events"]!.AsArray().Single()!;

        Assert.AreEqual(("answer", "token", null, file), ((string?)stored["kind"], (string?)stored["ask"], (string?)stored["text"], (string?)stored["secret"]!["file"]));
        Assert.AreEqual(secret, await File.ReadAllTextAsync(file, TestContext.CancellationToken), "Stored without the spaces a paste brings along.");
        Assert.DoesNotContain(secret, (await Read(id)).ToJsonString(), "The agent reads where it went, never what it is.");
        Assert.DoesNotContain(secret, (await Seen(id)).Feed.Last().Text);

        await client.PatchAsJsonAsync($"api/progress/{id}", new { status = "done" }, TestContext.CancellationToken);

        Assert.IsFalse(File.Exists(file), "Its file goes once the task is over.");
    }

    [TestMethod]
    public async Task Wait_Should_BringTheUsersStepsAndMessagesButNotTheAgentsOwnChanges()
    {
        var id = await Start(new { agent = "Claude Code", title = "Steps", steps = new object[] { new { id = "token", text = "Create a token", owner = "you", status = "doing" } } });
        var task = await Seen(id);
        var cursor = (long)(await Read(id))["cursor"]!;
        var agent = server.Services.GetRequiredService<WorkbenchProgressAgent>();

        await client.PatchAsJsonAsync($"api/progress/{id}", new { log = "Still here" }, TestContext.CancellationToken);

        var quiet = await agent.Wait("Claude Code", id, cursor, TimeSpan.FromMilliseconds(400), TestContext.CancellationToken);

        Assert.IsEmpty(quiet!.Value.Events, "The agent's own update wakes nobody.");

        await user.Change(session =>
        {
            Assert.IsTrue(session.CompleteStep(task, "token", "Sarah"));
            Assert.IsTrue(session.SendMessage(task, "Done, it's in the file.", "Sarah"));
        });

        var woken = await WorkbenchTestReviewer.WaitFor(() => agent.Wait("Claude Code", id, cursor, TimeSpan.FromSeconds(5), TestContext.CancellationToken), r => r?.Events.Count == 2);

        Assert.AreEqual((WorkbenchProgressEntryKind.Step, "token"), (woken!.Value.Events[0].Kind, woken.Value.Events[0].RefId));
        Assert.AreEqual((WorkbenchProgressEntryKind.Message, "Done, it's in the file."), (woken.Value.Events[1].Kind, woken.Value.Events[1].Text));
        Assert.IsGreaterThan(cursor, woken.Value.Cursor);
    }

    [TestMethod]
    public async Task TheList_Should_LetAChatFindItsOwnTaskAmongOthers()
    {
        var mine = await Start(new { agent = "Claude Code", title = "Deploy" });
        var theirs = await Start(new { agent = "Codex", title = "Loyalty points", asks = new object[] { new { id = "q", question = "Worth?" } } });

        var list = JsonNode.Parse(await client.GetStringAsync("api/progress", TestContext.CancellationToken))!.AsArray();

        Assert.AreEqual("Claude Code", (string?)list.Single(t => (string?)t!["id"] == mine)!["agent"]);
        Assert.AreEqual(1, (int)list.Single(t => (string?)t!["id"] == theirs)!["needsYou"]!);
    }

    [TestMethod]
    public async Task DeletingATask_Should_TakeItAwayFromTheUser()
    {
        var id = await Start(new { title = "Short-lived" });

        await Seen(id);

        Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync($"api/progress/{id}", TestContext.CancellationToken)).StatusCode);

        await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(user.Session.Tasks), tasks => tasks.All(t => t.Id != id));

        Assert.AreEqual(HttpStatusCode.NotFound, (await client.DeleteAsync($"api/progress/{id}", TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task TheWorkbench_Should_SayWhichCopyOfTheProjectItRunsFrom()
    {
        var info = JsonNode.Parse(await client.GetStringAsync("api/workbench", TestContext.CancellationToken))!;

        Assert.AreEqual(Environment.ProcessId, (int)info["processId"]!);
        Assert.AreEqual(WorkbenchFolders.Reviews, (string?)info["data"]);
    }
}
