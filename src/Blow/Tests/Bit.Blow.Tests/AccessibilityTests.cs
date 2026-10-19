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

        var edge = cut.Find(".bit-blow-edge");
        Assert.AreEqual("Edge from 1 to 2", edge.GetAttribute("aria-label"));
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
