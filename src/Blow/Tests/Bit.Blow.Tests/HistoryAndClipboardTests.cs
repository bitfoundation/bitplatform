using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
namespace Bit.Blow.Tests;

/// <summary>Tests for the undo/redo history and for copy/cut/paste/duplicate.</summary>
[TestClass]
public class HistoryAndClipboardTests : BunitContext
{
    public HistoryAndClipboardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge>? edges = null, Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            extra?.Invoke(p);
        });

    private static BlowSnapshot Snapshot(params (string Id, double X, double Y)[] nodes) =>
        new(nodes.Select(n => new BlowNode { Id = n.Id, Position = new(n.X, n.Y) }).ToList(), [], BlowViewport.Identity);

    // ---- history ----

    [TestMethod]
    public void AFreshHistoryHasNowhereToGo()
    {
        var history = new BlowHistory();

        Assert.IsFalse(history.CanUndo);
        Assert.IsFalse(history.CanRedo);
        Assert.IsNull(history.Current);
        Assert.IsNull(history.Undo());
        Assert.IsNull(history.Redo());
    }

    [TestMethod]
    public void TheFirstCaptureIsNotSomethingToUndoTo()
    {
        var history = new BlowHistory();
        history.Capture(Snapshot(("a", 0, 0)));

        Assert.IsFalse(history.CanUndo);
        Assert.IsNotNull(history.Current);
    }

    [TestMethod]
    public void UndoStepsBackToThePreviousState()
    {
        var history = new BlowHistory();
        var first = Snapshot(("a", 0, 0));
        history.Capture(first);
        history.Capture(Snapshot(("a", 100, 0)));

        Assert.IsTrue(history.CanUndo);
        Assert.AreSame(first, history.Undo());
    }

    [TestMethod]
    public void RedoStepsForwardAgain()
    {
        var history = new BlowHistory();
        history.Capture(Snapshot(("a", 0, 0)));
        var second = Snapshot(("a", 100, 0));
        history.Capture(second);

        history.Undo();

        Assert.IsTrue(history.CanRedo);
        Assert.AreSame(second, history.Redo());
    }

    [TestMethod]
    public void SeveralStepsCanBeWalkedBackAndForward()
    {
        var history = new BlowHistory();
        var states = new[]
        {
            Snapshot(("a", 0, 0)),
            Snapshot(("a", 10, 0)),
            Snapshot(("a", 20, 0)),
            Snapshot(("a", 30, 0)),
        };
        foreach (var s in states) history.Capture(s);

        Assert.AreSame(states[2], history.Undo());
        Assert.AreSame(states[1], history.Undo());
        Assert.AreSame(states[2], history.Redo());
        Assert.AreSame(states[3], history.Redo());
        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void ANewChangeAfterAnUndoEndsTheRedoBranch()
    {
        var history = new BlowHistory();
        history.Capture(Snapshot(("a", 0, 0)));
        history.Capture(Snapshot(("a", 100, 0)));
        history.Undo();

        Assert.IsTrue(history.CanRedo);
        history.Capture(Snapshot(("a", 50, 0)));

        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void CapturingAnUnchangedGraphRecordsNothing()
    {
        var history = new BlowHistory();
        history.Capture(Snapshot(("a", 0, 0)));

        Assert.IsFalse(history.Capture(Snapshot(("a", 0, 0))));
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void SelectionIsNotSomethingToUndo()
    {
        var history = new BlowHistory();
        history.Capture(new BlowSnapshot([new BlowNode { Id = "a" }], [], BlowViewport.Identity));

        var selected = new BlowSnapshot([new BlowNode { Id = "a", Selected = true }], [], BlowViewport.Identity);

        Assert.IsFalse(history.Capture(selected));
    }

    [TestMethod]
    public void RewiringAnEdgeCountsAsAChange()
    {
        var history = new BlowHistory();
        history.Capture(new BlowSnapshot(
            [new BlowNode { Id = "a" }, new BlowNode { Id = "b" }],
            [new BlowEdge { Id = "e", Source = "a", Target = "b" }],
            BlowViewport.Identity));

        var rewired = new BlowSnapshot(
            [new BlowNode { Id = "a" }, new BlowNode { Id = "b" }],
            [new BlowEdge { Id = "e", Source = "b", Target = "a" }],
            BlowViewport.Identity);

        Assert.IsTrue(history.Capture(rewired));
    }

    [TestMethod]
    public void AddingANodeCountsAsAChange()
    {
        var history = new BlowHistory();
        history.Capture(Snapshot(("a", 0, 0)));

        Assert.IsTrue(history.Capture(Snapshot(("a", 0, 0), ("b", 0, 0))));
    }

    [TestMethod]
    public void TheOldestStepIsDroppedOnceTheCapacityIsReached()
    {
        var history = new BlowHistory(capacity: 2);
        for (var i = 0; i < 6; i++) history.Capture(Snapshot(("a", i * 10, 0)));

        Assert.AreEqual(2, history.UndoDepth);

        history.Undo();
        history.Undo();
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void ClearForgetsEverything()
    {
        var history = new BlowHistory();
        history.Capture(Snapshot(("a", 0, 0)));
        history.Capture(Snapshot(("a", 10, 0)));

        history.Clear();

        Assert.IsFalse(history.CanUndo);
        Assert.IsFalse(history.CanRedo);
        Assert.IsNull(history.Current);
    }

    [TestMethod]
    public void ACapacityBelowOneIsRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BlowHistory(0));
    }

    [TestMethod]
    public async Task HistoryRoundTripsThroughTheCanvas()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Position = new(0, 0) } };
        var cut = Render(nodes);
        var history = new BlowHistory();

        history.Capture(cut.Instance.ToObject());
        nodes[0].Position = new BlowXYPosition(200, 300);
        history.Capture(cut.Instance.ToObject());

        var previous = history.Undo();
        Assert.IsNotNull(previous);
        await cut.InvokeAsync(() => cut.Instance.RestoreAsync(previous!));

        Assert.AreEqual(new BlowXYPosition(0, 0), cut.Instance.GetNode("a")!.Position);
    }

    // ---- clipboard: copy ----

    [TestMethod]
    public void CopyingTakesDetachedCopies()
    {
        var node = new BlowNode { Id = "a", Position = new(10, 20), Label = "original" };

        var fragment = BlowClipboard.Copy([node], []);
        node.Label = "changed";

        Assert.AreEqual("original", fragment.Nodes[0].Label);
        Assert.AreNotSame(node, fragment.Nodes[0]);
    }

    [TestMethod]
    public void OnlyEdgesInsideTheSelectionComeAlong()
    {
        var a = new BlowNode { Id = "a" };
        var b = new BlowNode { Id = "b" };
        var edges = new List<BlowEdge>
        {
            new() { Id = "inside", Source = "a", Target = "b" },
            new() { Id = "leaving", Source = "a", Target = "outside" },
        };

        var fragment = BlowClipboard.Copy([a, b], edges);

        var carried = Assert.ContainsSingle(fragment.Edges);
        Assert.AreEqual("inside", carried.Id);
    }

    [TestMethod]
    public void CopiesComeBackUnselected()
    {
        var node = new BlowNode { Id = "a", Selected = true };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "a", Selected = true } };

        var fragment = BlowClipboard.Copy([node], edges);

        Assert.IsFalse(fragment.Nodes[0].Selected);
        Assert.IsFalse(fragment.Edges[0].Selected);
    }

    [TestMethod]
    public void AChildWhoseParentWasLeftBehindBecomesARoot()
    {
        // The canvas resolves AbsolutePosition during layout; here it is set directly to stand
        // in for a child that has been laid out inside a parent at (100, 100).
        var child = new BlowNode { Id = "c", ParentId = "p", Position = new(20, 30) };
        child.AbsolutePosition = new BlowXYPosition(120, 130);

        var fragment = BlowClipboard.Copy([child], []);

        // Its position was parent-relative; without the parent it has to become absolute.
        Assert.IsNull(fragment.Nodes[0].ParentId);
        Assert.AreEqual(new BlowXYPosition(120, 130), fragment.Nodes[0].Position);
    }

    [TestMethod]
    public void AChildCopiedWithItsParentStaysRelative()
    {
        var parent = new BlowNode { Id = "p", Position = new(100, 100) };
        var child = new BlowNode { Id = "c", ParentId = "p", Position = new(20, 30) };

        var fragment = BlowClipboard.Copy([parent, child], []);

        var copiedChild = fragment.Nodes.First(n => n.Id == "c");
        Assert.AreEqual("p", copiedChild.ParentId);
        Assert.AreEqual(new BlowXYPosition(20, 30), copiedChild.Position);
    }

    [TestMethod]
    public void CopyingNothingGivesAnEmptyFragment()
    {
        Assert.IsTrue(BlowClipboard.Copy([], []).IsEmpty);
    }

    // ---- clipboard: paste ----

    [TestMethod]
    public void PastingMintsNewIds()
    {
        var fragment = new BlowFragment([new BlowNode { Id = "a" }], []);

        var pasted = BlowClipboard.Paste(fragment);

        Assert.AreEqual("a-copy", pasted.Nodes[0].Id);
    }

    [TestMethod]
    public void PastingAvoidsIdsAlreadyInUse()
    {
        var fragment = new BlowFragment([new BlowNode { Id = "a" }], []);

        var pasted = BlowClipboard.Paste(fragment, new BlowPasteOptions { ExistingIds = ["a", "a-copy"] });

        Assert.AreEqual("a-copy-2", pasted.Nodes[0].Id);
    }

    [TestMethod]
    public void PastingTheSameFragmentTwiceDoesNotCollide()
    {
        var fragment = new BlowFragment([new BlowNode { Id = "a" }], []);

        var first = BlowClipboard.Paste(fragment);
        var second = BlowClipboard.Paste(fragment, new BlowPasteOptions
        {
            ExistingIds = first.Nodes.Select(n => n.Id),
        });

        Assert.AreNotEqual(first.Nodes[0].Id, second.Nodes[0].Id);
    }

    [TestMethod]
    public void PastedEdgesPointAtTheCopiesNotTheOriginals()
    {
        var fragment = new BlowFragment(
            [new BlowNode { Id = "a" }, new BlowNode { Id = "b" }],
            [new BlowEdge { Id = "e", Source = "a", Target = "b" }]);

        var pasted = BlowClipboard.Paste(fragment);

        Assert.AreEqual(pasted.Nodes[0].Id, pasted.Edges[0].Source);
        Assert.AreEqual(pasted.Nodes[1].Id, pasted.Edges[0].Target);
    }

    [TestMethod]
    public void PastedHandlesSurviveTheRewiring()
    {
        var fragment = new BlowFragment(
            [new BlowNode { Id = "a" }, new BlowNode { Id = "b" }],
            [new BlowEdge { Id = "e", Source = "a", Target = "b", SourceHandle = "out", TargetHandle = "in" }]);

        var pasted = BlowClipboard.Paste(fragment);

        Assert.AreEqual("out", pasted.Edges[0].SourceHandle);
        Assert.AreEqual("in", pasted.Edges[0].TargetHandle);
    }

    [TestMethod]
    public void APastedChildFollowsItsPastedParent()
    {
        var fragment = new BlowFragment(
            [
                new BlowNode { Id = "p", Position = new(100, 100) },
                new BlowNode { Id = "c", ParentId = "p", Position = new(20, 30) },
            ],
            []);

        var pasted = BlowClipboard.Paste(fragment);

        var parent = pasted.Nodes.First(n => n.Id == "p-copy");
        var child = pasted.Nodes.First(n => n.Id == "c-copy");

        Assert.AreEqual(parent.Id, child.ParentId);
        // The child's position is relative to the parent, which already moved; offsetting the
        // child as well would push it out of the parent.
        Assert.AreEqual(new BlowXYPosition(20, 30), child.Position);
        Assert.AreEqual(new BlowXYPosition(124, 124), parent.Position);
    }

    [TestMethod]
    public void ThePasteOffsetShiftsRootNodes()
    {
        var fragment = new BlowFragment([new BlowNode { Id = "a", Position = new(10, 20) }], []);

        var pasted = BlowClipboard.Paste(fragment, new BlowPasteOptions { Offset = new BlowXYPosition(5, 7) });

        Assert.AreEqual(new BlowXYPosition(15, 27), pasted.Nodes[0].Position);
    }

    [TestMethod]
    public void PastedElementsComeBackSelected()
    {
        var fragment = new BlowFragment(
            [new BlowNode { Id = "a" }, new BlowNode { Id = "b" }],
            [new BlowEdge { Id = "e", Source = "a", Target = "b" }]);

        var pasted = BlowClipboard.Paste(fragment);

        foreach (var n in pasted.Nodes) Assert.IsTrue(n.Selected);
        foreach (var e in pasted.Edges) Assert.IsTrue(e.Selected);
    }

    [TestMethod]
    public void SelectingOnPasteCanBeDeclined()
    {
        var fragment = new BlowFragment([new BlowNode { Id = "a" }], []);

        var pasted = BlowClipboard.Paste(fragment, new BlowPasteOptions { Select = false });

        Assert.IsFalse(pasted.Nodes[0].Selected);
    }

    [TestMethod]
    public void ACustomIdFactoryIsUsed()
    {
        var fragment = new BlowFragment([new BlowNode { Id = "a" }], []);

        var pasted = BlowClipboard.Paste(fragment, new BlowPasteOptions
        {
            IdFactory = (id, attempt) => $"{id}#{attempt}",
        });

        Assert.AreEqual("a#0", pasted.Nodes[0].Id);
    }

    [TestMethod]
    public void PastingAnEmptyFragmentGivesNothing()
    {
        Assert.IsTrue(BlowClipboard.Paste(BlowFragment.Empty).IsEmpty);
    }

    [TestMethod]
    public void EachPasteIsANewSetOfInstances()
    {
        var fragment = new BlowFragment([new BlowNode { Id = "a" }], []);

        var first = BlowClipboard.Paste(fragment);
        var second = BlowClipboard.Paste(fragment);

        Assert.AreNotSame(first.Nodes[0], second.Nodes[0]);
        Assert.AreNotSame(fragment.Nodes[0], first.Nodes[0]);
    }

    // ---- clipboard on the canvas ----

    [TestMethod]
    public async Task CopyAndPasteAddToTheGraph()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Selected = true },
            new() { Id = "b", Position = new(0, 200), Selected = true },
        };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Selected = true } };
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.Copy());
        await cut.InvokeAsync(() => cut.Instance.PasteAsync());

        Assert.AreEqual(4, nodes.Count);
        Assert.AreEqual(2, edges.Count);
    }

    [TestMethod]
    public async Task ThePasteBecomesTheSelection()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Position = new(0, 0), Selected = true } };
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.Copy());
        await cut.InvokeAsync(() => cut.Instance.PasteAsync());

        Assert.IsFalse(nodes[0].Selected);
        Assert.IsTrue(nodes[1].Selected);
    }

    [TestMethod]
    public async Task CuttingCopiesThenDeletes()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Selected = true },
            new() { Id = "b", Position = new(0, 200) },
        };
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.CutAsync());

        Assert.ContainsSingle(nodes);
        Assert.IsTrue(cut.Instance.CanPaste);

        await cut.InvokeAsync(() => cut.Instance.PasteAsync());
        Assert.AreEqual(2, nodes.Count);
    }

    [TestMethod]
    public async Task DuplicateLeavesTheClipboardAlone()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Selected = true },
            new() { Id = "b", Position = new(0, 200) },
        };
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.Copy());          // clipboard holds "a"

        nodes[0].Selected = false;
        nodes[1].Selected = true;
        await cut.InvokeAsync(() => cut.Instance.DuplicateSelectionAsync());   // duplicates "b"

        Assert.Contains(n => n.Id == "b-copy", nodes);

        // Ctrl+D must not have overwritten what was on the clipboard.
        await cut.InvokeAsync(() => cut.Instance.PasteAsync());
        Assert.Contains(n => n.Id == "a-copy", nodes);
    }

    [TestMethod]
    public async Task PastingWithNothingCopiedIsANoOp()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" } };
        var cut = Render(nodes);

        Assert.IsFalse(cut.Instance.CanPaste);
        await cut.InvokeAsync(() => cut.Instance.PasteAsync());

        Assert.ContainsSingle(nodes);
    }

    [TestMethod]
    public async Task PasteRaisesTheCallbackWithWhatWasAdded()
    {
        BlowFragment? seen = null;
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true } };
        var cut = Render(nodes, extra: p =>
            p.Add(c => c.OnPaste, EventCallback.Factory.Create<BlowFragment>(this, f => seen = f)));

        await cut.InvokeAsync(() => cut.Instance.Copy());
        await cut.InvokeAsync(() => cut.Instance.PasteAsync());

        Assert.IsNotNull(seen);
        Assert.AreEqual("a-copy", seen!.Nodes[0].Id);
    }

    [TestMethod]
    public void ClipboardShortcutsAreOffByDefault()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true } };
        var cut = Render(nodes);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "c", CtrlKey = true });

        Assert.IsFalse(cut.Instance.CanPaste);
    }

    [TestMethod]
    public void ClipboardShortcutsCopyAndPasteWhenEnabled()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true } };
        var cut = Render(nodes, extra: p => p.Add(c => c.ClipboardShortcutsEnabled, true));

        var pane = cut.Find(".bit-blow");
        pane.KeyDown(new KeyboardEventArgs { Key = "c", CtrlKey = true });
        pane.KeyDown(new KeyboardEventArgs { Key = "v", CtrlKey = true });

        Assert.AreEqual(2, nodes.Count);
        Assert.AreEqual("a-copy", nodes[1].Id);
    }

    [TestMethod]
    public void TheDuplicateShortcutCopiesInOneStep()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true } };
        var cut = Render(nodes, extra: p => p.Add(c => c.ClipboardShortcutsEnabled, true));

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "d", CtrlKey = true });

        Assert.AreEqual(2, nodes.Count);
    }

    [TestMethod]
    public void TheCutShortcutRemovesTheSelection()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Selected = true },
            new() { Id = "b" },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.ClipboardShortcutsEnabled, true));

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "x", CtrlKey = true });

        Assert.ContainsSingle(nodes);
        Assert.IsTrue(cut.Instance.CanPaste);
    }
}
