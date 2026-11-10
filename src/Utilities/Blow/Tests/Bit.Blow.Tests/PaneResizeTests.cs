using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for the viewport helpers that work from the pane's size, called after the pane has resized but before the
/// browser has reported it - the case of an app fitting the graph from a ResizeObserver of its own - and for
/// <see cref="BlowCanvas.OnPaneResize"/>, which spares the app that observer.
/// </summary>
[TestClass]
public class PaneResizeTests : BunitContext
{
    /// <summary>
    /// The module the canvas imports. It imports once, so handlers set up on a module set up later would never be
    /// asked; the latest handler set up on this one answers.
    /// </summary>
    private readonly BunitJSModuleInterop _module;

    public PaneResizeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");
    }

    /// <summary>Makes the pane measure <paramref name="width"/> by 600 from now on.</summary>
    private void PaneIs(double width) =>
        _module.Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, 0, width, 600));

    private static List<BlowNode> ThreeNodes() =>
    [
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
        new() { Id = "b", Position = new(200, 0), Width = 100, Height = 40 },
        new() { Id = "c", Position = new(400, 0), Width = 100, Height = 40 },
    ];

    private IRenderedComponent<BlowCanvas> RenderAt(double width, List<BlowNode> nodes,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
    {
        PaneIs(width);
        return Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, []);
            extra?.Invoke(p);
        });
    }

    /// <summary>The viewport a fit with <paramref name="padding"/> gives the nodes in a pane <paramref name="width"/> by 600.</summary>
    private static BlowViewport FitFor(List<BlowNode> nodes, double width, double padding = 0.1)
        => BlowGraphUtils.GetViewportForBounds(BlowGraphUtils.GetNodesBounds(nodes), width, 600, 0.2, 2.5, padding);

    private static void AssertViewport(BlowViewport expected, BlowViewport actual)
    {
        Assert.AreEqual(expected.X, actual.X, 1e-6, "x");
        Assert.AreEqual(expected.Y, actual.Y, 1e-6, "y");
        Assert.AreEqual(expected.Zoom, actual.Zoom, 1e-6, "zoom");
    }

    [TestMethod]
    public async Task AFitMadeBeforeTheResizeIsReportedFitsTheNewSize()
    {
        var nodes = ThreeNodes();
        var cut = RenderAt(1200, nodes);
        // Guards the test: the two sizes fit the graph differently, or the assertion below proves nothing.
        Assert.AreNotEqual(FitFor(nodes, 1200), FitFor(nodes, 800));

        // The container shrank and the app fits at once, before the canvas's own observer has reported anything.
        PaneIs(800);
        await cut.InvokeAsync(() => cut.Instance.FitView());

        AssertViewport(FitFor(nodes, 800), cut.Instance.GetViewport());
        Assert.AreEqual(new BlowDimensions(800, 600), cut.Instance.PaneSize);
    }

    [TestMethod]
    public async Task AnAnimatedFitMeasuresThePaneBeforeItMoves()
    {
        var nodes = ThreeNodes();
        var cut = RenderAt(800, nodes);

        PaneIs(1200);
        await cut.InvokeAsync(() => cut.Instance.FitViewAsync(new BlowFitViewOptions { Padding = 0.2 }));

        AssertViewport(FitFor(nodes, 1200, 0.2), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task FitBoundsAndSetCenterWorkFromTheNewSizeToo()
    {
        var nodes = ThreeNodes();
        var cut = RenderAt(1200, nodes);
        var bounds = new BlowRect(0, 0, 300, 40);

        PaneIs(800);
        await cut.InvokeAsync(() => cut.Instance.FitBounds(bounds, 0.1));
        AssertViewport(BlowGraphUtils.GetViewportForBounds(bounds, 800, 600, 0.2, 2.5, 0.1), cut.Instance.GetViewport());

        PaneIs(1000);
        await cut.InvokeAsync(() => cut.Instance.SetCenter(100, 20, 1));
        AssertViewport(new BlowViewport(500 - 100, 300 - 20, 1), cut.Instance.GetViewport());

        PaneIs(600);
        await cut.InvokeAsync(() => cut.Instance.SetCenterAsync(100, 20, 1, 0));
        AssertViewport(new BlowViewport(300 - 100, 300 - 20, 1), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task AViewportMovedWhileThePaneWasBeingMeasuredIsNotFittedOver()
    {
        var nodes = ThreeNodes();
        var cut = RenderAt(1200, nodes);

        // The measurement is still on its way back when the app moves the viewport somewhere else.
        var pending = _module.Setup<BlowDomRect?>("getRect", _ => true);
        await cut.InvokeAsync(() => cut.Instance.FitView());
        var moved = new BlowViewport(10, 20, 1.5);
        await cut.InvokeAsync(() => cut.Instance.SetViewport(moved));

        await cut.InvokeAsync(() => pending.SetResult(new BlowDomRect(0, 0, 800, 600)));

        Assert.AreEqual(moved, cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task OnPaneResizeReportsANewSizeOnce()
    {
        var sizes = new List<BlowDimensions>();
        var cut = RenderAt(800, ThreeNodes(), p => p.Add(c => c.OnPaneResize, (BlowDimensions size) => sizes.Add(size)));

        // The first measurement is not a resize.
        Assert.IsEmpty(sizes);

        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved());
        Assert.IsEmpty(sizes, "the pane kept its size");

        PaneIs(1000);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved());
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved());

        CollectionAssert.AreEqual(new[] { new BlowDimensions(1000, 600) }, sizes);
    }

    [TestMethod]
    public async Task OnPaneResizeIsRaisedWhenAFitNoticesTheResizeFirst()
    {
        // The fit's measurement takes the news from the observer, which then finds the size unchanged: the event
        // has to be raised by whichever reading notices, or it would be lost.
        var sizes = new List<BlowDimensions>();
        var cut = RenderAt(800, ThreeNodes(), p => p.Add(c => c.OnPaneResize, (BlowDimensions size) => sizes.Add(size)));

        PaneIs(1000);
        await cut.InvokeAsync(() => cut.Instance.FitView());
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved());

        CollectionAssert.AreEqual(new[] { new BlowDimensions(1000, 600) }, sizes);
    }

    [TestMethod]
    public async Task FittingFromOnPaneResizeKeepsTheGraphFitted()
    {
        var nodes = ThreeNodes();
        BlowCanvas? canvas = null;
        var cut = RenderAt(800, nodes, p => p.Add(c => c.OnPaneResize, () => canvas!.FitView()));
        canvas = cut.Instance;

        PaneIs(1200);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved());

        AssertViewport(FitFor(nodes, 1200), cut.Instance.GetViewport());
    }
}
