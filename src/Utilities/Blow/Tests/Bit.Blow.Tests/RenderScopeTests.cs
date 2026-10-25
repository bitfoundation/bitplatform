using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// What each kind of render re-renders. A pan or a zoom touches only the canvas and the add-ons that draw from the
/// viewport; a drag re-renders the dragged node and its edges; anything else - selection, the lists, the host
/// re-rendering - re-renders everything, so nothing a template draws can go stale.
/// </summary>
[TestClass]
public class RenderScopeTests : BunitContext
{
    public RenderScopeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<BlowNode> Nodes() =>
    [
        new() { Id = "a", Position = new(0, 0) },
        new() { Id = "b", Position = new(0, 200) },
        new() { Id = "c", Position = new(300, 0) },
    ];

    private static List<BlowEdge> Edges() =>
    [
        new() { Id = "ab", Source = "a", Target = "b" },
        new() { Id = "bc", Source = "b", Target = "c" },
    ];

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge> edges,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges);
            extra?.Invoke(p);
        });

    private static int[] Counts<T>(IRenderedComponent<BlowCanvas> cut) where T : IComponent
        => cut.FindComponents<T>().Select(c => c.RenderCount).ToArray();

    /// <summary>
    /// Which components rendered between two counts. A count is bUnit's, which includes the renders of everything
    /// inside the component (the default node, its handles), so only zero and non-zero mean anything here.
    /// </summary>
    private static bool[] Rendered(int[] before, int[] after) => after.Zip(before, (a, b) => a > b).ToArray();

    [TestMethod]
    public async Task APanRendersNoNodeEdgeOrHandle()
    {
        var cut = Render(Nodes(), Edges());
        var nodes = Counts<BlowNodeRenderer>(cut);
        var edges = Counts<BlowEdgeRenderer>(cut);
        var handles = Counts<BlowHandle>(cut);

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(40, 30, 1.5)));

        CollectionAssert.AreEqual(nodes, Counts<BlowNodeRenderer>(cut));
        CollectionAssert.AreEqual(edges, Counts<BlowEdgeRenderer>(cut));
        CollectionAssert.AreEqual(handles, Counts<BlowHandle>(cut));
        StringAssert.Contains(cut.Find(".bit-blow-viewport").GetAttribute("style"), "translate(40px, 30px) scale(1.5)");
    }

    [TestMethod]
    public async Task AddOnsDrawnFromTheViewportFollowAPan()
    {
        var cut = Render(Nodes(), Edges(), p => p.Add(c => c.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<BlowBackground>(0);
            b.CloseComponent();
            b.OpenComponent<BlowControls>(1);
            b.AddAttribute(2, nameof(BlowControls.ShowFitView), false);
            b.CloseComponent();
            b.OpenComponent<BlowMiniMap>(3);
            b.CloseComponent();
        })));
        var background = cut.FindComponent<BlowBackground>().RenderCount;
        var minimap = cut.FindComponent<BlowMiniMap>().RenderCount;

        await cut.InvokeAsync(() => cut.Instance.ZoomTo(cut.Instance.MaxZoom));

        Assert.IsGreaterThan(background, cut.FindComponent<BlowBackground>().RenderCount, "the background did not follow the zoom");
        Assert.IsGreaterThan(minimap, cut.FindComponent<BlowMiniMap>().RenderCount, "the minimap did not follow the zoom");
        Assert.IsTrue(cut.Find(".bit-blow-controls-button").HasAttribute("disabled"), "zoom in stayed enabled at the maximum zoom");
    }

    [TestMethod]
    public void ADragFrameRendersTheDraggedNodeAndItsEdgesOnly()
    {
        var cut = Render(Nodes(), Edges());
        var before = Counts<BlowNodeRenderer>(cut);
        var edgesBefore = Counts<BlowEdgeRenderer>(cut);
        var pane = cut.Find(".bit-blow");

        cut.FindAll(".bit-blow-node")[2].PointerDown(new PointerEventArgs { Button = 0, PointerId = 1, ClientX = 300, ClientY = 0 });
        var started = Counts<BlowNodeRenderer>(cut);
        var edgesStarted = Counts<BlowEdgeRenderer>(cut);
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 340, ClientY = 20 });
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 380, ClientY = 40 });

        // Starting the drag selects the node: that re-renders everything, once.
        CollectionAssert.AreEqual(new[] { true, true, true }, Rendered(before, started));
        CollectionAssert.AreEqual(new[] { true, true }, Rendered(edgesBefore, edgesStarted));
        // Each move re-renders the node being moved, and the edge attached to it - nothing else.
        CollectionAssert.AreEqual(new[] { false, false, true }, Rendered(started, Counts<BlowNodeRenderer>(cut)));
        CollectionAssert.AreEqual(new[] { false, true }, Rendered(edgesStarted, Counts<BlowEdgeRenderer>(cut)));
        Assert.AreEqual(new BlowXYPosition(380, 40), cut.Instance.GetNode("c")!.Position);
    }

    [TestMethod]
    public void LettingGoOfADraggedNodeRendersThatNodeOnly()
    {
        var stops = 0;
        var cut = Render(Nodes(), Edges(), p => p.Add(c => c.OnNodeDragStop, _ => stops++));
        var pane = cut.Find(".bit-blow");
        cut.FindAll(".bit-blow-node")[2].PointerDown(new PointerEventArgs { Button = 0, PointerId = 1, ClientX = 300, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 340, ClientY = 20 });
        var dragged = Counts<BlowNodeRenderer>(cut);
        var edges = Counts<BlowEdgeRenderer>(cut);
        Assert.Contains("dragging", cut.FindAll(".bit-blow-node")[2].ClassList);

        pane.PointerUp(new PointerEventArgs { PointerId = 1, ClientX = 340, ClientY = 20 });

        // The node shows it is no longer being dragged; nothing else in the graph changed, so nothing else renders.
        Assert.AreEqual(1, stops);
        CollectionAssert.AreEqual(new[] { false, false, true }, Rendered(dragged, Counts<BlowNodeRenderer>(cut)));
        CollectionAssert.AreEqual(new[] { false, false }, Rendered(edges, Counts<BlowEdgeRenderer>(cut)));
        Assert.DoesNotContain("dragging", cut.FindAll(".bit-blow-node")[2].ClassList);
        Assert.AreEqual(new BlowXYPosition(340, 20), cut.Instance.GetNode("c")!.Position);
    }

    [TestMethod]
    public async Task HandlesShowTheVerdictWhenAConnectionStartsAndForgetItWhenItEnds()
    {
        var cut = Render(Nodes(), Edges());

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
        Assert.Contains("bit-blow-handle-valid", cut.Find(".bit-blow-node[data-id='c'] .bit-blow-handle.target").ClassName!);

        await cut.InvokeAsync(() => cut.Instance.CompleteConnection("c", null, BlowHandleType.Target));
        Assert.DoesNotContain("bit-blow-handle-valid", cut.Find(".bit-blow-node[data-id='c'] .bit-blow-handle.target").ClassName!);
    }

    [TestMethod]
    public async Task MovingTheConnectionLineRendersNoNode()
    {
        var cut = Render(Nodes(), Edges());
        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 0, 40));
        var before = Counts<BlowNodeRenderer>(cut);
        var handles = Counts<BlowHandle>(cut);

        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 150, ClientY = 150 });

        CollectionAssert.AreEqual(before, Counts<BlowNodeRenderer>(cut));
        CollectionAssert.AreEqual(handles, Counts<BlowHandle>(cut));
        Assert.HasCount(1, cut.FindAll(".bit-blow-connectionline"));
    }

    [TestMethod]
    public async Task SelectionReachesTheResizerAndTheToolbarInsideATemplate()
    {
        var cut = Render(Nodes(), [], p => p.Add(c => c.NodeTemplate, node => b =>
        {
            b.OpenComponent<BlowNodeResizer>(0);
            b.CloseComponent();
            b.OpenComponent<BlowNodeToolbar>(1);
            b.AddAttribute(2, nameof(BlowNodeToolbar.ChildContent), (RenderFragment)(t => t.AddContent(0, "tools")));
            b.CloseComponent();
            b.AddContent(3, node.Label);
        }));
        Assert.IsEmpty(cut.FindAll(".bit-blow-node-toolbar"));

        await cut.InvokeAsync(() => cut.Instance.SelectAllAsync());

        Assert.HasCount(3, cut.FindAll(".bit-blow-node-toolbar"));
        Assert.IsNotEmpty(cut.FindAll(".bit-blow-resize-handle"));
    }

    [TestMethod]
    public async Task ANodeToolbarKeepsItsScreenSizeAcrossAZoom()
    {
        var nodes = Nodes();
        nodes[0].Selected = true;
        var cut = Render(nodes, [], p => p.Add(c => c.NodeTemplate, node => b =>
        {
            b.OpenComponent<BlowNodeToolbar>(0);
            b.CloseComponent();
        }));

        await cut.InvokeAsync(() => cut.Instance.ZoomTo(2));

        StringAssert.Contains(cut.Find(".bit-blow-node-toolbar").GetAttribute("style"), "scale(0.5)");
    }

    [TestMethod]
    public void WhenTheHostReRendersEveryTemplateIsDrawnAgain()
    {
        var caption = "first";
        var nodes = Nodes();
        RenderFragment<BlowNode> Template() => node => b => b.AddContent(0, $"{node.Id}:{caption}");
        var cut = Render(nodes, [], p => p.Add(c => c.NodeTemplate, Template()));

        // Something the template reads that the canvas cannot see changes, and the host renders again.
        caption = "second";
        cut.Render(p => p.Add(c => c.NodeTemplate, Template()));

        Assert.AreEqual("a:second", cut.Find(".bit-blow-node[data-id='a']").TextContent);
    }

    [TestMethod]
    public void RefreshDrawsEverythingAgain()
    {
        var nodes = Nodes();
        var cut = Render(nodes, []);
        var before = Counts<BlowNodeRenderer>(cut);

        cut.InvokeAsync(() => cut.Instance.Refresh(remeasure: false));

        CollectionAssert.AreEqual(new[] { true, true, true }, Rendered(before, Counts<BlowNodeRenderer>(cut)));
    }

    [TestMethod]
    public void AnAddOnOfYourOwnOnTheUnnamedContextStillFollowsEveryRender()
    {
        var cut = Render(Nodes(), [], p => p.Add(c => c.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<ZoomReadout>(0);
            b.CloseComponent();
        })));

        cut.InvokeAsync(() => cut.Instance.ZoomTo(2));

        Assert.AreEqual("zoom 2", cut.Find(".readout").TextContent);
    }

    [TestMethod]
    public async Task AViewportViewInATemplateFollowsTheZoomOnlyWhereItsSelectorChanges()
    {
        var renders = 0;
        var cut = Render([new() { Id = "a" }], [], p => p.Add(c => c.NodeTemplate, node => b =>
        {
            b.OpenComponent<BlowViewportView>(0);
            b.AddAttribute(1, nameof(BlowViewportView.Select), (Func<BlowViewport, object?>)(v => v.Zoom >= 1.5));
            b.AddAttribute(2, nameof(BlowViewportView.ChildContent), (RenderFragment<BlowViewport>)(v => t =>
            {
                renders++;
                t.AddContent(0, v.Zoom >= 1.5 ? "details" : "summary");
            }));
            b.CloseComponent();
        }));
        Assert.AreEqual("summary", cut.Find(".bit-blow-node").TextContent);
        var initial = renders;

        // Panning, and zooming without crossing the threshold, render nothing.
        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(50, 50, 1)));
        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(50, 50, 1.2)));
        Assert.AreEqual(initial, renders);

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(50, 50, 2)));
        Assert.AreEqual("details", cut.Find(".bit-blow-node").TextContent);
        Assert.AreEqual(initial + 1, renders);
    }

    /// <summary>An app's own add-on, written the simple way: the unnamed context, re-rendered on every canvas render.</summary>
    private sealed class ZoomReadout : ComponentBase
    {
        [CascadingParameter] private IBlowContext? Flow { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "readout");
            builder.AddContent(2, $"zoom {Flow?.Viewport.Zoom}");
            builder.CloseElement();
        }
    }
}
