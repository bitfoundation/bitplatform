namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// Comment mode in a browser: a comment pinned on a spot of a view, its replies, the agent's answers arriving live, and
/// the read-only review a visitor through a tunnel gets.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ReviewUITests : WorkbenchPageTest
{
    private ILocator Button(string name) => Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator ThreadWith(string text) => Page.Locator("article.thread").Filter(new() { HasText = text });

    /// <summary>Opens a comment's thread; one just sent is open already.</summary>
    private async Task Select(string text)
    {
        var head = ThreadWith(text).Locator("button.thread-head");

        if (await head.GetAttributeAsync("aria-expanded") is not "true")
        {
            await head.ClickAsync();
        }

        await Expect(head).ToHaveAttributeAsync("aria-expanded", "true");
    }

    private async Task<string> PinAComment(string text)
    {
        await Button(WorkbenchStrings.ModeComment).ClickAsync();

        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");

        await View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }).ClickAsync();

        var composer = Page.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.NewComment, Exact = true });

        await Expect(composer).ToBeVisibleAsync();
        await Expect(Page.Locator(".composer")).ToContainTextAsync($"“{WorkbenchStrings.ShiftsHeading}”");

        await composer.FillAsync(text);
        await composer.PressAsync("Control+Enter");

        await Expect(ThreadWith(text)).ToBeVisibleAsync();

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages.FirstOrDefault()?.Text == text));

        return threads.Single(t => t.Messages[0].Text == text).Id;
    }

    [TestMethod]
    public async Task AComment_Should_BePinnedOnTheSpotItWasLeftOnAndReachTheAgent()
    {
        await OpenWorkbench("shifts", "?theme=dark");

        var text = $"Make the heading bolder {Guid.NewGuid():N}";
        var id = await PinAComment(text);

        var thread = (await Server.Agent.GetThreads(CancellationToken.None)).Single(t => t.Id == id);

        Assert.AreEqual("shifts", thread.PageKey);
        Assert.AreEqual("?theme=dark", thread.Query);
        Assert.AreEqual(WorkbenchStrings.ShiftsHeading, thread.Anchor.Text);
        Assert.IsTrue(thread.HasState, "The view's page state was saved with the comment.");

        await Expect(View().Locator($".wb-pin[data-id='{id}']")).ToBeVisibleAsync();
        await Expect(Composer()).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AReplyAndTheAgentsAnswer_Should_ShowInTheThread()
    {
        await OpenWorkbench("shifts");

        var text = $"Is the week right? {Guid.NewGuid():N}";
        var id = await PinAComment(text);

        await Select(text);

        var reply = Page.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.Reply, Exact = true });

        await reply.FillAsync("It should start on Monday");
        await reply.PressAsync("Control+Enter");

        await Expect(ThreadWith(text)).ToContainTextAsync("It should start on Monday");

        await Server.Agent.SetStatus("Claude Code", id, WorkbenchReviewStatus.Working, CancellationToken.None);
        await Server.Agent.Reply("Claude Code", id, "It starts on Monday now.", CancellationToken.None);

        await Expect(ThreadWith(text)).ToContainTextAsync("It starts on Monday now.");
        await Expect(ThreadWith(text).Locator(".thread-status")).ToHaveTextAsync(WorkbenchStrings.ReviewStatusWorking);
        await Expect(Page.Locator(".agent.active")).ToContainTextAsync("Claude Code");
    }

    [TestMethod]
    public async Task DeletingAComment_Should_AskOnceMoreAndRemoveItEverywhere()
    {
        await OpenWorkbench("shifts");

        var text = $"Never mind {Guid.NewGuid():N}";
        var id = await PinAComment(text);

        await Select(text);

        var delete = ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteComment });

        await delete.ClickAsync();
        await Expect(delete).ToContainTextAsync(WorkbenchStrings.DeleteCommentConfirm);

        await delete.ClickAsync();

        await Expect(ThreadWith(text)).ToHaveCountAsync(0);
        await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Id == id) is false);
    }

    [TestMethod]
    public async Task ResolvingAComment_Should_MoveItUnderDone()
    {
        await OpenWorkbench("shifts");

        var text = $"Done soon {Guid.NewGuid():N}";
        var id = await PinAComment(text);

        await Select(text);
        await ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Resolve, Exact = true }).ClickAsync();

        await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Single(t => t.Id == id).Status is WorkbenchReviewStatus.Done);
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex($"^{Regex.Escape(WorkbenchStrings.DoneComments.Split('(')[0])}") })).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ThreadWith(text)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task AVisitorThroughATunnel_Should_SeeTheReviewReadOnly()
    {
        var text = $"Seen by a visitor {Guid.NewGuid():N}";

        await using (var reviewer = await WorkbenchTestReviewer.Open(Server))
        {
            await reviewer.Comment(Server, "shifts", text);
        }

        await using var visitor = await Browser.NewContextAsync(new BrowserNewContextOptions(ContextOptions()) { ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" } });
        var page = await visitor.NewPageAsync();

        await page.GotoAsync(new Uri(Server.Address, "workbench/shifts").ToString());
        await Expect(page.Locator(".pane-stage.ready")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ModeComment, Exact = true }).ClickAsync();

        await Expect(page.GetByText(WorkbenchStrings.LiveReviewTitle)).ToBeVisibleAsync();
        await Expect(page.GetByText(WorkbenchStrings.AgentOffline)).ToBeVisibleAsync();
        await Expect(page.Locator("article.thread").Filter(new() { HasText = text })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ModeComment, Exact = true })).ToHaveAttributeAsync("aria-pressed", "false");

        await page.Locator("article.thread").Filter(new() { HasText = text }).Locator("button.thread-head").ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.Reply, Exact = true })).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PageStatusApproved, Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task EditText_Should_TraceATextToItsEntryAndSendAChangeToTheAgent()
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeText).ClickAsync();
        await View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }).ClickAsync();

        await Expect(Page.Locator(".text-panel")).ToContainTextAsync($"“{WorkbenchStrings.ShiftsHeading}”");
        await Expect(Page.Locator(".text-panel .key")).ToHaveTextAsync($"{nameof(WorkbenchStrings)} · {nameof(WorkbenchStrings.ShiftsHeading)}");
        await Expect(Page.GetByText(WorkbenchStrings.SaveTranslationsRemoteHint)).ToBeVisibleAsync();

        var text = $"Weekly schedule {Guid.NewGuid():N}";

        await Page.GetByLabel(CultureInfoManager.SupportedCultures[0].DisplayName, new() { Exact = true }).FillAsync(text);
        await Page.Locator(".text-panel").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AskTheAgent }).ClickAsync();

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages[0].Text.Contains(text, StringComparison.Ordinal)));
        var request = threads.Single(t => t.Messages[0].Text.Contains(text, StringComparison.Ordinal));

        Assert.AreEqual("shifts", request.PageKey);
        Assert.Contains(nameof(WorkbenchStrings.ShiftsHeading), request.Messages[0].Text);
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");
    }

    private ILocator Composer() => Page.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.NewComment, Exact = true });
}
