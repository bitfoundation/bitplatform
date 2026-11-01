using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.WorkbenchTests.Features.Shell;

/// <summary>
/// The Review tab, rendered in memory against a real review session. Each test reviews a page key of its own, so the
/// comments it lists are only its own.
/// </summary>
[TestClass, TestCategory("UITest")]
public class ReviewPanelTests
{
    private static readonly WorkbenchReviewAnchor anchor = new("main .card button", "BitButton", "Save", 0.5, 0.5);

    private readonly WorkbenchPageInfo page = WorkbenchPageCatalog.Find("buildings")! with { Key = $"bunit-{Guid.NewGuid():N}" };

    private static async Task<WorkbenchReviewSession> OpenSession(BunitContext ctx)
    {
        var session = ctx.Services.GetRequiredService<WorkbenchReviewSession>();

        await session.Open("Sarah");

        return session;
    }

    private IRenderedComponent<ReviewPanel> Render(BunitContext ctx, Action<ComponentParameterCollectionBuilder<ReviewPanel>>? parameters = null)
    {
        return ctx.Render<ReviewPanel>(p =>
        {
            p.Add(x => x.Page, page).Add(x => x.ReviewerName, "Sarah");
            parameters?.Invoke(p);
        });
    }

    [TestMethod]
    public async Task CtrlEnter_Should_SendTheCommentAndPlainEnterShouldNot()
    {
        await using var ctx = WorkbenchBunit.Create();
        await OpenSession(ctx);

        List<string> sent = [];
        var cut = Render(ctx, p => p.Add(x => x.PendingAnchor, anchor).Add(x => x.OnSend, (string text) => sent.Add(text)));

        var composer = cut.Find($"textarea[aria-label='{WorkbenchStrings.NewComment}']");

        composer.Input("Make the button blue");
        composer.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsEmpty(sent, "Enter alone starts a new line.");

        composer.KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        cut.WaitForAssertion(() => Assert.AreSequenceEqual(new[] { "Make the button blue" }, sent));

        cut.Find($"textarea[aria-label='{WorkbenchStrings.NewComment}']").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        Assert.HasCount(1, sent, "The draft is cleared once it is sent, so a second Ctrl+Enter sends nothing.");
    }

    [TestMethod]
    public async Task CmdEnter_Should_SendTheCommentOnAMac()
    {
        await using var ctx = WorkbenchBunit.Create();
        await OpenSession(ctx);

        string? sent = null;
        var cut = Render(ctx, p => p.Add(x => x.PendingAnchor, anchor).Add(x => x.OnSend, (string text) => sent = text));

        var composer = cut.Find($"textarea[aria-label='{WorkbenchStrings.NewComment}']");

        composer.Input("Bigger");
        composer.KeyDown(new KeyboardEventArgs { Key = "Enter", MetaKey = true });

        cut.WaitForAssertion(() => Assert.AreEqual("Bigger", sent));
    }

    [TestMethod]
    public async Task TheSendButton_Should_WaitForText()
    {
        await using var ctx = WorkbenchBunit.Create();
        await OpenSession(ctx);

        string? sent = null;
        var cut = Render(ctx, p => p.Add(x => x.PendingAnchor, anchor).Add(x => x.OnSend, (string text) => sent = text));

        var send = () => cut.FindAll("button").Single(b => b.TextContent.Trim() == WorkbenchStrings.Send);

        Assert.AreEqual("true", send().GetAttribute("aria-disabled"));

        cut.Find($"textarea[aria-label='{WorkbenchStrings.NewComment}']").Input("   ");
        Assert.AreEqual("true", send().GetAttribute("aria-disabled"), "Blank text isn't a comment.");

        cut.Find($"textarea[aria-label='{WorkbenchStrings.NewComment}']").Input("Fine");
        send().Click();

        cut.WaitForAssertion(() => Assert.AreEqual("Fine", sent));
    }

    [TestMethod]
    public async Task CtrlEnter_Should_SendAReplyOnTheSelectedComment()
    {
        await using var ctx = WorkbenchBunit.Create();
        var session = await OpenSession(ctx);
        var id = await session.AddThread(page.Key, "", anchor, "Sarah", "First thought", null);

        var cut = Render(ctx, p => p.Add(x => x.SelectedThreadId, id));

        var reply = cut.Find($"textarea[aria-label='{WorkbenchStrings.Reply}']");

        reply.Input("Second thought");
        reply.KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        cut.WaitForAssertion(() => Assert.HasCount(2, session.Threads.Single(t => t.Id == id).Messages));
        cut.WaitForAssertion(() => Assert.Contains("Second thought", cut.Markup));
    }

    [TestMethod]
    public async Task Delete_Should_AskOnceMore()
    {
        await using var ctx = WorkbenchBunit.Create();
        var session = await OpenSession(ctx);
        var id = await session.AddThread(page.Key, "", anchor, "Sarah", "Remove me", null);

        var cut = Render(ctx, p => p.Add(x => x.SelectedThreadId, id));

        cut.Find($"button[aria-label='{WorkbenchStrings.DeleteComment}']").Click();

        Assert.HasCount(1, session.ThreadsOf(page.Key));
        Assert.Contains(WorkbenchStrings.DeleteCommentConfirm, cut.Markup);

        cut.Find($"button[aria-label='{WorkbenchStrings.DeleteComment}']").Click();

        cut.WaitForAssertion(() => Assert.IsEmpty(session.ThreadsOf(page.Key)));
    }

    [TestMethod]
    public async Task Resolving_Should_MoveACommentUnderDoneAndReopeningShouldBringItBack()
    {
        await using var ctx = WorkbenchBunit.Create();
        var session = await OpenSession(ctx);
        var id = await session.AddThread(page.Key, "", anchor, "Sarah", "Fix the spacing", null);
        await session.AddThread(page.Key, "", anchor, "Sarah", "Another one", null);

        var cut = Render(ctx, p => p.Add(x => x.SelectedThreadId, id));

        Assert.Contains(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.OpenComments, 2), cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == WorkbenchStrings.Resolve).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.AreEqual(WorkbenchReviewStatus.Done, session.Threads.Single(t => t.Id == id).Status);
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.OpenComments, 1), cut.Markup);
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.DoneComments, 1), cut.Markup);
        });

        cut.Render(p => p.Add(x => x.SelectedThreadId, id));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == WorkbenchStrings.Reopen).Click();

        cut.WaitForAssertion(() => Assert.AreEqual(WorkbenchReviewStatus.Open, session.Threads.Single(t => t.Id == id).Status));
    }

    [TestMethod]
    public async Task ThePageStatus_Should_BeSetFromTheReviewTab()
    {
        await using var ctx = WorkbenchBunit.Create();
        var session = await OpenSession(ctx);

        var cut = Render(ctx);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == WorkbenchStrings.PageStatusApproved).Click();

        cut.WaitForAssertion(() =>
        {
            var status = session.PageOf(page.Key)?.Status;

            Assert.AreEqual(WorkbenchPageStatus.Approved, status);
            Assert.AreEqual("true", cut.FindAll("button").Single(b => b.TextContent.Trim() == WorkbenchStrings.PageStatusApproved).GetAttribute("aria-pressed"));
            Assert.Contains("Set by Sarah", cut.Markup);
        });
    }

    [TestMethod]
    public async Task NoComments_Should_SayHowToLeaveOne()
    {
        await using var ctx = WorkbenchBunit.Create();
        await OpenSession(ctx);

        var cut = Render(ctx);

        Assert.Contains(WorkbenchStrings.NoComments, cut.Markup);
        Assert.Contains(WorkbenchStrings.NoAgentListening, cut.Markup);
    }

    [TestMethod]
    public async Task ARemoteViewer_Should_SeeTheReviewButNoWayToChangeIt()
    {
        await using var ctx = WorkbenchBunit.Create(isLocal: false);
        await OpenSession(ctx);

        var cut = Render(ctx, p => p.Add(x => x.PendingAnchor, anchor));

        Assert.Contains(WorkbenchStrings.LiveReviewTitle, cut.Markup);
        Assert.Contains(WorkbenchStrings.AgentOffline, cut.Markup);
        Assert.IsEmpty(cut.FindAll("textarea"), "No composer and no reply box.");
        Assert.IsEmpty(cut.FindAll("button").Where(b => b.TextContent.Trim() == WorkbenchStrings.PageStatusApproved), "The page status is shown, not set.");
        Assert.Contains(WorkbenchStrings.PageStatusDraft, cut.Markup);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task TheAgentsProgress_Should_ShowAsItHappens()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;

        await using var ctx = WorkbenchBunit.Create(server: server);
        var session = await OpenSession(ctx);
        var id = (await session.AddThread(page.Key, "", anchor, "Sarah", "Align the icons", null))!;

        await WorkbenchTestReviewer.WaitFor(() => server.Agent.GetThreads(CancellationToken.None), threads => threads.Any(t => t.Id == id));

        var cut = Render(ctx, p => p.Add(x => x.SelectedThreadId, id));

        await server.Agent.SetStatus("Claude Code", id, WorkbenchReviewStatus.Working, CancellationToken.None);
        await server.Agent.SetTasks("Claude Code", id, [new("Find the icons", true), new("Align them", false)], CancellationToken.None);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.AgentWorkingOn, "Claude Code", 1), cut.Markup);
            Assert.Contains(WorkbenchStrings.ReviewStatusWorking, cut.Markup);
            Assert.Contains("Find the icons", cut.Markup);
        }, TimeSpan.FromSeconds(15));

        await server.Agent.Reply("Claude Code", id, "Aligned them on the baseline.", CancellationToken.None);

        cut.WaitForAssertion(() => Assert.Contains("Aligned them on the baseline.", cut.Markup), TimeSpan.FromSeconds(15));
    }
}
