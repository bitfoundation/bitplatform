using Bit.Bcrdt;

namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// The review document is shared by every browser and the agent, and kept on disk, so what it reads back and what it
/// trims decide what each of them sees.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchReviewDocumentTests
{
    private static readonly WorkbenchReviewAnchor anchor = new("div.card > button", "BitButton", "Save", 0.25, 0.75);

    private readonly BcrdtDocument document = new();

    private BcrdtMap Threads => document.GetMap("threads");

    private BcrdtMap Flows => document.GetMap("flows");

    private void Edit(Action edit) => document.Transact(_ => edit());

    private WorkbenchReviewThread Thread(string id) => WorkbenchReviewDocument.ReadThreads(Threads).Single(t => t.Id == id);

    [TestMethod]
    public void AThread_Should_ReadBackAsItWasPinned()
    {
        Edit(() => WorkbenchReviewDocument.AddThread(Threads, "a", "buildings", "?theme=dark", anchor, "Sarah", "Make it bigger", hasState: true));

        var thread = Thread("a");

        Assert.AreEqual("buildings", thread.PageKey);
        Assert.AreEqual("?theme=dark", thread.Query);
        Assert.AreEqual(WorkbenchThemeMode.Dark, thread.Scenario.Theme);
        Assert.AreEqual(anchor, thread.Anchor);
        Assert.AreEqual(WorkbenchReviewStatus.Open, thread.Status);
        Assert.IsTrue(thread.HasState);
        Assert.IsGreaterThan(0, thread.Sequence);
        Assert.HasCount(1, thread.Messages);
        Assert.IsFalse(thread.Messages[0].FromAgent);
        Assert.AreEqual(("Sarah", "Make it bigger"), (thread.Messages[0].Author, thread.Messages[0].Text));
        Assert.IsEmpty(thread.Tasks);
    }

    [TestMethod]
    public void Threads_Should_BeNumberedPerPageInTheOrderTheyWereMade()
    {
        Edit(() =>
        {
            WorkbenchReviewDocument.AddThread(Threads, "a", "theme", "", anchor, "R", "first", false);
            WorkbenchReviewDocument.AddThread(Threads, "b", "buildings", "", anchor, "R", "second", false);
            WorkbenchReviewDocument.AddThread(Threads, "c", "theme", "", anchor, "R", "third", false);
        });

        Assert.AreEqual(1, Thread("a").Number);
        Assert.AreEqual(1, Thread("b").Number);
        Assert.AreEqual(2, Thread("c").Number);
    }

    [TestMethod]
    public void TheTexts_Should_BeTrimmedToTheirLimits()
    {
        var longText = $"  {new string('t', WorkbenchReviewDocument.MaxTextLength + 100)}  ";
        var longName = new string('n', WorkbenchReviewDocument.MaxNameLength + 10);

        Edit(() => WorkbenchReviewDocument.AddThread(Threads, "a", "theme", "", anchor with { Text = new string('x', 500), X = -3, Y = 9 }, longName, longText, false));

        var thread = Thread("a");

        Assert.AreEqual(WorkbenchReviewDocument.MaxTextLength, thread.Messages[0].Text.Length);
        Assert.AreEqual(WorkbenchReviewDocument.MaxNameLength, thread.Messages[0].Author.Length);
        Assert.AreEqual(120, thread.Anchor.Text!.Length);
        Assert.AreEqual((0d, 1d), (thread.Anchor.X, thread.Anchor.Y), "The spot stays on the element's box.");
    }

    [TestMethod]
    public void AReviewersReply_Should_ReopenAThreadAndRaiseItsSequence()
    {
        Edit(() => WorkbenchReviewDocument.AddThread(Threads, "a", "theme", "", anchor, "R", "first", false));
        Edit(() => WorkbenchReviewDocument.SetStatus(Threads, "a", WorkbenchReviewStatus.Done, fromAgent: true));

        var before = Thread("a").Sequence;

        Assert.IsTrue(WorkbenchReviewDocument.AddMessage(Threads, "a", fromAgent: false, "R", "Not quite"));

        var thread = Thread("a");

        Assert.AreEqual(WorkbenchReviewStatus.Open, thread.Status);
        Assert.IsGreaterThan(before, thread.Sequence);
        Assert.HasCount(2, thread.Messages);
    }

    [TestMethod]
    public void TheAgentsReply_Should_NeitherReopenNorWakeTheAgentItself()
    {
        Edit(() => WorkbenchReviewDocument.AddThread(Threads, "a", "theme", "", anchor, "R", "first", false));
        Edit(() => WorkbenchReviewDocument.SetStatus(Threads, "a", WorkbenchReviewStatus.Done, fromAgent: true));

        var before = Thread("a").Sequence;

        Edit(() => WorkbenchReviewDocument.AddMessage(Threads, "a", fromAgent: true, "Claude", "Done, it's bigger now"));

        var thread = Thread("a");

        Assert.AreEqual(WorkbenchReviewStatus.Done, thread.Status);
        Assert.AreEqual(before, thread.Sequence);
        Assert.IsTrue(thread.Messages[^1].FromAgent);
    }

    [TestMethod]
    public void AReviewerReopeningAThread_Should_RaiseItsSequence()
    {
        Edit(() => WorkbenchReviewDocument.AddThread(Threads, "a", "theme", "", anchor, "R", "first", false));

        var created = Thread("a").Sequence;

        Edit(() => WorkbenchReviewDocument.SetStatus(Threads, "a", WorkbenchReviewStatus.Working, fromAgent: true));
        Assert.AreEqual(created, Thread("a").Sequence, "The agent's own status changes don't wake it.");

        Edit(() => WorkbenchReviewDocument.SetStatus(Threads, "a", WorkbenchReviewStatus.Open, fromAgent: false));
        Assert.IsGreaterThan(created, Thread("a").Sequence);
    }

    [TestMethod]
    public void AnEditOfAThreadThatIsGone_Should_ReturnFalse()
    {
        Assert.IsFalse(WorkbenchReviewDocument.AddMessage(Threads, "missing", false, "R", "text"));
        Assert.IsFalse(WorkbenchReviewDocument.SetStatus(Threads, "missing", WorkbenchReviewStatus.Done, true));
        Assert.IsFalse(WorkbenchReviewDocument.SetTasks(Threads, "missing", []));
    }

    [TestMethod]
    public void Tasks_Should_BeReplacedAsAWholeAndCapped()
    {
        Edit(() => WorkbenchReviewDocument.AddThread(Threads, "a", "theme", "", anchor, "R", "first", false));

        Edit(() => WorkbenchReviewDocument.SetTasks(Threads, "a", Enumerable.Range(1, 30).Select(i => new WorkbenchReviewTask($" step {i} ", i <= 2))));

        var tasks = Thread("a").Tasks;

        Assert.HasCount(WorkbenchReviewDocument.MaxTasks, tasks);
        Assert.AreEqual(new WorkbenchReviewTask("step 1", true), tasks[0]);
        Assert.IsFalse(tasks[2].Done);

        Edit(() => WorkbenchReviewDocument.SetTasks(Threads, "a", [new("only", false)]));

        Assert.HasCount(1, Thread("a").Tasks);
    }

    [TestMethod]
    public void APageStatus_Should_RecordWhoSetItAndWhen()
    {
        var pages = document.GetMap("pages");

        Edit(() => WorkbenchReviewDocument.SetPageStatus(pages, "buildings", WorkbenchPageStatus.Approved, "Sarah"));

        var page = WorkbenchReviewDocument.ReadPages(pages)["buildings"];

        Assert.AreEqual(WorkbenchPageStatus.Approved, page.Status);
        Assert.AreEqual("Sarah", page.By);
        Assert.IsLessThan(TimeSpan.FromMinutes(1), DateTimeOffset.UtcNow - page.At!.Value);
    }

    [TestMethod]
    public void TheAgent_Should_CountAsOnlineForTwoMinutes()
    {
        var agent = document.GetMap("agent");

        Assert.IsNull(WorkbenchReviewDocument.ReadAgent(agent), "No agent has called yet.");

        Edit(() => WorkbenchReviewDocument.SetAgent(agent, "Claude Code", "a"));

        var info = WorkbenchReviewDocument.ReadAgent(agent)!;

        Assert.AreEqual(("Claude Code", "a"), (info.Name, info.ThreadId));
        Assert.IsTrue(info.IsActive);
        Assert.IsFalse((info with { At = DateTimeOffset.UtcNow.AddMinutes(-3) }).IsActive);
    }

    [TestMethod]
    public void NextSequence_Should_OnlyGrowEvenWhenCalledAtOnce()
    {
        var values = new ConcurrentBag<long>();

        Parallel.For(0, 2_000, _ => values.Add(WorkbenchReviewDocument.NextSequence()));

        Assert.HasCount(2_000, values.Distinct());
    }

    [TestMethod]
    public void AFlow_Should_KeepWhenItWasMadeWhenItIsDrawnAgain()
    {
        WorkbenchFlowScreen[] screens = [new("s1", "buildings", "", "The list", false, 0, 0), new("s2", "buildings", "?theme=dark", null, true, 600, 0)];
        WorkbenchFlowLink[] links = [new("l1", "s1", "s2", "Tap Add", new(0.1, 0.2, 0.3, 0.05))];

        Edit(() => WorkbenchReviewDocument.SaveFlow(Flows, "f", "Adding a building", screens, links));

        var first = WorkbenchReviewDocument.ReadFlows(Flows).Single();

        Assert.AreEqual("Adding a building", first.Name);
        Assert.AreSequenceEqual(screens, first.Screens);
        Assert.AreSequenceEqual(links, first.Links);

        Edit(() => WorkbenchReviewDocument.SaveFlow(Flows, "f", "Renamed", screens.Take(1), []));

        var second = WorkbenchReviewDocument.ReadFlows(Flows).Single();

        Assert.AreEqual(first.CreatedAt, second.CreatedAt);
        Assert.AreEqual("Renamed", second.Name);
        Assert.HasCount(1, second.Screens);
        Assert.IsEmpty(second.Links);
    }

    [TestMethod]
    public void AFlow_Should_HoldAtMostItsLimitOfScreensAndLinks()
    {
        var screens = Enumerable.Range(0, 30).Select(i => new WorkbenchFlowScreen($"s{i}", "theme", "", null, false, i, 0)).ToArray();
        var links = Enumerable.Range(0, 60).Select(i => new WorkbenchFlowLink($"l{i}", "s0", "s1", null, null)).ToArray();

        Edit(() => WorkbenchReviewDocument.SaveFlow(Flows, "f", "Big", screens, links));

        var flow = WorkbenchReviewDocument.ReadFlows(Flows).Single();

        Assert.HasCount(WorkbenchReviewDocument.MaxScreens, flow.Screens);
        Assert.HasCount(WorkbenchReviewDocument.MaxLinks, flow.Links);
        Assert.IsFalse(WorkbenchReviewDocument.AddScreen(Flows, "f", new("extra", "theme", "", null, false, 0, 0), null));
        Assert.IsFalse(WorkbenchReviewDocument.AddLink(Flows, "f", new("extra", "s0", "s1", null, null)));
    }

    [TestMethod]
    public void RemovingAScreen_Should_RemoveEveryArrowThatTouchesIt()
    {
        WorkbenchFlowScreen[] screens = [new("a", "theme", "", null, false, 0, 0), new("b", "theme", "", null, false, 1, 0), new("c", "theme", "", null, false, 2, 0)];
        WorkbenchFlowLink[] links = [new("ab", "a", "b", null, null), new("bc", "b", "c", null, null), new("ac", "a", "c", null, null)];

        Edit(() => WorkbenchReviewDocument.SaveFlow(Flows, "f", "Flow", screens, links));

        Assert.IsTrue(WorkbenchReviewDocument.RemoveScreen(Flows, "f", "b"));

        var flow = WorkbenchReviewDocument.ReadFlows(Flows).Single();

        Assert.AreSequenceEqual(new[] { "a", "c" }, flow.Screens.Select(s => s.Id).ToArray());
        Assert.AreSequenceEqual(new[] { "ac" }, flow.Links.Select(l => l.Id).ToArray());
        Assert.IsFalse(WorkbenchReviewDocument.RemoveScreen(Flows, "f", "b"));
        Assert.IsFalse(WorkbenchReviewDocument.RemoveScreen(Flows, "missing", "a"));
    }

    [TestMethod]
    public void TheBoardsEdits_Should_ChangeOnlyWhatTheyName()
    {
        WorkbenchFlowScreen[] screens = [new("a", "theme", "", null, false, 0, 0), new("b", "theme", "", null, false, 900, 0)];

        Edit(() => WorkbenchReviewDocument.SaveFlow(Flows, "f", "Flow", screens, [new("ab", "a", "b", "Tap", new(0.1, 0.1, 0.2, 0.2))]));

        Edit(() =>
        {
            Assert.IsTrue(WorkbenchReviewDocument.MoveScreen(Flows, "f", "b", 1234.6, -20.2));
            Assert.IsTrue(WorkbenchReviewDocument.SetLinkLabel(Flows, "f", "ab", "   "));
            Assert.IsTrue(WorkbenchReviewDocument.SetLinkSpot(Flows, "f", "ab", null));
            Assert.IsTrue(WorkbenchReviewDocument.RenameFlow(Flows, "f", $"  {new string('n', 100)}  "));
        });

        var flow = WorkbenchReviewDocument.ReadFlows(Flows).Single();

        Assert.AreEqual((1235d, -20d), (flow.Screens[1].X, flow.Screens[1].Y));
        Assert.AreEqual((0d, 0d), (flow.Screens[0].X, flow.Screens[0].Y));
        Assert.IsNull(flow.Links[0].Label, "A blank label is no label.");
        Assert.IsNull(flow.Links[0].Spot);
        Assert.AreEqual(80, flow.Name.Length);

        Edit(() => Assert.IsTrue(WorkbenchReviewDocument.RemoveLink(Flows, "f", "ab")));

        Assert.IsEmpty(WorkbenchReviewDocument.ReadFlows(Flows).Single().Links);
        Assert.IsFalse(WorkbenchReviewDocument.MoveScreen(Flows, "f", "missing", 0, 0));
        Assert.IsFalse(WorkbenchReviewDocument.RenameFlow(Flows, "missing", "x"));
    }

    [TestMethod]
    public void AnUnreadableEntry_Should_ReadAsItsDefaults()
    {
        Edit(() =>
        {
            Threads.Set("not-a-map", "text");
            Threads.Set("bad", new BcrdtMap());
        });

        var threads = WorkbenchReviewDocument.ReadThreads(Threads);

        Assert.HasCount(1, threads);
        Assert.AreEqual(WorkbenchReviewStatus.Open, threads[0].Status);
        Assert.AreEqual("body", threads[0].Anchor.Selector);
        Assert.IsEmpty(threads[0].Messages);
    }
}
