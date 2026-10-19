using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for the canvas's own undo/redo: which moments it records as a step, that stepping
/// through them does not itself count as editing, and that a restored step stays restorable.
/// </summary>
[TestClass]
public class BuiltInHistoryTests : BunitContext
{
    public BuiltInHistoryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge>? edges = null,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            p.Add(c => c.HistoryEnabled, true);
            extra?.Invoke(p);
        });

    private static List<BlowNode> TwoNodes() =>
    [
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
        new() { Id = "b", Position = new(300, 0), Width = 100, Height = 40 },
    ];

    private static void DragNode(
        IRenderedComponent<BlowCanvas> cut, (double X, double Y) to, int nodeIndex = 0)
    {
        cut.FindAll(".bit-blow-node")[nodeIndex]
            .PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        var pane = cut.Find(".bit-blow");
        pane.PointerMove(new PointerEventArgs { ClientX = to.X, ClientY = to.Y });
        pane.PointerUp(new PointerEventArgs { ClientX = to.X, ClientY = to.Y });
    }

    // ---- opting in ----

    [TestMethod]
    public void ThereIsNoHistoryUnlessItIsAskedFor()
    {
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, TwoNodes()));

        Assert.IsNull(cut.Instance.History);
        Assert.IsFalse(cut.Instance.CanUndo);
    }

    [TestMethod]
    public void TurningItOnRecordsWhatIsAlreadyOnScreenAsTheStartingPoint()
    {
        var cut = Render(TwoNodes());

        Assert.IsNotNull(cut.Instance.History);
        Assert.IsFalse(cut.Instance.CanUndo);
        Assert.AreEqual(2, cut.Instance.History!.Current!.Nodes.Count);
    }

    [TestMethod]
    public async Task UndoingWithNothingRecordedDoesNothing()
    {
        var cut = Render(TwoNodes());

        Assert.IsFalse(await cut.InvokeAsync(() => cut.Instance.UndoAsync()));
    }

    // ---- what counts as a step ----

    [TestMethod]
    public async Task AFinishedDragIsOneStep()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);

        DragNode(cut, (60, 40));
        Assert.IsTrue(cut.Instance.CanUndo);
        Assert.AreEqual(1, cut.Instance.History!.UndoDepth);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        Assert.AreEqual(new BlowXYPosition(0, 0), nodes[0].Position);
    }

    [TestMethod]
    public void ADragIsOneStepAndNotOnePerPointerMove()
    {
        var cut = Render(TwoNodes());

        cut.FindAll(".bit-blow-node")[0]
            .PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        var pane = cut.Find(".bit-blow");
        for (var i = 1; i <= 20; i++)
            pane.PointerMove(new PointerEventArgs { ClientX = i * 5, ClientY = 0 });
        pane.PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 0 });

        Assert.AreEqual(1, cut.Instance.History!.UndoDepth);
    }

    [TestMethod]
    public async Task ConnectingIsAStep()
    {
        var edges = new List<BlowEdge>();
        var cut = Render(TwoNodes(), edges);

        await cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });

        Assert.ContainsSingle(edges);
        Assert.IsTrue(await cut.InvokeAsync(() => cut.Instance.UndoAsync()));
        Assert.IsEmpty(edges);
    }

    [TestMethod]
    public async Task DeletingIsAStep()
    {
        var nodes = TwoNodes();
        nodes[0].Selected = true;
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };

        var cut = Render(nodes, edges);
        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.ContainsSingle(nodes);
        Assert.IsEmpty(edges);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        // The cascaded edge comes back with the node it was hanging off.
        Assert.AreEqual(2, nodes.Count);
        Assert.ContainsSingle(edges);
    }

    [TestMethod]
    public async Task PastingIsAStep()
    {
        var nodes = TwoNodes();
        nodes[0].Selected = true;
        var cut = Render(nodes);

        cut.Instance.Copy();
        await cut.InvokeAsync(() => cut.Instance.PasteAsync());
        Assert.AreEqual(3, nodes.Count);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        Assert.AreEqual(2, nodes.Count);
    }

    [TestMethod]
    public async Task AligningIsAStep()
    {
        var nodes = TwoNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var cut = Render(nodes);
        await cut.InvokeAsync(() => cut.Instance.AlignSelectionAsync(BlowAlignEdge.Left));
        Assert.AreEqual(0, nodes[1].Position.X);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        Assert.AreEqual(300, nodes[1].Position.X);
    }

    [TestMethod]
    public async Task NudgingWithTheKeyboardIsAStep()
    {
        var nodes = TwoNodes();
        nodes[0].Selected = true;

        var cut = Render(nodes);
        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual(5, nodes[0].Position.X);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        Assert.AreEqual(0, nodes[0].Position.X);
    }

    [TestMethod]
    public void SelectingIsNotAStep()
    {
        // Undo should take back an edit, not a change of what is highlighted.
        var nodes = TwoNodes();
        var cut = Render(nodes);

        cut.FindAll(".bit-blow-node")[0]
            .PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        cut.Find(".bit-blow").PointerUp(new PointerEventArgs { ClientX = 0, ClientY = 0 });

        Assert.IsTrue(nodes[0].Selected);
        Assert.IsFalse(cut.Instance.CanUndo);
    }

    [TestMethod]
    public void PanningIsNotAStep()
    {
        var cut = Render(TwoNodes());

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 120, ClientY = 60 });
        pane.PointerUp(new PointerEventArgs { ClientX = 120, ClientY = 60 });

        Assert.IsFalse(cut.Instance.CanUndo);
    }

    // ---- stepping through ----

    [TestMethod]
    public async Task RedoPutsBackWhatUndoTookAway()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);

        DragNode(cut, (80, 0));
        var moved = nodes[0].Position;

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.AreEqual(new BlowXYPosition(0, 0), nodes[0].Position);

        Assert.IsTrue(await cut.InvokeAsync(() => cut.Instance.RedoAsync()));
        Assert.AreEqual(moved, nodes[0].Position);
    }

    [TestMethod]
    public async Task SteppingBackAndForwardIsNotItselfRecorded()
    {
        var cut = Render(TwoNodes());

        DragNode(cut, (80, 0));
        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        await cut.InvokeAsync(() => cut.Instance.RedoAsync());

        // One edit means one step, however many times it is stepped over.
        Assert.AreEqual(1, cut.Instance.History!.UndoDepth);
        Assert.AreEqual(0, cut.Instance.History.RedoDepth);
    }

    [TestMethod]
    public async Task UndoingTwiceToTheSameStepRestoresTheSameGraphBothTimes()
    {
        // The step holds node objects, and restoring used to hand those very objects to the live
        // graph - so the next drag edited the history as well as the canvas.
        var nodes = TwoNodes();
        var cut = Render(nodes);

        DragNode(cut, (80, 0));
        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.AreEqual(new BlowXYPosition(0, 0), nodes[0].Position);

        // Edit the restored graph, then come back to the same step again.
        DragNode(cut, (250, 130));
        Assert.AreNotEqual(new BlowXYPosition(0, 0), nodes[0].Position);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        Assert.AreEqual(new BlowXYPosition(0, 0), nodes[0].Position);
    }

    [TestMethod]
    public async Task EditingAfterUndoingEndsTheRedoBranch()
    {
        var cut = Render(TwoNodes());

        DragNode(cut, (80, 0));
        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.IsTrue(cut.Instance.CanRedo);

        DragNode(cut, (0, 90));

        Assert.IsFalse(cut.Instance.CanRedo);
    }

    [TestMethod]
    public async Task UndoingLeavesTheViewportWhereTheUserPutIt()
    {
        // Coming back from an edit should not also throw away where they had scrolled to.
        var cut = Render(TwoNodes());

        DragNode(cut, (80, 0));
        await cut.InvokeAsync(() => cut.Instance.SetViewportAsync(new BlowViewport(140, -60, 1.4), 0));

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        Assert.AreEqual(new BlowViewport(140, -60, 1.4), cut.Instance.GetViewport());
    }

    // ---- keyboard ----

    [TestMethod]
    [DataRow("z", false)]
    [DataRow("Z", false)]
    public async Task ControlZStepsBack(string key, bool shift)
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);
        DragNode(cut, (80, 0));

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = key, CtrlKey = true, ShiftKey = shift });
        await Task.Yield();

        Assert.AreEqual(new BlowXYPosition(0, 0), nodes[0].Position);
    }

    [TestMethod]
    [DataRow("z", true)]
    [DataRow("y", false)]
    public async Task BothConventionsForSteppingForwardWork(string key, bool shift)
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);
        DragNode(cut, (80, 0));
        var moved = nodes[0].Position;

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = key, CtrlKey = true, ShiftKey = shift });
        await Task.Yield();

        Assert.AreEqual(moved, nodes[0].Position);
    }

    [TestMethod]
    public void TheShortcutsAreNotTakenWhenTheHistoryIsOff()
    {
        var nodes = TwoNodes();
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, new List<BlowEdge>());
        });

        DragNode(cut, (80, 0));
        var moved = nodes[0].Position;

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "z", CtrlKey = true });

        Assert.AreEqual(moved, nodes[0].Position);
    }

    // ---- reporting ----

    [TestMethod]
    public void RecordingAStepIsReported()
    {
        BlowHistoryState? state = null;
        var cut = Render(TwoNodes(), extra: p =>
            p.Add(c => c.OnHistoryChanged, (BlowHistoryState s) => state = s));

        DragNode(cut, (80, 0));

        Assert.IsNotNull(state);
        Assert.IsTrue(state!.Value.CanUndo);
        Assert.IsFalse(state.Value.CanRedo);
        Assert.AreEqual(1, state.Value.UndoDepth);
    }

    [TestMethod]
    public async Task SteppingBackIsReported()
    {
        BlowHistoryState? state = null;
        var cut = Render(TwoNodes(), extra: p =>
            p.Add(c => c.OnHistoryChanged, (BlowHistoryState s) => state = s));

        DragNode(cut, (80, 0));
        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        Assert.IsFalse(state!.Value.CanUndo);
        Assert.IsTrue(state.Value.CanRedo);
    }

    [TestMethod]
    public void CapturingByHandIsAvailableForEditsTheCanvasCannotSee()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);

        // Something the app did to the graph itself.
        nodes[0].Label = "renamed";
        cut.Instance.CaptureHistory();

        Assert.IsTrue(cut.Instance.CanUndo);
    }

    [TestMethod]
    public void CapturingAnUnchangedGraphIsNotAStep()
    {
        var cut = Render(TwoNodes());

        cut.Instance.CaptureHistory();
        cut.Instance.CaptureHistory();

        Assert.IsFalse(cut.Instance.CanUndo);
    }
}
