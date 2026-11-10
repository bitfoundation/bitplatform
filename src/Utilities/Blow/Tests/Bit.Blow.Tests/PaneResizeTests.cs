using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

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

    [TestMethod]
    public async Task OverlappingReadingsOfANewSizeReportItOnce()
    {
        var sizes = new List<BlowDimensions>();
        var cut = RenderAt(800, ThreeNodes(), p => p.Add(c => c.OnPaneResize, (BlowDimensions size) => sizes.Add(size)));

        // Two reports of the observer's on their way at once, as a drag-resize of the window sends them, and both
        // come back with the same new size.
        var pending = _module.Setup<BlowDomRect?>("getRect", _ => true);
        var first = cut.InvokeAsync(() => cut.Instance.OnResizeObserved());
        var second = cut.InvokeAsync(() => cut.Instance.OnResizeObserved());
        await cut.InvokeAsync(() => pending.SetResult(new BlowDomRect(0, 0, 1000, 600)));
        await Task.WhenAll(first, second);

        CollectionAssert.AreEqual(new[] { new BlowDimensions(1000, 600) }, sizes);
    }

    [TestMethod]
    public async Task TheObserverHandsOverThePanesRectangleWithoutAnotherRoundTrip()
    {
        var sizes = new List<BlowDimensions>();
        var cut = RenderAt(800, ThreeNodes(), p => p.Add(c => c.OnPaneResize, (BlowDimensions size) => sizes.Add(size)));

        // A reading asked for here would never come back.
        _module.Setup<BlowDomRect?>("getRect", _ => true);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(true, new BlowDomRect(0, 0, 1000, 600)));
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(true, new BlowDomRect(0, 0, 1000, 600)));

        CollectionAssert.AreEqual(new[] { new BlowDimensions(1000, 600) }, sizes);
        Assert.AreEqual(new BlowDimensions(1000, 600), cut.Instance.PaneSize);
    }

    [TestMethod]
    public async Task AFitFromOnPaneResizeDoesNotMeasureThePaneAgain()
    {
        var nodes = ThreeNodes();
        BlowCanvas? canvas = null;
        var cut = RenderAt(800, nodes, p => p.Add(c => c.OnPaneResize, () => canvas!.FitView()));
        canvas = cut.Instance;

        var readings = 0;
        _module.Setup<BlowDomRect?>("getRect", _ => { readings++; return true; }).SetResult(new BlowDomRect(0, 0, 1200, 600));
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(true, new BlowDomRect(0, 0, 1200, 600)));

        Assert.AreEqual(0, readings);
        AssertViewport(FitFor(nodes, 1200), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task ABurstOfMovesSharesOneMeasurementOfThePane()
    {
        var cut = RenderAt(1000, ThreeNodes());

        var readings = 0;
        var pending = _module.Setup<BlowDomRect?>("getRect", _ => { readings++; return true; });
        await cut.InvokeAsync(() =>
        {
            // A node followed on every move of a drag.
            cut.Instance.SetCenter(100, 20, 1);
            cut.Instance.SetCenter(110, 20, 1);
            cut.Instance.SetCenter(120, 20, 1);
        });
        Assert.AreEqual(1, readings);

        await cut.InvokeAsync(() => pending.SetResult(new BlowDomRect(0, 0, 600, 600)));

        // The last call is the one corrected for the size the pane turned out to have.
        cut.WaitForAssertion(() => AssertViewport(new BlowViewport(300 - 120, 300 - 20, 1), cut.Instance.GetViewport()));
    }

    [TestMethod]
    public async Task AnAsyncFitMovesAtOnceSoALaterMoveWins()
    {
        var nodes = ThreeNodes();
        var cut = RenderAt(1200, nodes);

        var pending = _module.Setup<BlowDomRect?>("getRect", _ => true);
        Task fit = Task.CompletedTask;
        await cut.InvokeAsync(() => { fit = cut.Instance.FitViewAsync(0); });

        // Applied before the call returned, as a fit with no duration always was.
        AssertViewport(FitFor(nodes, 1200), cut.Instance.GetViewport());

        var moved = new BlowViewport(10, 20, 1.5);
        await cut.InvokeAsync(() => cut.Instance.SetViewport(moved));
        await cut.InvokeAsync(() => pending.SetResult(new BlowDomRect(0, 0, 800, 600)));
        await fit;

        Assert.AreEqual(moved, cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task AMoveMadeDuringAnAnimatedFitsMeasurementCancelsTheFit()
    {
        var cut = RenderAt(1200, ThreeNodes());

        var pending = _module.Setup<BlowDomRect?>("getRect", _ => true);
        Task fit = Task.CompletedTask;
        await cut.InvokeAsync(() => { fit = cut.Instance.FitViewAsync(new BlowFitViewOptions { Duration = 5000 }); });

        var moved = new BlowViewport(10, 20, 1.5);
        await cut.InvokeAsync(() => cut.Instance.SetViewport(moved));
        await cut.InvokeAsync(() => pending.SetResult(new BlowDomRect(0, 0, 800, 600)));
        await fit.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(moved, cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task AnAnimatedFitIsSteeredToTheSizeThePaneTurnsOutToHave()
    {
        var nodes = ThreeNodes();
        var cut = RenderAt(1200, nodes);

        var pending = _module.Setup<BlowDomRect?>("getRect", _ => true);
        Task fit = Task.CompletedTask;
        await cut.InvokeAsync(() => { fit = cut.Instance.FitViewAsync(new BlowFitViewOptions { Duration = 100 }); });
        await cut.InvokeAsync(() => pending.SetResult(new BlowDomRect(0, 0, 800, 600)));
        await fit.WaitAsync(TimeSpan.FromSeconds(5));

        AssertViewport(FitFor(nodes, 800), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task OnPaneResizeComesAfterTheNodesAreMeasured()
    {
        List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0) }];
        BlowDimensions? seen = null;
        BlowCanvas? canvas = null;
        _module.Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult([new BlowMeasuredNode("a", 100, 40, [])]);
        var cut = RenderAt(800, nodes, p => p.Add(c => c.OnPaneResize, () => seen = canvas!.GetNode("a")!.Measured));
        canvas = cut.Instance;

        // One layout pass resized the pane and the node together.
        _module.Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult([new BlowMeasuredNode("a", 150, 60, [])]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(true, new BlowDomRect(0, 0, 1000, 600)));

        Assert.AreEqual(new BlowDimensions(150, 60), seen);
    }

    [TestMethod]
    public async Task APanBegunBeforeTheMeasurementReturnsIsNotFittedOver()
    {
        var nodes = ThreeNodes();
        var sizes = new List<BlowDimensions>();
        var cut = RenderAt(1200, nodes, p => p.Add(c => c.OnPaneResize, (BlowDimensions size) => sizes.Add(size)));

        var pending = _module.Setup<BlowDomRect?>("getRect", _ => true);
        await cut.InvokeAsync(() => cut.Instance.FitView());
        var fitted = cut.Instance.GetViewport();

        // The press arms the pan; the viewport has not moved yet.
        var down = cut.Find(".bit-blow").PointerDownAsync(new PointerEventArgs { PointerId = 1, Button = 0, ClientX = 600, ClientY = 300 });
        await cut.InvokeAsync(() => pending.SetResult(new BlowDomRect(0, 0, 800, 600)));
        await down;

        Assert.AreEqual(fitted, cut.Instance.GetViewport());
        // Neither reading is the observer's, so neither raises the event a handler might fit from mid-gesture.
        Assert.IsEmpty(sizes);
    }

    [TestMethod]
    public async Task AnOnPaneResizeHandlerThatThrowsReachesTheRenderer()
    {
        var cut = RenderAt(800, ThreeNodes(), p => p.Add(c => c.OnPaneResize,
            (BlowDimensions _) => throw new InvalidOperationException("handler failed")));

        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(true, new BlowDomRect(0, 0, 1000, 600)));

        var thrown = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("handler failed", thrown.Message);
    }

    [TestMethod]
    public async Task ZoomingInAfterAnUnreportedResizePivotsOnTheNewCentre()
    {
        var cut = RenderAt(1200, ThreeNodes());
        var from = cut.Instance.GetViewport();

        PaneIs(800);
        await cut.InvokeAsync(() => cut.Instance.ZoomIn());

        // About the centre of the pane as it now is, (400, 300).
        var zoom = from.Zoom * 1.2;
        var flowX = (400 - from.X) / from.Zoom;
        var flowY = (300 - from.Y) / from.Zoom;
        AssertViewport(new BlowViewport(400 - flowX * zoom, 300 - flowY * zoom, zoom), cut.Instance.GetViewport());
    }
}
