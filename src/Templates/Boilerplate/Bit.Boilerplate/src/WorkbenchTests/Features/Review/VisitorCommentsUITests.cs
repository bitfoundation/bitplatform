namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// A visitor who reaches a copy from elsewhere, like a published demo or a tunnel. Their comments stay in their tab, and nothing
/// reaches the review the machine keeps.
/// </summary>
[TestClass, TestCategory("UITest")]
public class VisitorCommentsUITests : ReviewPageTest
{
    private const string AgentName = "Claude Code";

    private ILocator ShiftsHeading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading });

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        options.ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Forwarded-For"] = "203.0.113.7" };

        return options;
    }

    private async Task ExpectNoWayToChangeTheThread(ILocator thread)
    {
        await Expect(Tools.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.Reply, Exact = true })).ToHaveCountAsync(0);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Reply, Exact = true })).ToHaveCountAsync(0);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Resolve, Exact = true })).ToHaveCountAsync(0);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Reopen, Exact = true })).ToHaveCountAsync(0);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteComment })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AVisitorsComment_Should_StayInTheirTabAndSaySo(WorkbenchSurface surface)
    {
        await ClearComments("shifts");
        await OpenWorkbench("shifts");

        var text = Unique("Seen only by this visitor");

        await PickSpot(ShiftsHeading);
        await Composer.FillAsync(text);
        await Press(ToolsButton(WorkbenchStrings.Send));

        await Expect(ThreadWith(text)).ToBeVisibleAsync();
        await Expect(ThreadWith(text).Locator(".message.auto-reply")).ToContainTextAsync(WorkbenchStrings.CommentStaysInTab);

        Assert.DoesNotContain(t => t.Messages.Any(m => m.Text == text), await Server.Agent.GetThreads(CancellationToken.None), "The visitor's comment reached the machine's review.");

        var reply = Unique("A second thought");

        await ReplyBox.FillAsync(reply);
        await Press(ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Reply, Exact = true }));

        await Expect(ThreadWith(text)).ToContainTextAsync(reply);

        var delete = ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.DeleteComment });

        await Press(delete);
        await Expect(delete).ToContainTextAsync(WorkbenchStrings.DeleteCommentConfirm);
        await Press(delete);

        await Expect(ThreadWith(text)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AVisitorsTab_Should_NotSeeAnotherVisitorsComment(WorkbenchSurface surface)
    {
        await ClearComments("shifts");
        await OpenWorkbench("shifts");

        var text = Unique("Seen only by the first tab");

        await PickSpot(ShiftsHeading);
        await Composer.FillAsync(text);
        await Press(ToolsButton(WorkbenchStrings.Send));
        await Expect(ThreadWith(text)).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await WaitForViews();
        await OpenReview();
        await OpenToolsIfClosed();

        await Expect(AgentLine).ToBeVisibleAsync();
        await Expect(ThreadWith(text)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheMachinesComments_Should_ShowWithNoWayToChangeThem(WorkbenchSurface surface)
    {
        await ClearComments("shifts");

        var text = Unique("Left by the owner");
        var answer = Unique("Answered by the agent");
        var id = await SeedComment("shifts", text);

        await Server.Agent.SetTasks(AgentName, id, [new("Read it", true), new("Answer it", false)], CancellationToken.None);
        await Server.Agent.Reply(AgentName, id, answer, CancellationToken.None);

        await OpenWorkbench("shifts");
        await OpenReview();

        var thread = ThreadWith(answer);

        await Expect(thread).ToBeVisibleAsync();
        await Expect(thread.Locator(".message.from-agent .message-author")).ToContainTextAsync(AgentName);

        await Press(thread.Locator("button.thread-head"));

        await Expect(thread).ToContainTextAsync(text);
        await Expect(thread.GetByRole(AriaRole.List, new() { Name = WorkbenchStrings.AgentTasks }).Locator("li.done")).ToHaveTextAsync(["Read it"]);
        await Expect(thread.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView })).ToBeVisibleAsync();

        await ExpectNoWayToChangeTheThread(thread);

        await Expect(Tools.GetByRole(AriaRole.Switch, new() { Name = WorkbenchStrings.ReplyNotifications })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheMachinesUnansweredComment_Should_NotBeToldItStaysInTheTab(WorkbenchSurface surface)
    {
        await ClearComments("shifts");

        var text = Unique("Left by the owner, not answered yet");

        await SeedComment("shifts", text);

        await OpenWorkbench("shifts");
        await OpenReview();

        await Expect(ThreadWith(text)).ToBeVisibleAsync();
        await Expect(ThreadWith(text).Locator(".message.auto-reply")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ShowTheView_Should_BringBackThePageStateOfAVisitorsComment(WorkbenchSurface surface)
    {
        await ClearComments("buildings");
        await OpenWorkbench("buildings");

        var editor = View().GetByText(WorkbenchStrings.BuildingsAddTitle, new() { Exact = true });

        await Press(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true }));
        await Expect(editor).ToBeVisibleAsync();

        var text = Unique("About the editor");

        await PickSpot(editor);
        await Composer.FillAsync(text);
        await Press(ToolsButton(WorkbenchStrings.Send));
        await Expect(ThreadWith(text)).ToBeVisibleAsync();

        await CloseTools();

        if (IsPhone is false)
        {
            await Button(WorkbenchStrings.ModeBrowse).ClickAsync();
        }

        await Press(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true }));
        await Expect(editor).ToHaveCountAsync(0);

        await OpenReview();
        await OpenThread(text);
        await Press(ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView }));

        await WaitForViews();
        await Expect(editor).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ADoneComment_Should_HaveNoReopen(WorkbenchSurface surface)
    {
        await ClearComments("shifts");

        var text = Unique("Finished before this copy was published");
        var id = await SeedComment("shifts", text);

        await Server.Agent.SetStatus(AgentName, id, WorkbenchReviewStatus.Done, CancellationToken.None);

        await OpenWorkbench("shifts");
        await OpenReview();
        await Press(ListButton(WorkbenchStrings.DoneComments));
        await OpenThread(text);

        await Expect(ThreadWith(text).GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.ShowView })).ToBeVisibleAsync();

        await ExpectNoWayToChangeTheThread(ThreadWith(text));
    }

    private async Task OpenToolsIfClosed()
    {
        if (IsPhone && await Page.Locator(".tools-header").CountAsync() == 0)
        {
            await OpenTools();
        }
    }
}
