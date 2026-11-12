using Boilerplate.WorkbenchTests.Features.Review;

namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>The tour's live review step, which lights up the Comment mode's button and the Review tab and takes a real comment.</summary>
[TestClass, TestCategory("UITest")]
public class TourCommentUITests : ReviewPageTest
{
    private ILocator Popover => Page.Locator(".bit-bonboarding-popover");

    private ILocator Inspector => Page.Locator("#wb-inspector");

    private ILocator TourComposer => Inspector.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.NewComment, Exact = true });

    private ILocator CommentMode => Page.Locator(".modes").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ModeComment, Exact = true });

    private async Task ReachTheLiveReviewStep(string pageKey = "shifts")
    {
        await ClearComments(pageKey);
        await SetAgentLastSeen("Claude Code", null, DateTimeOffset.UtcNow.AddHours(-1));
        await OpenWorkbench(pageKey);
        await AdvanceToTheLiveReviewStep();
    }

    private async Task AdvanceToTheLiveReviewStep()
    {
        if (IsPhone)
        {
            await OpenTools();
            await Press(Tools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.TourStart, Exact = true }));
        }
        else
        {
            await Press(Button(WorkbenchStrings.TourStart));
        }

        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyAgentsTitle, new() { Exact = true })).ToBeVisibleAsync();

        foreach (var title in new[] { WorkbenchStrings.HomeWhyHeadStartTitle, WorkbenchStrings.HomeWhyDevicesTitle, WorkbenchStrings.HomeWhyStatesTitle, WorkbenchStrings.HomeWhyStoresTitle, WorkbenchStrings.HomeWhyRealTitle, WorkbenchStrings.HomeWhyLiveTitle })
        {
            await Press(Button(WorkbenchStrings.TourNext));
            await Expect(Popover.GetByText(title, new() { Exact = true })).ToBeVisibleAsync();
        }
    }

    // Nothing of the tour's lies over the middle of the element, so it is lit and can be used.
    private async Task ExpectNotBlocked(ILocator target)
    {
        await Expect(target).ToBeVisibleAsync();

        var isReachable = await target.EvaluateAsync<bool>("element => { const box = element.getBoundingClientRect(); const top = document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2); return element === top || element.contains(top); }");

        Assert.IsTrue(isReachable, "The tour covers this element.");
    }

    [TestMethod, EverySurface]
    public async Task TheStep_Should_LightUpTheCommentButtonAndTheReviewTab(WorkbenchSurface surface)
    {
        await ReachTheLiveReviewStep();

        await Expect(CommentMode).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");

        await ExpectNotBlocked(CommentMode);
        await ExpectNotBlocked(Page.Locator("#wb-tab-review"));
    }

    [TestMethod, EverySurface]
    public async Task TheStep_Should_TakeACommentAndSayWhatHappensNext(WorkbenchSurface surface)
    {
        await ReachTheLiveReviewStep();

        var text = Unique("Tried during the tour");

        await Expect(TourComposer).ToBeVisibleAsync();
        await ExpectNotBlocked(TourComposer);
        await Press(TourComposer);
        await Page.Keyboard.TypeAsync(text);
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Expect(TourComposer).ToHaveValueAsync(text);
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyLiveTitle, new() { Exact = true })).ToBeVisibleAsync();

        await Press(Inspector.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Send, Exact = true }));

        var thread = Inspector.Locator("article.thread").Filter(new() { HasText = text });

        await Expect(thread).ToBeVisibleAsync();
        await Expect(thread.Locator(".message.auto-reply")).ToContainTextAsync(WorkbenchStrings.CommentWaitsForAgent);
        await Expect(TourComposer).ToHaveCountAsync(0);

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages.FirstOrDefault()?.Text == text));

        Assert.AreEqual("shifts", threads.Single(t => t.Messages[0].Text == text).PageKey);

        await Press(Button(WorkbenchStrings.TourNext));
        await Expect(Popover.GetByText(WorkbenchStrings.Flows, new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task APressOnThePage_Should_PinTheCommentThere(WorkbenchSurface surface)
    {
        await ReachTheLiveReviewStep();

        await View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }).ClickAsync();

        await Expect(Inspector.Locator(".composer")).ToContainTextAsync($"“{WorkbenchStrings.ShiftsHeading}”");
        await Expect(DraftPin).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task APageStillLoadingFromTheStepBefore_Should_NotTakeOverTheView(WorkbenchSurface surface)
    {
        await Context.RouteAsync(url => url.Contains("/preview/shifts", StringComparison.Ordinal), async route =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3), TestContext.CancellationToken);
            await route.ContinueAsync();
        });

        await ReachTheLiveReviewStep("pos-dashboard");

        await Page.WaitForTimeoutAsync(4000);

        await Expect(View().Locator(".shifts")).ToHaveCountAsync(0);
        await Expect(View().GetByText(WorkbenchStrings.PosCustomize, new() { Exact = true })).ToBeAttachedAsync();
    }

    [TestMethod, EverySurface]
    public async Task Cancel_Should_CloseTheComposerWithoutLeavingTheStep(WorkbenchSurface surface)
    {
        await ReachTheLiveReviewStep();

        await Expect(TourComposer).ToBeVisibleAsync();

        await Press(Inspector.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true }));

        await Expect(TourComposer).ToHaveCountAsync(0);
        await Expect(Popover.GetByText(WorkbenchStrings.HomeWhyLiveTitle, new() { Exact = true })).ToBeVisibleAsync();

        if (IsPhone is false)
        {
            await View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }).ClickAsync();

            await Expect(TourComposer).ToBeVisibleAsync();
        }
    }

    [TestMethod, EverySurface]
    public async Task AThreadOpenBeforeTheTour_Should_NotHideTheComposer(WorkbenchSurface surface)
    {
        await ClearComments("shifts");

        var text = Unique("Open when the tour starts");

        await SeedComment("shifts", text);

        await OpenWorkbench("shifts");
        await OpenReview();
        await OpenThread(text);
        await CloseTools();

        await AdvanceToTheLiveReviewStep();

        await Expect(TourComposer).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task LeavingTheStep_Should_TakeTheComposerAway(WorkbenchSurface surface)
    {
        await ReachTheLiveReviewStep();

        await Expect(TourComposer).ToBeVisibleAsync();

        await Press(Button(WorkbenchStrings.TourSkip));

        await Expect(Popover).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.NewComment, Exact = true })).ToHaveCountAsync(0);
    }
}
