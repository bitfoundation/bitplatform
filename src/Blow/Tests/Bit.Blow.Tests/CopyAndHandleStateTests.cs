using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for copying part of a graph, and for how a handle presents itself while a connection
/// is being dragged.
/// </summary>
[TestClass]
public class CopyAndHandleStateTests : BunitContext
{
    public CopyAndHandleStateTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge> edges,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges);
            extra?.Invoke(p);
        });

    private static List<BlowNode> Chain() =>
    [
        new() { Id = "a", Position = new(0, 0) },
        new() { Id = "b", Position = new(0, 100) },
        new() { Id = "c", Position = new(0, 200) },
    ];

    private static List<BlowEdge> ChainEdges() =>
    [
        new() { Id = "a-b", Source = "a", Target = "b" },
        new() { Id = "b-c", Source = "b", Target = "c" },
    ];

    // ---- copy ----

    [TestMethod]
    public void CopyingNodesCarriesTheEdgesBetweenThemEvenWhenTheEdgesWereNotSelected()
    {
        // Shift-clicking two nodes selects the nodes and nothing else. Copying them and getting
        // back two unconnected nodes is not what anyone means by copying part of a graph.
        var nodes = Chain();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var cut = Render(nodes, ChainEdges());
        var fragment = cut.Instance.Copy();

        Assert.AreEqual(2, fragment.Nodes.Count);
        Assert.ContainsSingle(fragment.Edges);
        Assert.AreEqual("a-b", fragment.Edges[0].Id);
    }

    [TestMethod]
    public void AnEdgeWithOneEndOutsideTheSelectionIsLeftBehind()
    {
        var nodes = Chain();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var cut = Render(nodes, ChainEdges());
        var fragment = cut.Instance.Copy();

        // b-c leaves the selection, so there would be nothing for it to connect to.
        Assert.DoesNotContain(e => e.Id == "b-c", fragment.Edges);
    }

    [TestMethod]
    public async Task PastingBringsBackAConnectedCopyRatherThanLooseNodes()
    {
        var nodes = Chain();
        var edges = ChainEdges();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var cut = Render(nodes, edges);
        cut.Instance.Copy();
        var pasted = await cut.InvokeAsync(() => cut.Instance.PasteAsync());

        Assert.AreEqual(2, pasted.Nodes.Count);
        var edge = Assert.ContainsSingle(pasted.Edges);

        // The copy is wired to the copies, not back to the originals.
        Assert.AreEqual(pasted.Nodes[0].Id, edge.Source);
        Assert.AreEqual(pasted.Nodes[1].Id, edge.Target);
        Assert.AreNotEqual("a", edge.Source);
    }

    [TestMethod]
    public async Task DuplicatingASelectionAlsoCarriesItsEdges()
    {
        var nodes = Chain();
        var edges = ChainEdges();
        foreach (var n in nodes) n.Selected = true;

        var cut = Render(nodes, edges);
        var duplicate = await cut.InvokeAsync(() => cut.Instance.DuplicateSelectionAsync());

        Assert.AreEqual(3, duplicate.Nodes.Count);
        Assert.AreEqual(2, duplicate.Edges.Count);

        // The two originals plus the two copies, all still wired up.
        Assert.AreEqual(4, edges.Count);
        Assert.AreEqual(6, nodes.Count);
    }

    [TestMethod]
    public void CopyingNothingLeavesTheClipboardAsItWas()
    {
        var nodes = Chain();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var cut = Render(nodes, ChainEdges());
        cut.Instance.Copy();

        foreach (var n in nodes) n.Selected = false;
        var second = cut.Instance.Copy();

        Assert.AreEqual(2, second.Nodes.Count);
        Assert.IsTrue(cut.Instance.CanPaste);
    }

    // ---- handle state while connecting ----

    private const string Template = "template";

    private IRenderedComponent<BlowCanvas> RenderWithHandles(List<BlowNode> nodes)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.NodeTemplate, (RenderFragment<BlowNode>)(node => b =>
            {
                b.OpenComponent<BlowHandle>(0);
                b.AddAttribute(1, nameof(BlowHandle.Type), BlowHandleType.Target);
                b.AddAttribute(2, nameof(BlowHandle.Position), BlowPosition.Top);
                b.CloseComponent();

                b.OpenComponent<BlowHandle>(3);
                b.AddAttribute(4, nameof(BlowHandle.Type), BlowHandleType.Source);
                b.AddAttribute(5, nameof(BlowHandle.Position), BlowPosition.Bottom);
                b.CloseComponent();
            }));
        });

    [TestMethod]
    public void TheHandleAConnectionIsDraggedFromIsNotMarkedRejected()
    {
        // It is not a place the drag can land, so asking "is this a valid target" answers no -
        // and marking it rejected paints the one handle the user is using as forbidden.
        List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0) }, new() { Id = "b", Position = new(0, 200) }];
        var cut = RenderWithHandles(nodes);

        cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 10, 10));
        cut.Render();

        var source = cut.FindAll(".bit-blow-handle")
            .Single(h => h.GetAttribute("data-id") == "a" && h.GetAttribute("data-handle-type") == "source");

        Assert.Contains("bit-blow-handle-connectingfrom", source.GetAttribute("class") ?? string.Empty);
        Assert.DoesNotContain("bit-blow-handle-invalid", source.GetAttribute("class") ?? string.Empty);
    }

    [TestMethod]
    public void AHandleTheConnectionCanLandOnIsStillMarkedValid()
    {
        List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0) }, new() { Id = "b", Position = new(0, 200) }];
        var cut = RenderWithHandles(nodes);

        cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 10, 10));
        cut.Render();

        var target = cut.FindAll(".bit-blow-handle")
            .Single(h => h.GetAttribute("data-id") == "b" && h.GetAttribute("data-handle-type") == "target");

        Assert.Contains("bit-blow-handle-valid", target.GetAttribute("class") ?? string.Empty);
    }

    [TestMethod]
    public void AHandleTheConnectionCannotLandOnIsStillMarkedRejected()
    {
        List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0) }, new() { Id = "b", Position = new(0, 200) }];
        var cut = RenderWithHandles(nodes);

        cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 10, 10));
        cut.Render();

        // Source to source is not a connection in strict mode.
        var other = cut.FindAll(".bit-blow-handle")
            .Single(h => h.GetAttribute("data-id") == "b" && h.GetAttribute("data-handle-type") == "source");

        Assert.Contains("bit-blow-handle-invalid", other.GetAttribute("class") ?? string.Empty);
    }

    [TestMethod]
    public void NoHandleIsMarkedAtAllWhenNothingIsBeingConnected()
    {
        List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0) }];
        var cut = RenderWithHandles(nodes);

        foreach (var handle in cut.FindAll(".bit-blow-handle"))
        {
            var css = handle.GetAttribute("class")!;
            Assert.DoesNotContain("bit-blow-handle-valid", css);
            Assert.DoesNotContain("bit-blow-handle-invalid", css);
            Assert.DoesNotContain("bit-blow-handle-connectingfrom", css);
        }
    }

    [TestMethod]
    public void TheSourceMarkIsClearedOnceTheConnectionEnds()
    {
        List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0) }];
        var cut = RenderWithHandles(nodes);

        cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 10, 10));
        cut.Find(".bit-blow").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.IsFalse(cut.Instance.IsConnectionSource("a", null, BlowHandleType.Source));
        Assert.DoesNotContain("bit-blow-handle-connectingfrom", cut.Markup);
    }
}
