using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

[TestClass]
public class AccessibilityTests : BunitContext
{
    public AccessibilityTests()
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

    [TestMethod]
    public void ThePaneExposesAnApplicationRoleAndLabel()
    {
        var cut = Render([new BlowNode { Id = "1" }], extra: p => p.Add(c => c.AriaLabel, "Order pipeline"));

        var pane = cut.Find(".bit-blow");
        Assert.AreEqual("application", pane.GetAttribute("role"));
        Assert.AreEqual("Order pipeline", pane.GetAttribute("aria-label"));
    }

    // ---- a canvas that is only a picture (#14902) ----

    /// <summary>Every interaction off, as for a canvas used as a picture - except the one named, left on.</summary>
    private static Action<ComponentParameterCollectionBuilder<BlowCanvas>> Inert(string? except = null) => p =>
    {
        p.Add(c => c.PanOnDrag, false);
        p.Add(c => c.ZoomOnScroll, false);
        p.Add(c => c.ZoomOnPinch, false);
        p.Add(c => c.ZoomOnDoubleClick, false);
        p.Add(c => c.PreventScrolling, false);
        p.Add(c => c.NodesFocusable, false);
        p.Add(c => c.EdgesFocusable, false);
        p.Add(c => c.NodesDraggable, except == nameof(BlowCanvas.NodesDraggable));
        p.Add(c => c.NodesConnectable, except == nameof(BlowCanvas.NodesConnectable));
        p.Add(c => c.ElementsSelectable, except == nameof(BlowCanvas.ElementsSelectable));
        p.Add(c => c.ClipboardShortcutsEnabled, except == nameof(BlowCanvas.ClipboardShortcutsEnabled));
        p.Add(c => c.HistoryEnabled, except == nameof(BlowCanvas.HistoryEnabled));
    };

    [TestMethod]
    public void ACanvasWithNothingForTheKeyboardIsNoTabStop()
    {
        var cut = Render([new BlowNode { Id = "1" }], extra: Inert());

        var pane = cut.Find(".bit-blow");
        Assert.IsNull(pane.GetAttribute("tabindex"));
        Assert.AreEqual("group", pane.GetAttribute("role"));
        Assert.AreEqual("Flow diagram", pane.GetAttribute("aria-label"));
        // "Press Tab to move between the nodes and edges" describes nothing when neither takes focus.
        Assert.IsNull(pane.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void AnInteractiveCanvasStaysATabStop()
    {
        foreach (var setting in new[]
                 {
                     nameof(BlowCanvas.ElementsSelectable), nameof(BlowCanvas.NodesDraggable), nameof(BlowCanvas.NodesConnectable),
                     nameof(BlowCanvas.ClipboardShortcutsEnabled), nameof(BlowCanvas.HistoryEnabled),
                 })
        {
            var cut = Render([new BlowNode { Id = "1" }], extra: Inert(setting));

            var pane = cut.Find(".bit-blow");
            Assert.AreEqual("0", pane.GetAttribute("tabindex"), setting);
            Assert.AreEqual("application", pane.GetAttribute("role"), setting);
        }
    }

    [TestMethod]
    public void ASelectionTheAppMadeKeepsThePaneATabStopForTheDeleteKeys()
    {
        var cut = Render([new BlowNode { Id = "1", Selected = true }], extra: Inert());

        var pane = cut.Find(".bit-blow");
        Assert.AreEqual("0", pane.GetAttribute("tabindex"));
        Assert.AreEqual("application", pane.GetAttribute("role"));

        // Nothing for the delete keys to do: a picture again.
        var noDelete = Render([new BlowNode { Id = "1", Selected = true }],
            extra: p => { Inert()(p); p.Add(c => c.DeleteKeyEnabled, false); });
        Assert.IsNull(noDelete.Find(".bit-blow").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void InstructionsOfTheAppsOwnStayWhenNothingTakesFocus()
    {
        var labels = new BlowAriaLabels { PaneInstructions = "Press Delete to remove the selection, Ctrl+Z to undo." };
        var cut = Render([new BlowNode { Id = "1" }], extra: p =>
        {
            Inert(nameof(BlowCanvas.HistoryEnabled))(p);
            p.Add(c => c.AriaLabels, labels);
        });

        var pane = cut.Find(".bit-blow");
        var describedBy = pane.GetAttribute("aria-describedby");
        Assert.IsNotNull(describedBy);
        Assert.AreEqual(labels.PaneInstructions, cut.Find($"#{describedBy}").TextContent);
    }

    [TestMethod]
    public void TheAppsOwnTabIndexRoleAndLabelWin()
    {
        var cut = Render([new BlowNode { Id = "1" }], extra: p => p
            .AddUnmatched("tabindex", "-1")
            .AddUnmatched("role", "img")
            .AddUnmatched("aria-label", "Architecture"));

        var pane = cut.Find(".bit-blow");
        Assert.AreEqual("-1", pane.GetAttribute("tabindex"));
        Assert.AreEqual("img", pane.GetAttribute("role"));
        Assert.AreEqual("Architecture", pane.GetAttribute("aria-label"));

        // And the other way around: a picture an app wants reachable from the keyboard after all.
        var forced = Render([new BlowNode { Id = "1" }], extra: p => { Inert()(p); p.AddUnmatched("tabindex", "0"); });
        Assert.AreEqual("0", forced.Find(".bit-blow").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void ThePaneTellsTheStylesheetWhatADragOnItDoes()
    {
        static string[] ClassesOf(IRenderedComponent<BlowCanvas> cut) => cut.Find(".bit-blow").ClassList.ToArray();

        // The default: a drag pans, so the pane takes every touch and shows the grab cursor.
        var panning = ClassesOf(Render([new BlowNode { Id = "1" }]));
        CollectionAssert.Contains(panning, "bit-blow-pane-pan");
        CollectionAssert.DoesNotContain(panning, "bit-blow-page-scroll");
        CollectionAssert.DoesNotContain(panning, "bit-blow-nodes-locked");

        // A drag that draws a selection box is still the canvas's touch, but grabs nothing.
        var selecting = ClassesOf(Render([new BlowNode { Id = "1" }], extra: p => p.Add(c => c.SelectionOnDrag, true)));
        CollectionAssert.DoesNotContain(selecting, "bit-blow-pane-pan");
        CollectionAssert.DoesNotContain(selecting, "bit-blow-page-scroll");

        // Neither: the page scrolls under a finger; the pinch stays the canvas's while it zooms on one.
        var pinching = ClassesOf(Render([new BlowNode { Id = "1" }], extra: p => p.Add(c => c.PanOnDrag, false)));
        CollectionAssert.DoesNotContain(pinching, "bit-blow-pane-pan");
        CollectionAssert.Contains(pinching, "bit-blow-page-scroll");
        CollectionAssert.DoesNotContain(pinching, "bit-blow-page-zoom");

        // A picture: the page scrolls and zooms, and no node offers to be dragged.
        var picture = ClassesOf(Render([new BlowNode { Id = "1" }], extra: Inert()));
        CollectionAssert.DoesNotContain(picture, "bit-blow-pane-pan");
        CollectionAssert.Contains(picture, "bit-blow-page-scroll");
        CollectionAssert.Contains(picture, "bit-blow-page-zoom");
        CollectionAssert.Contains(picture, "bit-blow-nodes-locked");

        // Panning with the middle button only leaves the left one - a finger - with nothing to do on the pane, but the
        // middle button's drag still grabs it.
        var middle = ClassesOf(Render([new BlowNode { Id = "1" }], extra: p => p.Add(c => c.PanButtons, [1])));
        CollectionAssert.Contains(middle, "bit-blow-pane-pan");
        CollectionAssert.Contains(middle, "bit-blow-page-scroll");
    }

    [TestMethod]
    public void HoldingThePanKeyShowsThatADragNowPans()
    {
        var cut = Render([new BlowNode { Id = "1" }], extra: p => p.Add(c => c.SelectionOnDrag, true));
        var pane = cut.Find(".bit-blow");
        Assert.IsFalse(pane.ClassList.Contains("bit-blow-pane-pan"));

        pane.KeyDown(new KeyboardEventArgs { Key = " " });
        Assert.IsTrue(cut.Find(".bit-blow").ClassList.Contains("bit-blow-pane-pan"));

        cut.Find(".bit-blow").KeyUp(new KeyboardEventArgs { Key = " " });
        Assert.IsFalse(cut.Find(".bit-blow").ClassList.Contains("bit-blow-pane-pan"));
    }

    [TestMethod]
    public void ANodeOnlyItsDragHandleDragsSaysSo()
    {
        var cut = Render([new BlowNode { Id = "1", UseCustomDragHandle = true }, new BlowNode { Id = "2" }]);

        var nodes = cut.FindAll(".bit-blow-node");
        Assert.IsTrue(nodes[0].ClassList.Contains("custom-drag-handle"));
        Assert.IsFalse(nodes[1].ClassList.Contains("custom-drag-handle"));
    }

    [TestMethod]
    public void NodesAndEdgesCarryAccessibleNames()
    {
        var nodes = new List<BlowNode> { new() { Id = "1", Label = "Start" }, new() { Id = "2", AriaLabel = "The end" } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "1", Target = "2" } };
        var cut = Render(nodes, edges);

        var rendered = cut.FindAll(".bit-blow-node");
        Assert.AreEqual("Start", rendered[0].GetAttribute("aria-label"));
        Assert.AreEqual("The end", rendered[1].GetAttribute("aria-label"));
        // aria-selected is not allowed on role="group"; selection is announced through the live
        // region instead, which is what a screen reader user actually hears.
        Assert.IsNull(rendered[0].GetAttribute("aria-selected"));

        // The nodes an edge joins are named as they are announced, not by their ids.
        var edge = cut.Find(".bit-blow-edge");
        Assert.AreEqual("Edge from Start to The end", edge.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void ABlankLabelNamesNothingSoTheIdDoes()
    {
        // An empty label is a node drawn without text, not one called nothing: a screen reader would read a blank.
        var nodes = new List<BlowNode> { new() { Id = "a", Label = "" }, new() { Id = "b", Label = "  ", AriaLabel = "" } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Label = " " } };
        var cut = Render(nodes, edges);

        var rendered = cut.FindAll(".bit-blow-node");
        Assert.AreEqual("Node a", rendered[0].GetAttribute("aria-label"));
        Assert.AreEqual("Node b", rendered[1].GetAttribute("aria-label"));
        Assert.AreEqual("Edge from a to b", cut.Find(".bit-blow-edge").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void AnEdgeLabelBecomesItsAccessibleName()
    {
        var cut = Render(
            [new BlowNode { Id = "1" }, new BlowNode { Id = "2" }],
            [new BlowEdge { Id = "e", Source = "1", Target = "2", Label = "approved" }]);

        Assert.AreEqual("approved", cut.Find(".bit-blow-edge").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void NodesAndEdgesAreTabbableUnlessOptedOut()
    {
        var nodes = new List<BlowNode> { new() { Id = "1" }, new() { Id = "2", Focusable = false } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "1", Target = "2" } };
        var cut = Render(nodes, edges);

        var rendered = cut.FindAll(".bit-blow-node");
        Assert.AreEqual("0", rendered[0].GetAttribute("tabindex"));
        Assert.IsNull(rendered[1].GetAttribute("tabindex"));
        Assert.AreEqual("0", cut.Find(".bit-blow-edge").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void DisablingKeyboardA11yRemovesEverythingFromTheTabOrder()
    {
        var cut = Render(
            [new BlowNode { Id = "1" }, new BlowNode { Id = "2" }],
            [new BlowEdge { Id = "e", Source = "1", Target = "2" }],
            p => p.Add(c => c.DisableKeyboardA11y, true));

        foreach (var n in cut.FindAll(".bit-blow-node")) Assert.IsNull(n.GetAttribute("tabindex"));
        Assert.IsNull(cut.Find(".bit-blow-edge").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void EdgesFocusableFalseKeepsNodesTabbableButNotEdges()
    {
        var cut = Render(
            [new BlowNode { Id = "1" }, new BlowNode { Id = "2" }],
            [new BlowEdge { Id = "e", Source = "1", Target = "2" }],
            p => p.Add(c => c.EdgesFocusable, false));

        Assert.AreEqual("0", cut.FindAll(".bit-blow-node")[0].GetAttribute("tabindex"));
        Assert.IsNull(cut.Find(".bit-blow-edge").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void PressingEnterOnAFocusedNodeSelectsIt()
    {
        var node = new BlowNode { Id = "1", Label = "Start" };
        var cut = Render([node]);

        cut.Find(".bit-blow-node").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(node.Selected);
    }

    [TestMethod]
    public void PressingSpaceOnAFocusedEdgeSelectsIt()
    {
        var edge = new BlowEdge { Id = "e", Source = "1", Target = "2" };
        var cut = Render([new BlowNode { Id = "1" }, new BlowNode { Id = "2" }], [edge]);

        cut.Find(".bit-blow-edge").KeyDown(new KeyboardEventArgs { Key = " " });

        Assert.IsTrue(edge.Selected);
    }

    [TestMethod]
    public void ShiftEnterTogglesANodeWithinTheExistingSelection()
    {
        var a = new BlowNode { Id = "a" };
        var b = new BlowNode { Id = "b" };
        var cut = Render([a, b]);

        cut.FindAll(".bit-blow-node")[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });
        cut.FindAll(".bit-blow-node")[1].KeyDown(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });
        Assert.IsTrue(a.Selected);
        Assert.IsTrue(b.Selected);

        // A second Shift+Enter on the same node deselects it and leaves the rest alone.
        cut.FindAll(".bit-blow-node")[1].KeyDown(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });
        Assert.IsTrue(a.Selected);
        Assert.IsFalse(b.Selected);
    }

    [TestMethod]
    public void AKeyOtherThanEnterOrSpaceDoesNotSelect()
    {
        var node = new BlowNode { Id = "1" };
        var cut = Render([node]);

        cut.Find(".bit-blow-node").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.IsFalse(node.Selected);
    }

    [TestMethod]
    public void TheLiveRegionAnnouncesKeyboardActivation()
    {
        var cut = Render([new BlowNode { Id = "1", Label = "Start" }]);

        cut.Find(".bit-blow-node").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Contains("Start selected", cut.Find(".bit-blow-a11y-live").TextContent);
    }

    [TestMethod]
    public void TheLiveRegionIsPoliteAndAtomic()
    {
        var cut = Render([new BlowNode { Id = "1" }]);

        var live = cut.Find(".bit-blow-a11y-live");
        Assert.AreEqual("polite", live.GetAttribute("aria-live"));
        Assert.AreEqual("true", live.GetAttribute("aria-atomic"));
        Assert.AreEqual("status", live.GetAttribute("role"));
    }

    [TestMethod]
    public void ArrowKeysMoveTheSelectedNodesAndAnnounceTheMove()
    {
        var node = new BlowNode { Id = "1", Label = "Start", Selected = true };
        var cut = Render([node]);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual(5, node.Position.X);
        Assert.Contains("moved", cut.Find(".bit-blow-a11y-live").TextContent);
    }

    [TestMethod]
    public void ArrowKeysSkipNodesThatCannotBeDragged()
    {
        var node = new BlowNode { Id = "1", Selected = true, Draggable = false };
        var cut = Render([node]);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual(0, node.Position.X);
    }

    [TestMethod]
    public void ShiftArrowMovesFurtherThanArrowAlone()
    {
        var slow = new BlowNode { Id = "1", Selected = true };
        var fast = new BlowNode { Id = "1", Selected = true };

        Render([slow]).Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Render([fast]).Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });

        Assert.IsTrue(fast.Position.Y > slow.Position.Y);
    }

    [TestMethod]
    public void EscapeClearsTheSelection()
    {
        var node = new BlowNode { Id = "1", Selected = true };
        var cut = Render([node]);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(node.Selected);
    }

    [TestMethod]
    public void CtrlASelectsEverything()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var cut = Render(nodes);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });

        foreach (var n in nodes) Assert.IsTrue(n.Selected);
    }

    [TestMethod]
    public void CtrlACanBeTurnedOff()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" } };
        var cut = Render(nodes, extra: p => p.Add(c => c.SelectAllKeyEnabled, false));

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });

        Assert.IsFalse(nodes[0].Selected);
    }

    [TestMethod]
    public void DeleteRemovesTheSelectionAndCanBeDisabled()
    {
        var enabled = new List<BlowNode> { new() { Id = "a", Selected = true } };
        Render(enabled).Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "Delete" });
        Assert.IsEmpty(enabled);

        var disabled = new List<BlowNode> { new() { Id = "a", Selected = true } };
        Render(disabled, extra: p => p.Add(c => c.DeleteKeyEnabled, false))
            .Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "Delete" });
        Assert.ContainsSingle(disabled);
    }
}
