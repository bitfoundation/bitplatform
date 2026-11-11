namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>The agent in an open browser: its replies, plan and statuses arriving live, the line saying what it does, and a comment reaching it as it waits.</summary>
[TestClass, TestCategory("UITest")]
public class ReviewAgentUITests : ReviewPageTest
{
    private const string AgentName = "Claude Code";

    private ILocator ShiftsHeading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading });

    private ILocator PlanOf(string text) => ThreadWith(text).GetByRole(AriaRole.List, new() { Name = WorkbenchStrings.AgentTasks });

    private static string Formatted(string format, params object[] values) => string.Format(CultureInfo.InvariantCulture, format, values);

    /// <summary>Opens the review of a page on which a comment waits for the agent, with its thread open.</summary>
    private async Task<(string Id, string Text)> OpenACommentForTheAgent(string pageKey = "shifts")
    {
        var text = Unique("Is the week right?");
        var id = await SeedComment(pageKey, text);

        await OpenWorkbench(pageKey);
        await OpenReview();
        await OpenThread(text);
        await MarkThePage();

        return (id, text);
    }

    [TestMethod, EverySurface]
    public async Task TheAgentsReply_Should_ArriveLiveWithoutAReload(WorkbenchSurface surface)
    {
        var (id, text) = await OpenACommentForTheAgent();
        var answer = Unique("It starts on Monday now");

        await Server.Agent.Reply(AgentName, id, answer, CancellationToken.None);

        var message = ThreadWith(text).Locator(".message.from-agent");

        await Expect(message.Locator(".message-text")).ToHaveTextAsync(answer);
        await Expect(message.Locator(".message-author")).ToContainTextAsync(AgentName);
        await ExpectNoReload();
    }

    [TestMethod, EverySurface]
    public async Task ThePlan_Should_ShowAsAChecklistAndTickLive(WorkbenchSurface surface)
    {
        var (id, text) = await OpenACommentForTheAgent();

        await Expect(PlanOf(text)).ToHaveCountAsync(0);

        await Server.Agent.SetTasks(AgentName, id, [new("Find the week", false), new("Start it on Monday", false)], CancellationToken.None);

        await Expect(PlanOf(text).GetByRole(AriaRole.Listitem)).ToHaveTextAsync(["Find the week", "Start it on Monday"]);
        await Expect(PlanOf(text).Locator("li.done")).ToHaveCountAsync(0);

        await Server.Agent.SetTasks(AgentName, id, [new("Find the week", true), new("Start it on Monday", false)], CancellationToken.None);

        await Expect(PlanOf(text).Locator("li.done")).ToHaveTextAsync(["Find the week"]);

        await Server.Agent.SetTasks(AgentName, id, [new("Find the week", true), new("Start it on Monday", true)], CancellationToken.None);

        await Expect(PlanOf(text).Locator("li.done")).ToHaveCountAsync(2);
        await ExpectNoReload();
    }

    [TestMethod, EverySurface]
    public async Task TheAgentsStatus_Should_ArriveLive(WorkbenchSurface surface)
    {
        var (id, text) = await OpenACommentForTheAgent();

        await Expect(ThreadWith(text).Locator(".thread-status")).ToHaveTextAsync(WorkbenchStrings.ReviewStatusOpen);

        await Server.Agent.SetStatus(AgentName, id, WorkbenchReviewStatus.Working, CancellationToken.None);

        await Expect(ThreadWith(text).Locator(".thread-status")).ToHaveTextAsync(WorkbenchStrings.ReviewStatusWorking);
        await Expect(ThreadWith(text).Locator(".number")).ToHaveClassAsync(new Regex("status-working"));

        await Server.Agent.SetStatus(AgentName, id, WorkbenchReviewStatus.Done, CancellationToken.None);

        await Expect(ListButton(WorkbenchStrings.DoneComments)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ThreadWith(text).Locator(".thread-status")).ToHaveTextAsync(WorkbenchStrings.ReviewStatusDone);
        await ExpectNoReload();
    }

    [TestMethod, EverySurface]
    public async Task AClosedThread_Should_ShowTheAgentsLatestReplyAndAnotherThreadStayWhereItIs(WorkbenchSurface surface)
    {
        var closed = Unique("Waiting closed");
        var closedId = await SeedComment("shifts", closed);

        await OpenWorkbench("shifts");
        await OpenReview();

        await Expect(ThreadWith(closed)).ToBeVisibleAsync();
        await MarkThePage();

        var answer = Unique("Answered");

        await Server.Agent.Reply(AgentName, closedId, answer, CancellationToken.None);

        await Expect(ThreadWith(answer).Locator("button.thread-head")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(ThreadWith(closed)).ToHaveCountAsync(0);
        await ExpectNoReload();
    }

    [TestMethod, WithAnAgent]
    public async Task ACommentFromAnotherBrowser_Should_ArriveLive(WorkbenchSurface surface)
    {
        var text = Unique("From the other browser");

        await OpenWorkbench("shifts");
        await OpenReview();
        await MarkThePage();

        await using var reviewer = await WorkbenchTestReviewer.Open(Server, name: "Someone else");

        await reviewer.Comment(Server, "shifts", text);

        await Expect(ThreadWith(text)).ToBeVisibleAsync();
        await ExpectNoReload();
    }

    [TestMethod, WithAnAgent]
    public async Task TheAgentLine_Should_SayWhatTheAgentDoes(WorkbenchSurface surface)
    {
        var text = Unique("For the agent line");
        var id = await SeedComment("shifts", text);

        await SetAgentLastSeen(AgentName, null, DateTimeOffset.UtcNow.AddMinutes(-10));

        await OpenWorkbench("shifts");
        await OpenReview();

        await Expect(AgentLine).ToHaveTextAsync(WorkbenchStrings.NoAgentListening);
        await Expect(AgentLine).Not.ToHaveClassAsync(new Regex(@"\bactive\b"));

        await Server.Agent.Wait(AgentName, long.MaxValue, TimeSpan.Zero, CancellationToken.None);

        await Expect(AgentLine).ToHaveTextAsync(Formatted(WorkbenchStrings.AgentListening, AgentName));
        await Expect(AgentLine).ToHaveClassAsync(new Regex(@"\bactive\b"));

        await Server.Agent.SetStatus(AgentName, id, WorkbenchReviewStatus.Working, CancellationToken.None);

        var number = (await Server.Agent.GetThreads(CancellationToken.None)).Single(t => t.Id == id).Number;

        await Expect(AgentLine).ToHaveTextAsync(Formatted(WorkbenchStrings.AgentWorkingOn, AgentName, number));
        await Expect(AgentLine).ToHaveClassAsync(new Regex(@"\bactive\b"));

        await Server.Agent.SetStatus(AgentName, id, WorkbenchReviewStatus.Done, CancellationToken.None);

        await Expect(AgentLine).ToHaveTextAsync(Formatted(WorkbenchStrings.AgentListening, AgentName));
    }

    [TestMethod, WithAnAgent]
    public async Task TheAgentLine_Should_GoIdleOnceTheAgentStopsCalling(WorkbenchSurface surface)
    {
        await Server.Agent.Wait(AgentName, long.MaxValue, TimeSpan.Zero, CancellationToken.None);

        await OpenWorkbench("shifts");
        await OpenReview();

        await Expect(AgentLine).ToHaveTextAsync(Formatted(WorkbenchStrings.AgentListening, AgentName));

        // Its last call was just long enough ago, and nothing changes in the review after that.
        await SetAgentLastSeen(AgentName, null, DateTimeOffset.UtcNow - WorkbenchReviewAgentInfo.OnlineFor + TimeSpan.FromSeconds(2));

        await Expect(AgentLine).ToHaveTextAsync(WorkbenchStrings.NoAgentListening);
        await Expect(AgentLine).Not.ToHaveClassAsync(new Regex(@"\bactive\b"));
    }

    [TestMethod, WithoutAnAgent]
    public async Task TheAgentLine_Should_SayOfflineWhateverTheReviewRemembers(WorkbenchSurface surface)
    {
        await Server.Agent.Wait(AgentName, long.MaxValue, TimeSpan.Zero, CancellationToken.None);

        await OpenWorkbench("shifts");
        await OpenReview();

        await Expect(AgentLine).ToHaveTextAsync(WorkbenchStrings.AgentOffline);
        await Expect(AgentLine).Not.ToHaveClassAsync(new Regex(@"\bactive\b"));
    }

    [TestMethod, WithAnAgent]
    public async Task ACommentAndAReply_Should_WakeTheWaitingAgent(WorkbenchSurface surface)
    {
        await ClearComments("shifts");
        await OpenWorkbench("shifts");

        var cursor = (await Server.Agent.GetThreads(CancellationToken.None)).Select(t => t.Sequence).DefaultIfEmpty(0).Max();
        var text = Unique("Wake up");
        var waiting = Server.Agent.Wait(AgentName, cursor, WorkbenchReviewAgent.MaxWait, CancellationToken.None);

        var id = await LeaveAComment(ShiftsHeading, text);

        var (next, threads) = await waiting;

        Assert.Contains(t => t.Id == id && t.Messages[0].Text == text, threads);

        waiting = Server.Agent.Wait(AgentName, next, WorkbenchReviewAgent.MaxWait, CancellationToken.None);

        var reply = Unique("And one more thing");

        await ReplyBox.FillAsync(reply);
        await ReplyBox.PressAsync("Control+Enter");

        (_, threads) = await waiting;

        Assert.Contains(t => t.Id == id && t.Messages[^1].Text == reply, threads);
    }

    [TestMethod, WithAnAgent]
    public async Task AnAgentOverHttp_Should_ShowLiveInTheBrowser(WorkbenchSurface surface)
    {
        var (id, text) = await OpenACommentForTheAgent();

        using var client = Server.CreateClient();

        var answer = Unique("Answered over HTTP");

        (await client.PostAsJsonAsync($"api/review/{id}/status", new { status = "working", agent = "Http Agent" }, cancellationToken: TestContext.CancellationToken)).EnsureSuccessStatusCode();

        await Expect(ThreadWith(text).Locator(".thread-status")).ToHaveTextAsync(WorkbenchStrings.ReviewStatusWorking);
        await Expect(AgentLine).ToContainTextAsync("Http Agent");

        (await client.PutAsJsonAsync($"api/review/{id}/tasks", new { tasks = new[] { new { text = "Read it", done = true }, new { text = "Answer it", done = false } }, agent = "Http Agent" }, cancellationToken: TestContext.CancellationToken)).EnsureSuccessStatusCode();

        await Expect(PlanOf(text).Locator("li.done")).ToHaveTextAsync(["Read it"]);

        (await client.PostAsJsonAsync($"api/review/{id}/reply", new { text = answer, agent = "Http Agent" }, cancellationToken: TestContext.CancellationToken)).EnsureSuccessStatusCode();

        await Expect(ThreadWith(text).Locator(".message.from-agent")).ToContainTextAsync(answer);

        (await client.PostAsJsonAsync("api/review/pages/shifts/status", new { status = "inreview", agent = "Http Agent" }, cancellationToken: TestContext.CancellationToken)).EnsureSuccessStatusCode();

        await Expect(ToolsButton(WorkbenchStrings.PageStatusInReview)).ToHaveAttributeAsync("aria-pressed", "true");
        await ExpectNoReload();
    }
}
