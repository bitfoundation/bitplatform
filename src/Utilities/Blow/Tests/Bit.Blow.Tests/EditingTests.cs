using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// The editing layer: typed change notifications for every edit, transactions, labelled and structurally shared
/// history, grouping and stacking order.
/// </summary>
[TestClass]
public class EditingTests : BunitContext
{
    public EditingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private readonly List<BlowGraphChange> _changes = [];

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge>? edges = null,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            p.Add(c => c.HistoryEnabled, true);
            p.Add(c => c.OnGraphChanged, change => _changes.Add(change));
            extra?.Invoke(p);
        });

    private static List<BlowNode> ThreeNodes() =>
    [
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
        new() { Id = "b", Position = new(200, 0), Width = 100, Height = 40 },
        new() { Id = "c", Position = new(0, 200), Width = 100, Height = 40 },
    ];

    // ---- change notifications ----

    [TestMethod]
    public void ADragRaisesOneMovedChangeWhenItEnds()
    {
        var nodes = ThreeNodes();
        var cut = Render(nodes);
        var pane = cut.Find(".bit-blow");

        cut.FindAll(".bit-blow-node")[1].PointerDown(new PointerEventArgs { Button = 0, PointerId = 1, ClientX = 210, ClientY = 10 });
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 230, ClientY = 30 });
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 260, ClientY = 50 });
        Assert.IsEmpty(_changes, "a change was raised mid-drag");
        pane.PointerUp(new PointerEventArgs { PointerId = 1, ClientX = 260, ClientY = 50 });

        var change = _changes.Single();
        Assert.AreEqual(BlowChangeKind.Moved, change.Kind);
        Assert.AreEqual(BlowChangeOrigin.User, change.Origin);
        Assert.AreEqual("move", change.Label);
        Assert.AreSame(nodes[1], change.Nodes.Single());
        Assert.AreEqual("move", cut.Instance.History!.UndoLabel);
    }

    [TestMethod]
    public async Task ConnectingDeletingAndPastingEachRaiseTheirChange()
    {
        var nodes = ThreeNodes();
        var edges = new List<BlowEdge>();
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });
        var connected = _changes[^1];
        Assert.AreEqual(BlowChangeKind.Connected, connected.Kind);
        Assert.AreSame(edges.Single(), connected.Edges.Single());

        nodes[0].Selected = true;
        await cut.InvokeAsync(() => cut.Instance.DuplicateSelectionAsync());
        var duplicated = _changes[^1];
        Assert.AreEqual(BlowChangeKind.Added, duplicated.Kind);
        Assert.AreEqual("duplicate", duplicated.Label);
        Assert.AreEqual(BlowChangeOrigin.Api, duplicated.Origin);

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(nodes: [nodes[0]]));
        var deleted = _changes[^1];
        Assert.AreEqual(BlowChangeKind.Removed, deleted.Kind);
        Assert.AreEqual("a", deleted.RemovedNodes.Single().Id);
        Assert.AreEqual(1, deleted.RemovedEdges.Count, "the edge attached to the node went with it");
        Assert.IsEmpty(edges);
    }

    [TestMethod]
    public async Task KeyboardEditsAreTheUsers()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        var cut = Render(nodes, extra: p => p.Add(c => c.ClipboardShortcutsEnabled, true));

        await cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
        await cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = "d", CtrlKey = true });

        Assert.AreEqual(BlowChangeOrigin.User, _changes[0].Origin);
        Assert.AreEqual(BlowChangeKind.Moved, _changes[0].Kind);
        Assert.AreEqual(BlowChangeOrigin.User, _changes[1].Origin);
        Assert.AreEqual("duplicate", _changes[1].Label);
    }

    [TestMethod]
    public async Task UndoAndRedoAreReportedAsHistoryReplacingTheGraph()
    {
        var nodes = ThreeNodes();
        var cut = Render(nodes);
        await cut.InvokeAsync(() => cut.Instance.AddNodes(new BlowNode { Id = "d" }));

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        var undo = _changes[^1];
        Assert.AreEqual(BlowChangeKind.Replaced, undo.Kind);
        Assert.AreEqual(BlowChangeOrigin.History, undo.Origin);
        Assert.AreEqual("undo", undo.Label);
        Assert.IsTrue(undo.RemovedNodes.Any(n => n.Id == "d"));
        Assert.AreEqual("add", cut.Instance.History!.RedoLabel);
    }

    [TestMethod]
    public async Task HistoryLabelsReachTheHistoryState()
    {
        var states = new List<BlowHistoryState>();
        var cut = Render(ThreeNodes(), extra: p => p.Add(c => c.OnHistoryChanged, s => states.Add(s)));

        await cut.InvokeAsync(() => cut.Instance.AddNodes(new BlowNode { Id = "d" }));

        Assert.AreEqual("add", states[^1].UndoLabel);
        Assert.IsNull(states[^1].RedoLabel);
    }

    [TestMethod]
    public async Task MovingTheSelectionFromCodeIsOneAnnouncedStepLikeAnArrowKey()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = nodes[1].Selected = true;
        nodes[1].Draggable = false;
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.MoveSelectionAsync(30, -10));

        Assert.AreEqual(new BlowXYPosition(30, -10), nodes[0].Position);
        Assert.AreEqual(new BlowXYPosition(200, 0), nodes[1].Position, "a node that cannot be dragged moved");
        Assert.AreEqual(BlowChangeOrigin.Api, _changes.Single().Origin);
        Assert.AreEqual("move", cut.Instance.History!.UndoLabel);
        StringAssert.Contains(cut.Find(".bit-blow-a11y-live").TextContent, "moved");
    }

    // ---- transactions ----

    [TestMethod]
    public async Task AnEditIsOneUndoStepAndOneChangeHoweverMuchItDoes()
    {
        var nodes = ThreeNodes();
        var edges = new List<BlowEdge>();
        var changed = 0;
        var cut = Render(nodes, edges, p => p.Add(c => c.NodesChanged, _ => changed++));
        var depth = cut.Instance.History!.UndoDepth;

        await cut.InvokeAsync(() => cut.Instance.EditAsync(() =>
        {
            nodes.Add(new BlowNode { Id = "d" });
            nodes[0].Position = new(50, 50);
            cut.Instance.AddNodes(new BlowNode { Id = "e" });       // a canvas method inside the edit is part of it
            edges.Add(new BlowEdge { Id = "x", Source = "d", Target = "e" });
            nodes.RemoveAll(n => n.Id == "c");
        }, "import"));

        Assert.AreEqual(depth + 1, cut.Instance.History.UndoDepth, "the edit was not one step");
        Assert.AreEqual(1, changed, "NodesChanged was raised more than once");
        var change = _changes.Single();
        Assert.AreEqual(BlowChangeKind.Edited, change.Kind);
        Assert.AreEqual("import", change.Label);
        CollectionAssert.AreEquivalent(new[] { "d", "e" }, change.Nodes.Select(n => n.Id).ToArray());
        Assert.AreEqual("x", change.Edges.Single().Id);
        Assert.AreEqual("c", change.RemovedNodes.Single().Id);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, nodes.Select(n => n.Id).ToArray(), "undo did not take the whole edit back");
        Assert.AreEqual(new BlowXYPosition(0, 0), nodes[0].Position);
    }

    [TestMethod]
    public async Task ARemoteEditIsReportedAsSuchAndStaysOutOfTheHistory()
    {
        var nodes = ThreeNodes();
        var cut = Render(nodes);
        var depth = cut.Instance.History!.UndoDepth;

        await cut.InvokeAsync(() => cut.Instance.EditAsync(() => nodes[1].Position = new(500, 500),
            "remote-move", BlowChangeOrigin.Remote, recordHistory: false));

        Assert.AreEqual(BlowChangeOrigin.Remote, _changes.Single().Origin);
        Assert.AreEqual(depth, cut.Instance.History.UndoDepth);
    }

    // ---- structural sharing ----

    [TestMethod]
    public void AHistoryStepSharesTheCopiesOfEverythingThatDidNotChange()
    {
        var nodes = Enumerable.Range(0, 100).Select(i => new BlowNode { Id = $"n{i}", Position = new(i, 0) }).ToList();
        var history = new BlowHistory();
        history.Capture(nodes, [], BlowViewport.Identity, "start");
        var first = history.Current!;

        nodes[42].Position = new(999, 0);
        Assert.IsTrue(history.Capture(nodes, [], BlowViewport.Identity, "move"));
        var second = history.Current!;

        Assert.AreEqual(99, Enumerable.Range(0, 100).Count(i => ReferenceEquals(first.Nodes[i], second.Nodes[i])));
        Assert.AreNotSame(first.Nodes[42], second.Nodes[42]);
        Assert.AreEqual(new BlowXYPosition(42, 0), first.Nodes[42].Position, "the earlier step was changed");
        Assert.AreNotSame(nodes[0], second.Nodes[0], "the history holds a live node");
    }

    [TestMethod]
    public void SharingFindsUnchangedCopiesWhenTheListShifted()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" }, new() { Id = "c" } };
        var history = new BlowHistory();
        history.Capture(nodes, [], BlowViewport.Identity);
        var first = history.Current!;

        nodes.RemoveAt(0);
        history.Capture(nodes, [], BlowViewport.Identity);

        Assert.AreSame(first.Nodes[1], history.Current!.Nodes[0]);
        Assert.AreSame(first.Nodes[2], history.Current.Nodes[1]);
    }

    [TestMethod]
    public void AnUnchangedGraphRecordsNothing()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" } };
        var history = new BlowHistory();
        history.Capture(nodes, [], BlowViewport.Identity);

        Assert.IsFalse(history.Capture(nodes, [], BlowViewport.Identity, "noop"));
        Assert.AreEqual(0, history.UndoDepth);
    }

    // ---- grouping ----

    [TestMethod]
    public async Task GroupingKeepsEveryNodeWhereItWasOnScreen()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = nodes[1].Selected = true;
        var cut = Render(nodes);
        var before = nodes.ToDictionary(n => n.Id, n => n.GetRect());

        var group = await cut.InvokeAsync(() => cut.Instance.GroupSelectionAsync(padding: 10));

        Assert.IsNotNull(group);
        Assert.AreEqual("group", group.Type);
        Assert.AreEqual(new BlowXYPosition(-10, -10), group.Position);
        Assert.AreEqual(320, group.Width);
        Assert.AreEqual(60, group.Height);
        Assert.AreEqual(group.Id, nodes.Single(n => n.Id == "a").ParentId);
        Assert.AreEqual(new BlowXYPosition(10, 10), nodes.Single(n => n.Id == "a").Position);
        Assert.IsTrue(nodes.IndexOf(group) < nodes.FindIndex(n => n.Id == "a"), "the group is drawn in front of its members");
        Assert.IsTrue(group.Selected);
        Assert.AreEqual("group", _changes[^1].Label);

        cut.Render();
        foreach (var id in new[] { "a", "b", "c" })
            Assert.AreEqual(before[id], nodes.Single(n => n.Id == id).GetRect(), $"{id} moved on screen");
    }

    [TestMethod]
    public async Task UngroupingHandsTheChildrenBackWhereTheyAre()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "g", Type = "group", Position = new(100, 100), Width = 300, Height = 200 },
            new() { Id = "a", ParentId = "g", Position = new(20, 30), Width = 50, Height = 20 },
        };
        var edges = new List<BlowEdge> { new() { Id = "to-group", Source = "a", Target = "g" } };
        var cut = Render(nodes, edges);

        var children = await cut.InvokeAsync(() => cut.Instance.UngroupAsync(nodes[0]));

        Assert.AreEqual("a", children.Single().Id);
        Assert.IsNull(children[0].ParentId);
        Assert.AreEqual(new BlowXYPosition(120, 130), children[0].Position);
        Assert.HasCount(1, nodes);
        Assert.IsEmpty(edges, "the edge to the removed group stayed");
        Assert.AreEqual("to-group", _changes[^1].RemovedEdges.Single().Id);
    }

    [TestMethod]
    public async Task NodesInDifferentParentsAreNotGrouped()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "g", Type = "group", Width = 300, Height = 300 },
            new() { Id = "inside", ParentId = "g", Selected = true },
            new() { Id = "outside", Position = new(500, 0), Selected = true },
        };
        var cut = Render(nodes);

        Assert.IsNull(await cut.InvokeAsync(() => cut.Instance.GroupSelectionAsync()));
        Assert.HasCount(3, nodes);
    }

    // ---- stacking ----

    [TestMethod]
    public async Task BringingToFrontAndSendingToBackMoveANodeWithItsChildren()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "g", Type = "group" },
            new() { Id = "child", ParentId = "g" },
            new() { Id = "x" },
            new() { Id = "y" },
        };
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.BringToFrontAsync([nodes[0]]));
        CollectionAssert.AreEqual(new[] { "x", "y", "g", "child" }, nodes.Select(n => n.Id).ToArray());

        await cut.InvokeAsync(() => cut.Instance.SendToBackAsync([nodes.Single(n => n.Id == "y")]));
        CollectionAssert.AreEqual(new[] { "y", "x", "g", "child" }, nodes.Select(n => n.Id).ToArray());
        Assert.AreEqual("order", _changes[^1].Label);
    }
}
