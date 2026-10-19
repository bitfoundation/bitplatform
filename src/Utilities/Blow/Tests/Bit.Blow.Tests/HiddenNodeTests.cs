using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for hiding nodes. An edge is drawn between two nodes, so hiding one of them leaves the
/// edge with nothing to be an edge between - it has to go too, or it hangs in mid-air pointing
/// at a gap.
/// </summary>
[TestClass]
public class HiddenNodeTests : BunitContext
{
    public HiddenNodeTests()
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
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
        new() { Id = "b", Position = new(0, 150), Width = 100, Height = 40 },
        new() { Id = "c", Position = new(0, 300), Width = 100, Height = 40 },
    ];

    private static List<BlowEdge> ChainEdges() =>
    [
        new() { Id = "a-b", Source = "a", Target = "b" },
        new() { Id = "b-c", Source = "b", Target = "c" },
    ];

    private static IEnumerable<string> RenderedEdgeIds(IRenderedComponent<BlowCanvas> cut)
        => cut.FindAll(".bit-blow-edge").Select(e => e.GetAttribute("data-id")!);

    [TestMethod]
    public void HidingANodeAlsoTakesAwayTheEdgesAttachedToIt()
    {
        var nodes = Chain();
        nodes[1].Hidden = true;

        var cut = Render(nodes, ChainEdges());

        Assert.IsEmpty(RenderedEdgeIds(cut));
    }

    [TestMethod]
    public void EdgesNotTouchingTheHiddenNodeStay()
    {
        var nodes = Chain();
        nodes[2].Hidden = true;

        var cut = Render(nodes, ChainEdges());

        Assert.AreSequenceEqual(["a-b"], RenderedEdgeIds(cut));
    }

    [TestMethod]
    public void ShowingTheNodeAgainBringsItsEdgesBack()
    {
        var nodes = Chain();
        var edges = ChainEdges();
        nodes[1].Hidden = true;

        var cut = Render(nodes, edges);
        Assert.IsEmpty(RenderedEdgeIds(cut));

        nodes[1].Hidden = false;
        cut.Render();

        Assert.AreSequenceEqual(["a-b", "b-c"], RenderedEdgeIds(cut));
    }

    [TestMethod]
    public void HidingANodeDoesNotDeleteAnything()
    {
        // Hidden is a rendering decision, not a change to the graph.
        var nodes = Chain();
        var edges = ChainEdges();
        nodes[1].Hidden = true;

        Render(nodes, edges);

        Assert.AreEqual(3, nodes.Count);
        Assert.AreEqual(2, edges.Count);
    }

    [TestMethod]
    public void AHiddenEdgeIsStillHiddenEvenWhenBothItsNodesAreVisible()
    {
        var edges = ChainEdges();
        edges[0].Hidden = true;

        var cut = Render(Chain(), edges);

        Assert.AreSequenceEqual(["b-c"], RenderedEdgeIds(cut));
    }

    [TestMethod]
    public void ASelfLoopGoesWithItsNode()
    {
        var nodes = Chain();
        nodes[0].Hidden = true;
        List<BlowEdge> edges = [new() { Id = "self", Source = "a", Target = "a" }];

        var cut = Render(nodes, edges);

        Assert.IsEmpty(RenderedEdgeIds(cut));
    }

    [TestMethod]
    public void TheEdgesOfAHiddenNodeAreGoneWithViewportCullingOnToo()
    {
        JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js")
            .Setup<BlowDomRect?>("getRect", _ => true)
            .SetResult(new BlowDomRect(0, 0, 800, 600));

        var nodes = Chain();
        nodes[1].Hidden = true;

        var cut = Render(nodes, ChainEdges(), p => p.Add(c => c.OnlyRenderVisibleElements, true));
        cut.Render();

        Assert.IsEmpty(RenderedEdgeIds(cut));
    }

    [TestMethod]
    public void AnEdgeToAHiddenNodeIsNotReportedAsAMissingEndpoint()
    {
        // The node is there; it is simply not being drawn. Reporting it as a malformed graph
        // would cry wolf every time something was collapsed.
        var nodes = Chain();
        nodes[1].Hidden = true;

        var errors = new List<BlowError>();
        Render(nodes, ChainEdges(), p =>
            p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add)));

        Assert.DoesNotContain(e => e.Code == BlowErrorCode.MissingEdgeEndpoint, errors);
    }
}
