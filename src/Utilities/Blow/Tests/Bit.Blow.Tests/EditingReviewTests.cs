using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Regressions found by the independent review of the editing layer: what an edit folds in and what it must not,
/// edits that cannot be undone, grouping with a node origin, stacking inside a group, and what the history shares.
/// </summary>
[TestClass]
public class EditingReviewTests : BunitContext
{
    public EditingReviewTests()
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

    private static List<BlowNode> TwoNodes() =>
    [
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
        new() { Id = "b", Position = new(200, 0), Width = 100, Height = 40 },
    ];

    private static Task Press(IRenderedComponent<BlowCanvas> cut, string key, bool ctrl = false)
        => cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = key, CtrlKey = ctrl });

    // ---- what an edit folds in ----

    [TestMethod]
    public async Task AnEditThatIsWaitingDoesNotSwallowAnEditMadeMeanwhile()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);
        var gate = new TaskCompletionSource();
        Task pending = Task.CompletedTask;

        // A statement block, so InvokeAsync returns once the edit is waiting rather than awaiting it.
        await cut.InvokeAsync(() =>
        {
            pending = cut.Instance.EditAsync(async () =>
            {
                await gate.Task;
                nodes[0].Position = new(9, 9);
            }, "remote", BlowChangeOrigin.Remote, recordHistory: false);
        });

        await cut.InvokeAsync(() => cut.Instance.AddNodes(new BlowNode { Id = "local" }));

        var added = _changes.Single();
        Assert.AreEqual(BlowChangeKind.Added, added.Kind, "the add made while the edit waited was folded into it");
        Assert.AreEqual(BlowChangeOrigin.Api, added.Origin);
        Assert.AreEqual("add", cut.Instance.History!.UndoLabel);

        gate.SetResult();
        await cut.InvokeAsync(() => pending);
        Assert.AreEqual(BlowChangeOrigin.Remote, _changes[^1].Origin);

        // The local add is still the user's to undo; the remote move is not.
        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.IsFalse(nodes.Any(n => n.Id == "local"));
        Assert.AreEqual(new BlowXYPosition(9, 9), nodes.Single(n => n.Id == "a").Position);
    }

    [TestMethod]
    public async Task WorkAnEditStartedButDidNotAwaitIsNotPartOfIt()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);
        var later = new TaskCompletionSource();
        Task detached = Task.CompletedTask;

        await cut.InvokeAsync(() => cut.Instance.EditAsync(() =>
        {
            detached = Later();
            nodes[0].Position = new(5, 5);
        }, "edit"));
        later.SetResult();
        await cut.InvokeAsync(() => detached);

        Assert.AreEqual(2, _changes.Count, "the work left running after the edit was swallowed");
        Assert.AreEqual("add", _changes[^1].Label);

        async Task Later()
        {
            await later.Task;
            cut.Instance.AddNodes(new BlowNode { Id = "late" });
        }
    }

    [TestMethod]
    public async Task AnEditRaisesTheListEventsOnceHoweverManyCallsItMakes()
    {
        var nodes = TwoNodes();
        var nodesChanged = 0;
        var edgesChanged = 0;
        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.NodesChanged, _ => nodesChanged++);
            p.Add(c => c.EdgesChanged, _ => edgesChanged++);
        });

        await cut.InvokeAsync(() => cut.Instance.EditAsync(async () =>
        {
            cut.Instance.AddNodes(new BlowNode { Id = "c" });
            cut.Instance.AddNodes(new BlowNode { Id = "d" });
            cut.Instance.AddEdges(new BlowEdge { Id = "c-d", Source = "c", Target = "d" });
            await cut.Instance.DeleteElementsAsync(nodes: [nodes[0]]);
        }));

        Assert.AreEqual(1, nodesChanged);
        Assert.AreEqual(1, edgesChanged);
        Assert.AreEqual(1, _changes.Count);
    }

    [TestMethod]
    public async Task LoadingInsideAnEditIsPartOfItsOneStep()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);
        var depth = cut.Instance.History!.UndoDepth;
        var json = BlowSerializer.Serialize(new BlowDocument { Nodes = [new BlowNode { Id = "loaded" }] });

        await cut.InvokeAsync(() => cut.Instance.EditAsync(async () =>
        {
            await cut.Instance.LoadJsonAsync(json);
            cut.Instance.AddNodes(new BlowNode { Id = "extra" });
        }, "import"));

        Assert.AreEqual(depth + 1, cut.Instance.History.UndoDepth);
        Assert.AreEqual("import", cut.Instance.History.UndoLabel);
    }

    [TestMethod]
    public async Task WhatAFailingEditChangedIsCommittedAndTheErrorRethrown()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => cut.InvokeAsync(() => cut.Instance.EditAsync(() =>
        {
            nodes[1].Position = new(777, 777);
            throw new InvalidOperationException("half done");
        }, "broken")));

        Assert.AreEqual("broken", _changes.Single().Label);
        Assert.AreEqual("broken", cut.Instance.History!.UndoLabel);

        // A later step must not carry the half-done edit with it.
        nodes[0].Selected = true;
        await Press(cut, "ArrowRight");
        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.AreEqual(new BlowXYPosition(777, 777), nodes.Single(n => n.Id == "b").Position);
    }

    // ---- edits that cannot be undone ----

    [TestMethod]
    public async Task AnEditKeptOutOfTheHistoryCannotBeUndoneByLaterSteps()
    {
        var nodes = TwoNodes();
        nodes[0].Selected = true;
        var cut = Render(nodes);
        var a = () => nodes.Single(n => n.Id == "a");
        var b = () => nodes.Single(n => n.Id == "b");

        await Press(cut, "ArrowRight");
        var afterFirstMove = a().Position;
        await cut.InvokeAsync(() => cut.Instance.EditAsync(() => b().Position = new(500, 500),
            "remote", BlowChangeOrigin.Remote, recordHistory: false));
        await Press(cut, "ArrowRight");

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.AreEqual(afterFirstMove, a().Position);
        Assert.AreEqual(new BlowXYPosition(500, 500), b().Position, "undo took back the remote edit");

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.AreEqual(new BlowXYPosition(0, 0), a().Position);
        Assert.AreEqual(new BlowXYPosition(500, 500), b().Position, "an older step brought the remote edit back");

        await cut.InvokeAsync(() => cut.Instance.RedoAsync());
        await cut.InvokeAsync(() => cut.Instance.RedoAsync());
        Assert.AreEqual(new BlowXYPosition(500, 500), b().Position);
    }

    [TestMethod]
    public void RebasingDropsAStepTheChangeOverwrote()
    {
        var nodes = TwoNodes();
        var history = new BlowHistory();
        history.Capture(nodes, [], BlowViewport.Identity);
        nodes[1].Position = new(1, 1);
        history.Capture(nodes, [], BlowViewport.Identity, "move b");
        nodes[0].Position = new(2, 2);
        history.Capture(nodes, [], BlowViewport.Identity, "move a");

        // The remote change puts b somewhere else: "move b" no longer changes anything.
        nodes[1].Position = new(50, 50);
        Assert.IsTrue(history.Rebase(nodes, [], BlowViewport.Identity));

        Assert.AreEqual(1, history.UndoDepth);
        Assert.AreEqual("move a", history.UndoLabel);
        var back = history.Undo()!;
        Assert.AreEqual(new BlowXYPosition(0, 0), back.Nodes[0].Position);
        Assert.AreEqual(new BlowXYPosition(50, 50), back.Nodes[1].Position);
    }

    [TestMethod]
    public void RebasingARemovalDropsTheEdgesItOrphansInOlderSteps()
    {
        var nodes = TwoNodes();
        nodes.Add(new BlowNode { Id = "c", Position = new(0, 200) });
        var edges = new List<BlowEdge> { new() { Id = "a-b", Source = "a", Target = "b" } };
        var history = new BlowHistory();
        history.Capture(nodes, edges, BlowViewport.Identity);
        nodes[2].Position = new(0, 300);
        history.Capture(nodes, edges, BlowViewport.Identity, "move c");
        edges.Clear();
        history.Capture(nodes, edges, BlowViewport.Identity, "delete");

        nodes.RemoveAt(1);
        Assert.IsTrue(history.Rebase(nodes, edges, BlowViewport.Identity));

        // Deleting the edge no longer changes anything once its node is gone, so that step went with it.
        Assert.AreEqual(1, history.UndoDepth);
        Assert.AreEqual("move c", history.UndoLabel);
        var back = history.Undo()!;
        CollectionAssert.AreEqual(new[] { "a", "c" }, back.Nodes.Select(n => n.Id).ToArray());
        Assert.IsEmpty(back.Edges, "undo would bring back an edge to a node another user removed");
    }

    // ---- origins ----

    [TestMethod]
    public async Task WhatAnAppHandlerDoesDuringAKeyboardEditIsNotTheUsers()
    {
        var nodes = TwoNodes();
        nodes[0].Selected = true;
        IRenderedComponent<BlowCanvas>? cut = null;
        cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.ClipboardShortcutsEnabled, true);
            p.Add(c => c.OnPaste, _ => cut!.Instance.AddNodes(new BlowNode { Id = "from-handler" }));
        });

        await Press(cut, "c", ctrl: true);
        await Press(cut, "v", ctrl: true);

        var paste = _changes.Single(c => c.Label == "paste");
        var add = _changes.Single(c => c.Label == "add");
        Assert.AreEqual(BlowChangeOrigin.User, paste.Origin);
        Assert.AreEqual(BlowChangeOrigin.Api, add.Origin);
    }

    // ---- grouping with an origin ----

    [TestMethod]
    public async Task GroupingAndUngroupingKeepNodesInPlaceWhateverTheNodeOrigin()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "outer", Type = "group", Position = new(300, 200), Width = 400, Height = 300 },
            new() { Id = "a", ParentId = "outer", Position = new(-100, -60), Width = 100, Height = 40, Selected = true },
            new() { Id = "b", ParentId = "outer", Position = new(80, 40), Width = 100, Height = 40, Selected = true },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.NodeOrigin, new BlowXYPosition(0.5, 0.5)));
        cut.Render();
        var before = nodes.ToDictionary(n => n.Id, n => n.GetRect());

        var group = await cut.InvokeAsync(() => cut.Instance.GroupSelectionAsync(padding: 10));
        cut.Render();
        Assert.IsNotNull(group);
        foreach (var id in new[] { "a", "b", "outer" })
            Assert.AreEqual(before[id], nodes.Single(n => n.Id == id).GetRect(), $"{id} moved when grouped");

        await cut.InvokeAsync(() => cut.Instance.UngroupAsync(group));
        cut.Render();
        foreach (var id in new[] { "a", "b", "outer" })
            Assert.AreEqual(before[id], nodes.Single(n => n.Id == id).GetRect(), $"{id} moved when ungrouped");
        Assert.AreEqual("outer", nodes.Single(n => n.Id == "a").ParentId);
    }

    [TestMethod]
    public async Task AGroupThatCannotBeDeletedIsNotDissolvedAndOneThatCanIsReportedDeleted()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "locked", Type = "group", Deletable = false, Width = 200, Height = 200 },
            new() { Id = "in-locked", ParentId = "locked" },
            new() { Id = "open", Type = "group", Position = new(300, 0), Width = 200, Height = 200 },
            new() { Id = "in-open", ParentId = "open" },
        };
        var deleted = new List<string>();
        var cut = Render(nodes, extra: p => p.Add(c => c.OnNodesDelete, list => deleted.AddRange(list.Select(n => n.Id))));

        Assert.IsEmpty(await cut.InvokeAsync(() => cut.Instance.UngroupAsync(nodes[0])));
        Assert.AreEqual("locked", nodes.Single(n => n.Id == "in-locked").ParentId);

        await cut.InvokeAsync(() => cut.Instance.UngroupAsync(nodes.Single(n => n.Id == "open")));
        CollectionAssert.AreEqual(new[] { "open" }, deleted);
    }

    // ---- stacking inside a group ----

    [TestMethod]
    public async Task AChildSentToTheBackStaysInFrontOfItsParent()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "other" },
            new() { Id = "g", Type = "group" },
            new() { Id = "x", ParentId = "g" },
            new() { Id = "y", ParentId = "g" },
        };
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.SendToBackAsync([nodes[3]]));

        CollectionAssert.AreEqual(new[] { "other", "g", "y", "x" }, nodes.Select(n => n.Id).ToArray());
    }

    // ---- what the history shares ----

    [TestMethod]
    public void AStepRemembersTheSelectionItWasTakenWith()
    {
        var nodes = TwoNodes();
        var history = new BlowHistory();
        nodes[0].Selected = true;
        history.Capture(nodes, [], BlowViewport.Identity);
        nodes[0].Position = new(10, 0);
        history.Capture(nodes, [], BlowViewport.Identity, "move a");

        nodes[0].Selected = false;
        nodes[1].Selected = true;
        nodes[1].Position = new(210, 0);
        history.Capture(nodes, [], BlowViewport.Identity, "move b");

        Assert.IsFalse(history.Current!.Nodes[0].Selected, "a shared copy still says a is selected");
        Assert.IsTrue(history.Undo()!.Nodes[0].Selected);
        Assert.IsFalse(history.Redo()!.Nodes[0].Selected);
    }

    [TestMethod]
    public void ACopyTakenBeforeANodeWasMeasuredIsNotSharedAfterwards()
    {
        var nodes = TwoNodes();
        var history = new BlowHistory();
        history.Capture(nodes, [], BlowViewport.Identity);

        nodes[0].Measured = new BlowDimensions(100, 40);
        nodes[0].Handles["Source:"] = new BlowHandleBounds("", BlowHandleType.Source, BlowPosition.Right, 100, 20);
        nodes[1].Position = new(250, 0);
        history.Capture(nodes, [], BlowViewport.Identity, "move b");

        var a = history.Current!.Nodes[0];
        Assert.AreEqual(new BlowDimensions(100, 40), a.Measured);
        Assert.HasCount(1, a.Handles);
    }
}
