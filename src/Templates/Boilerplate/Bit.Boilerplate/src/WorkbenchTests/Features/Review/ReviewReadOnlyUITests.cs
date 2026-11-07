namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>The review without an agent: a copy like a published demo, where comments still work and little else changes, and a visitor through a tunnel on a copy with one.</summary>
[TestClass, TestCategory("UITest")]
public class ReviewReadOnlyUITests : ReviewPageTest
{
    private const string AgentName = "Claude Code";

    private async Task ExpectNoWayToChangeTheThread(ILocator tools, ILocator thread)
    {
        await Expect(tools.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.Reply, Exact = true })).ToHaveCountAsync(0);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Reply, Exact = true })).ToHaveCountAsync(0);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Resolve, Exact = true })).ToHaveCountAsync(0);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Reopen, Exact = true })).ToHaveCountAsync(0);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteComment })).ToHaveCountAsync(0);
    }

    [TestMethod, OnDesktops]
    public async Task AClickWithoutCommentMode_Should_StartNoComment(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        await View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true }).ClickAsync();

        await Expect(View().GetByText(WorkbenchStrings.BuildingsAddTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Composer).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task CommentMode_Should_OpenTheReviewAndStartAComment(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        await Button(WorkbenchStrings.ModeText).ClickAsync();
        await Expect(Page.Locator(".text-panel")).ToBeVisibleAsync();

        await Button(WorkbenchStrings.ModeComment).ClickAsync();

        await Expect(Page.Locator(".text-panel")).ToHaveCountAsync(0);
        await Expect(AgentLine).ToHaveTextAsync(WorkbenchStrings.AgentOffline);
        await Expect(Button(WorkbenchStrings.ModeComment)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(View().Locator("html")).ToHaveAttributeAsync("data-wb-mode", WorkbenchModes.Comment);

        await View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true }).ClickAsync();

        await Expect(Composer).ToBeVisibleAsync();
        await Expect(View().GetByText(WorkbenchStrings.BuildingsAddTitle, new() { Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task CommentMode_Should_OpenTheToolsWhenTheyAreFoldedAway(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.Tools).ClickAsync();
        await Expect(Button(WorkbenchStrings.Tools)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#wb-inspector")).Not.ToBeInViewportAsync();

        await Button(WorkbenchStrings.ModeComment).ClickAsync();

        await Expect(Button(WorkbenchStrings.Tools)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(AgentLine).ToHaveTextAsync(WorkbenchStrings.AgentOffline);
        await Expect(AgentLine).ToBeInViewportAsync();
        await Expect(Button(WorkbenchStrings.ModeComment)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithoutAgent)]
    public async Task ThePhonesTools_Should_OfferThePinWithThePageStillInUse(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");
        await OpenTools();

        await Expect(ToolsButton(WorkbenchStrings.PinComment)).ToBeVisibleAsync();
        await Expect(AgentLine).ToHaveTextAsync(WorkbenchStrings.AgentOffline);

        await CloseTools();

        await Expect(PickBanner).ToHaveCountAsync(0);

        await Press(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true }));

        await Expect(View().GetByText(WorkbenchStrings.BuildingsAddTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Tools).ToHaveAttributeAsync("inert", string.Empty);
    }

    [TestMethod, WithoutAnAgent]
    public async Task TheNoticeOnEveryPage_Should_SayNoAgentIsConnected(WorkbenchSurface surface)
    {
        var (flowId, _) = await Server.Agent.SaveFlow("Seed", new(Unique("A flow"), [new("only", "theme", "", null, null)], null, null, null), CancellationToken.None);

        foreach (var url in new[] { WorkbenchUrls.Home, WorkbenchUrls.Workbench("shifts"), WorkbenchUrls.Flows, WorkbenchUrls.Flow(flowId!), "/workbench/no-such-page" })
        {
            await Page.GotoAsync(new Uri(Server.Address, url).ToString());

            var notice = Page.GetByRole(AriaRole.Status).Filter(new() { HasText = WorkbenchStrings.AgentNotConnected });

            await Expect(notice).ToBeVisibleAsync();
            await Expect(notice).ToContainTextAsync(WorkbenchStrings.LimitedExperience);
            await Expect(notice).ToHaveAttributeAsync("title", WorkbenchStrings.AgentNotConnectedHint);
        }
    }

    [TestMethod, Surfaces(WorkbenchSurface.PhoneWithoutAgent)]
    public async Task TheNotice_Should_StayClearOfTheToolsButton(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        var notice = await Page.GetByRole(AriaRole.Status).Filter(new() { HasText = WorkbenchStrings.AgentNotConnected }).BoundingBoxAsync();
        var button = await Page.Locator(".tools-button").BoundingBoxAsync();

        Assert.IsNotNull(notice);
        Assert.IsNotNull(button);

        var overlaps = notice.X < button.X + button.Width && button.X < notice.X + notice.Width && notice.Y < button.Y + button.Height && button.Y < notice.Y + notice.Height;

        Assert.IsFalse(overlaps, "The notice would cover the only way to the tools.");
    }

    [TestMethod, WithAnAgent]
    public async Task DevMode_Should_ShowNoNotice(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");
        await OpenTools();

        await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToHaveCountAsync(0);
    }

    [TestMethod, WithoutAnAgent]
    public async Task ThePreview_Should_LeaveTheNoticeOutOfTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await Expect(Page.GetByText(WorkbenchStrings.AgentNotConnected)).ToBeVisibleAsync();
        await Expect(View().GetByText(WorkbenchStrings.AgentNotConnected)).ToHaveCountAsync(0);
    }

    [TestMethod, WithAnAgent]
    public async Task AVisitorThroughATunnel_Should_SeeTheReviewReadOnly(WorkbenchSurface surface)
    {
        var text = Unique("Seen by a visitor");
        var id = await SeedComment("shifts", text);

        await using var visitor = await Browser.NewContextAsync(new BrowserNewContextOptions(ContextOptions()) { ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" } });
        var page = await visitor.NewPageAsync();

        await page.GotoAsync(new Uri(Server.Address, "workbench/shifts").ToString());
        await Expect(page.Locator(".pane-stage.ready")).ToBeVisibleAsync();

        var tools = page.Locator(IsPhone ? ".tools-panel" : "#wb-inspector");
        var thread = tools.Locator("article.thread").Filter(new() { HasText = text });

        if (IsPhone)
        {
            await page.Locator(".tools-button").TapAsync();
            await Expect(page.Locator(".tools-header")).ToBeVisibleAsync();
            await Expect(tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PinComment, Exact = true })).ToBeVisibleAsync();
        }
        else
        {
            var commentMode = page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ModeComment, Exact = true });

            await commentMode.ClickAsync();
            await Expect(commentMode).ToHaveAttributeAsync("aria-pressed", "true");
        }

        await Expect(page.GetByText(WorkbenchStrings.AgentNotConnected)).ToBeVisibleAsync();
        await Expect(tools.Locator(".agent")).ToHaveTextAsync(WorkbenchStrings.AgentOffline);
        await Expect(thread).ToBeVisibleAsync();

        var head = thread.Locator("button.thread-head");

        await (IsPhone ? head.TapAsync() : head.ClickAsync());

        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView })).ToBeVisibleAsync();

        await ExpectNoWayToChangeTheThread(tools, thread);

        await Expect(tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PageStatusApproved, Exact = true })).ToHaveCountAsync(0);

        // The visitor still sees the review change as the agent answers.
        var answer = Unique("Answered while they watch");

        await Server.Agent.Reply(AgentName, id, answer, CancellationToken.None);

        await Expect(thread.Locator(".message.from-agent")).ToContainTextAsync(answer);
    }
}
