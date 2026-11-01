namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// The review's HTTP endpoints, the way an agent without MCP uses them: comments come from a reviewer's session through
/// the relay, as they do from a browser, and the agent answers over HTTP. Not in parallel: the agent last seen on the
/// review is one for the whole document.
/// </summary>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public class ReviewApiTests
{
    public TestContext TestContext { get; set; } = default!;

    private WorkbenchTestServer server = default!;
    private HttpClient client = default!;
    private WorkbenchTestReviewer reviewer = default!;

    [TestInitialize]
    public async Task Initialize()
    {
        server = await WorkbenchTestsInitializer.SharedServer;
        client = server.CreateClient();
        reviewer = await WorkbenchTestReviewer.Open(server, name: "Sarah");
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        client.Dispose();
        await reviewer.DisposeAsync();
    }

    private async Task<JsonNode> GetReview(bool includeDone = false)
    {
        return JsonNode.Parse(await client.GetStringAsync(includeDone ? "api/review?includeDone=true" : "api/review", TestContext.CancellationToken))!;
    }

    private static JsonNode? CommentOf(JsonNode review, string id) => review["comments"]!.AsArray().FirstOrDefault(c => (string?)c!["id"] == id);

    [TestMethod]
    public async Task TheReview_Should_DescribeEachCommentsElementViewAndLinks()
    {
        var id = await reviewer.Comment(server, "buildings", "Make the header sticky", query: "?theme=dark&device=ipad-mini");

        var review = await GetReview();
        var comment = CommentOf(review, id)!;

        Assert.IsNotNull(comment);
        Assert.AreEqual("buildings", (string?)comment["page"]);
        Assert.AreEqual("open", (string?)comment["status"]);
        Assert.AreEqual("BitButton", (string?)comment["element"]!["component"]);
        Assert.AreEqual("Save", (string?)comment["element"]!["text"]);
        Assert.AreEqual("main button", (string?)comment["element"]!["selector"]);

        var view = comment["view"]!;

        Assert.AreEqual("iPad mini", (string?)view["device"]);
        Assert.AreEqual((744, 1133), ((int)view["width"]!, (int)view["height"]!));
        Assert.AreEqual("dark", (string?)view["theme"]);
        Assert.AreEqual(CultureInfoManager.DefaultCulture.Name, (string?)view["culture"]);
        Assert.AreEqual("live", (string?)view["data"]);
        Assert.AreEqual("signedin", (string?)view["signIn"]);
        Assert.AreEqual(WorkbenchDemoUsers.All[0].Email, (string?)view["user"]);

        Assert.AreEqual("/workbench/buildings?theme=dark&device=ipad-mini", (string?)comment["links"]!["workbench"]);
        Assert.AreEqual($"/api/review/{id}/screenshot", (string?)comment["links"]!["screenshot"]);

        var message = comment["messages"]!.AsArray().Single()!;

        Assert.AreEqual(("reviewer", "Sarah", "Make the header sticky"), ((string?)message["from"], (string?)message["author"], (string?)message["text"]));
        var sequence = (await server.Agent.GetThreads(TestContext.CancellationToken)).Single(t => t.Id == id).Sequence;

        Assert.IsGreaterThanOrEqualTo(sequence, (long)review["cursor"]!);
    }

    [TestMethod]
    public async Task ACommentThatIsDone_Should_OnlyBeListedWhenAskedFor()
    {
        var open = await reviewer.Comment(server, "shifts", "Open one");
        var done = await reviewer.Comment(server, "shifts", "Done one");

        reviewer.Session.SetStatus(done, WorkbenchReviewStatus.Done);

        await WorkbenchTestReviewer.WaitFor(() => GetReview(includeDone: true), review => (string?)CommentOf(review, done)?["status"] == "done");

        var review = await GetReview();

        Assert.IsNotNull(CommentOf(review, open));
        Assert.IsNull(CommentOf(review, done));
        Assert.IsNotNull(CommentOf(await GetReview(includeDone: true), done));
    }

    [TestMethod]
    public async Task Wait_Should_AnswerAsSoonAsAReviewerComments()
    {
        var cursor = (long)(await GetReview())["cursor"]!;
        var stopwatch = Stopwatch.StartNew();

        var wait = client.GetStringAsync($"api/review/wait?cursor={cursor}&agent=Codex", TestContext.CancellationToken);

        var id = await reviewer.Comment(server, "assistant", "Shorter answers please");

        var answer = JsonNode.Parse(await wait.WaitAsync(TimeSpan.FromSeconds(20), TestContext.CancellationToken))!;

        // A comment made just before this one can answer the wait first; waiting again from its cursor brings this one.
        for (var attempt = 0; attempt < 3 && CommentsIn(answer).Contains(id) is false; attempt++)
        {
            answer = JsonNode.Parse(await client.GetStringAsync($"api/review/wait?cursor={(long)answer["cursor"]!}&agent=Codex", TestContext.CancellationToken))!;
        }

        Assert.Contains(id, CommentsIn(answer));
        Assert.IsGreaterThan(cursor, (long)answer["cursor"]!);
        Assert.IsLessThan(TimeSpan.FromSeconds(20), stopwatch.Elapsed, "The wait answers when the comment arrives, not when it times out.");
    }

    [TestMethod]
    public async Task Wait_Should_NotAnswerForTheAgentsOwnChanges()
    {
        var id = await reviewer.Comment(server, "theme", "Bigger corners");
        var sequence = (await server.Agent.GetThreads(TestContext.CancellationToken)).Single(t => t.Id == id).Sequence;

        await server.Agent.Reply("Agent", id, "Done", TestContext.CancellationToken);
        await server.Agent.SetStatus("Agent", id, WorkbenchReviewStatus.Working, TestContext.CancellationToken);

        var (_, threads) = await server.Agent.Wait("Agent", sequence, TimeSpan.FromMilliseconds(400), TestContext.CancellationToken);

        Assert.DoesNotContain(t => t.Id == id, threads);

        reviewer.Session.Reply(id, "Sarah", "Still too small");

        var (_, woken) = await server.Agent.Wait("Agent", sequence, TimeSpan.FromSeconds(10), TestContext.CancellationToken);

        Assert.Contains(t => t.Id == id, woken, "A reviewer's reply wakes the agent.");
    }

    [TestMethod]
    public async Task Wait_Should_ReturnNothingOnceItTimesOut()
    {
        var (cursor, threads) = await server.Agent.Wait("Agent", long.MaxValue - 1, TimeSpan.FromMilliseconds(300), TestContext.CancellationToken);

        Assert.AreEqual(long.MaxValue - 1, cursor);
        Assert.IsEmpty(threads);
    }

    [TestMethod]
    public async Task AReply_Should_ReachTheReviewerAtOnce()
    {
        var id = await reviewer.Comment(server, "settings", "Is this label right?");

        var response = await client.PostAsJsonAsync($"api/review/{id}/reply", new { text = "Yes, it matches the app.", agent = "Claude Code" }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var thread = await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(reviewer.Session.Threads.Single(t => t.Id == id)), t => t.Messages.Length == 2);

        Assert.IsTrue(thread.Messages[1].FromAgent);
        Assert.AreEqual(("Claude Code", "Yes, it matches the app."), (thread.Messages[1].Author, thread.Messages[1].Text));
    }

    [TestMethod]
    public async Task AReplyWithoutTextOrToACommentThatIsGone_Should_BeRefused()
    {
        var id = await reviewer.Comment(server, "settings", "Something");

        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"api/review/{id}/reply", new { text = "  " }, TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("api/review/missing/reply", new { text = "Hi" }, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task Working_Should_ShowTheAgentOnTheCommentAndDoneShouldCloseIt()
    {
        var id = await reviewer.Comment(server, "pos-dashboard", "Swap the first two widgets");

        var working = await client.PostAsJsonAsync($"api/review/{id}/status", new { status = "Working", agent = "Copilot" }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, working.StatusCode);

        var agent = await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(reviewer.Session.Agent), a => a?.ThreadId == id);

        Assert.AreEqual("Copilot", agent!.Name);
        Assert.IsTrue(agent.IsActive);
        Assert.AreEqual("working", (string?)CommentOf(await GetReview(), id)!["status"]);

        await client.PostAsJsonAsync($"api/review/{id}/status", new { status = "done", agent = "Copilot" }, TestContext.CancellationToken);

        Assert.IsNull(CommentOf(await GetReview(), id));
        Assert.AreEqual(WorkbenchReviewStatus.Done, (await server.Agent.GetThreads(TestContext.CancellationToken)).Single(t => t.Id == id).Status);
    }

    [TestMethod]
    public async Task AStatusThatIsNotOneOrACommentThatIsGone_Should_BeRefused()
    {
        var id = await reviewer.Comment(server, "pos-dashboard", "Something");

        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"api/review/{id}/status", new { status = "finished" }, TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("api/review/missing/status", new { status = "done" }, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task Tasks_Should_ShowAsTheAgentsChecklist()
    {
        var id = await reviewer.Comment(server, "assistant", "Add a stop button");

        var response = await client.PutAsJsonAsync($"api/review/{id}/tasks", new { tasks = new[] { new { text = "Find the composer", done = true }, new { text = "Add the button", done = false } } }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var tasks = CommentOf(await GetReview(), id)!["tasks"]!.AsArray();

        Assert.HasCount(2, tasks);
        Assert.AreEqual(("Find the composer", true), ((string?)tasks[0]!["text"], (bool)tasks[0]!["done"]!));

        var thread = await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(reviewer.Session.Threads.Single(t => t.Id == id)), t => t.Tasks.Length == 2);

        Assert.IsFalse(thread.Tasks[1].Done);
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("api/review/missing/tasks", new { tasks = Array.Empty<object>() }, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task APageStatus_Should_BeSetOnlyForAPageTheWorkbenchHas()
    {
        var response = await client.PostAsJsonAsync("api/review/pages/SHIFTS/status", new { status = "inreview", agent = "Junie" }, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        var page = (await GetReview())["pages"]!.AsArray().Single(p => (string?)p!["page"] == "shifts")!;

        Assert.AreEqual(("inreview", "Junie"), ((string?)page["status"], (string?)page["by"]));
        Assert.AreEqual(WorkbenchPageStatus.InReview, (await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(reviewer.Session.PageOf("shifts")), p => p is not null))!.Status);

        Assert.AreEqual(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("api/review/pages/missing/status", new { status = "approved" }, TestContext.CancellationToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/review/pages/shifts/status", new { status = "shipped" }, TestContext.CancellationToken)).StatusCode);
    }

    [TestMethod]
    public async Task AFlow_Should_BeDrawnAndDrawnAgainUnderTheSameId()
    {
        var request = new
        {
            name = "Planning a week",
            agent = "Claude Code",
            screens = new[]
            {
                new { key = "week", page = "shifts", query = "", note = (string?)"The week", taps = Array.Empty<string>() },
                new { key = "dark", page = "shifts", query = "?theme=dark", note = (string?)null, taps = Array.Empty<string>() }
            },
            links = new[] { new { from = "week", to = "dark", label = "Switch to dark", tap = (string?)null } }
        };

        var response = await client.PostAsJsonAsync("api/review/flows", request, TestContext.CancellationToken);
        var saved = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken))!;
        var id = (string)saved["id"]!;

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual($"/flows/{id}", (string?)saved["board"]);

        var flow = (await GetReview())["flows"]!.AsArray().Single(f => (string?)f!["id"] == id)!;

        Assert.AreEqual("Planning a week", (string?)flow["name"]);
        Assert.HasCount(2, flow["screens"]!.AsArray());
        Assert.AreEqual("?theme=dark", (string?)flow["screens"]![1]!["query"]);
        Assert.AreEqual("Switch to dark", (string?)flow["links"]![0]!["label"]);

        var again = await client.PostAsJsonAsync("api/review/flows", new { id, name = "Planning a week, again", request.agent, request.screens, request.links }, TestContext.CancellationToken);

        Assert.AreEqual(id, (string?)JsonNode.Parse(await again.Content.ReadAsStringAsync(TestContext.CancellationToken))!["id"]);

        var redrawn = (await GetReview())["flows"]!.AsArray().Where(f => (string?)f!["id"] == id).ToArray();

        Assert.HasCount(1, redrawn);
        Assert.AreEqual("Planning a week, again", (string?)redrawn[0]!["name"]);
    }

    [TestMethod]
    [DataRow("""{"name":"","screens":[{"key":"a","page":"theme"}]}""", "name")]
    [DataRow("""{"name":"Flow","screens":[]}""", "screen")]
    [DataRow("""{"name":"Flow","screens":[{"key":"a","page":"nowhere"}]}""", "nowhere")]
    [DataRow("""{"name":"Flow","screens":[{"key":"a","page":"theme"},{"key":"a","page":"theme"}]}""", "key of its own")]
    [DataRow("""{"name":"Flow","screens":[{"key":"a","page":"theme"}],"links":[{"from":"a","to":"b"}]}""", "not in the flow")]
    public async Task AFlowThatDoesNotHoldTogether_Should_BeRefusedWithTheReason(string body, string reason)
    {
        var response = await client.PostAsync("api/review/flows", new StringContent(body, System.Text.Encoding.UTF8, "application/json"), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(reason, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    private static string[] CommentsIn(JsonNode answer) => [.. answer["comments"]!.AsArray().Select(c => (string)c!["id"]!)];
}
