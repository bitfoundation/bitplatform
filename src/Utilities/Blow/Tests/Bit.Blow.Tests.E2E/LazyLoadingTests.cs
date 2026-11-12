using System.Globalization;
using Bit.Blow.Tests.E2E.Infrastructure;
using Bit.Utilities.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// An app that lazy loads Bit.Blow on WebAssembly, set up as the README's "Lazy loading on WebAssembly" section says:
/// the lazy loading sample (<c>Samples/Lazy</c>), published - trimmed, with Bit.Blow marked lazy and nothing of it
/// registered anywhere - and driven in a browser. Its home page has no canvas and never downloads <c>Bit.Blow.wasm</c>,
/// though Blow's stylesheet is already linked from it; its workflow page has the router download the assembly before
/// the page renders, and its canvas, which takes only <c>IJSRuntime</c>, measures and fits its nodes, follows a dragged
/// node and connects two handles. A deep link to the workflow is prerendered by the server, which registers the
/// <c>LazyAssemblyLoader</c> its router injects. Published with AOT (<c>BLOW_E2E_LAZY_AOT=1</c>), the sample loads
/// Bit.Blow with the app, as the README says an AOT build must, and works just the same.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests : BlowTest
{
    private LazyLoadingSample? _sample;

    protected override string BaseUrl => _sample!.BaseUrl;

    [TestInitialize]
    public async Task StartSampleAsync()
    {
        string? unavailableReason = null;
        try
        {
            _sample = await LazyLoadingSample.GetAsync("Bit.Blow", "BLOW", E2EEnvironment.Configuration);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            unavailableReason = $"the lazy loading sample could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}";
        }

        if (unavailableReason is not null)
        {            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + unavailableReason);
            Assert.Inconclusive("The browser tests could not run: " + unavailableReason);
        }
    }

    [TestMethod]
    public async Task The_home_page_never_downloads_Bit_Blow_and_the_workflow_downloads_it_once_before_its_canvas_renders()
    {
        var (page, problems) = await NewPageAsync();
        var requests = _sample!.Track(page);

        await GotoAsync(page, "/");
        await WaitForFlowAsync(page);
        _sample.AssertNotLoaded(requests);

        // The stylesheet is a static web asset the host page links, not part of the assembly: it is there already, so a
        // canvas is laid out from its first render.
        await Expect(page.Locator("link[rel='stylesheet'][href*='_content/Bit.Blow/bit-blow.']")).ToHaveCountAsync(1);

        // In the app: the router's OnNavigateAsync downloads the assembly, then the workflow renders.
        await page.GetByRole(AriaRole.Link, new() { Name = "Workflow", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/workflow");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Workflow");

        await AssertTheCanvasEditsAsync(page);

        // Back and forth: the router asks for the assembly again, and nothing is downloaded again.
        await page.GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Lazy loading");
        await page.GetByRole(AriaRole.Link, new() { Name = "Workflow", Exact = true }).ClickAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("Workflow");
        await AssertTheCanvasEditsAsync(page);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    [TestMethod]
    public async Task A_deep_link_to_the_workflow_is_prerendered_and_edits_once_the_browser_takes_over()
    {
        var (page, problems) = await NewPageAsync();
        var requests = _sample!.Track(page);

        // The server prerenders the router - LazyAssemblyLoader resolved there too - and the canvas with its nodes, which
        // nothing measures until the browser takes over.
        var response = await GotoAsync(page, "/workflow");
        var actualValue = response?.Status;
        Assert.AreEqual(200, actualValue);
        var prerendered = await response!.TextAsync();
        Assert.Contains("bit-blow-node", prerendered);
        Assert.Contains("not measured yet", prerendered);

        await WaitForFlowAsync(page);

        await AssertTheCanvasEditsAsync(page);

        _sample.AssertLoadedOnce(requests);
        AssertNoProblems(problems);
    }

    /// <summary>
    /// The canvas at work through the script module it imported without a registration: it measures its nodes and fits
    /// them in view, a node follows the mouse, and a drag from one handle to another connects them. The page's own
    /// readouts, which the canvas's events update in .NET, show each edit.
    /// </summary>
    private static async Task AssertTheCanvasEditsAsync(IPage page)
    {
        var connections = page.Locator("#connections");
        var position = page.Locator("#review-position");

        // OnInit: measured in the browser. Then the fit settles and the edges are drawn to the measured handles.
        await Expect(page.Locator("#measured")).ToHaveTextAsync("measured");
        await WaitForQuietAsync(page);
        await ScrollFlowIntoViewAsync(page);
        await WaitForEdgesAnchoredAsync(page);

        var pane = await PaneBoxAsync(page);
        foreach (var id in (string[])["1", "2", "3"])
        {
            var box = await NodeBoxAsync(page, id);
            Assert.IsTrue(pane.Contains(box), $"node {id} {box} is outside the canvas {pane} after the initial fit");
        }

        await Expect(connections).ToHaveTextAsync("1");
        await Expect(position).ToHaveTextAsync("100, 120");

        // A drag: the node follows the pointer on screen, and the position NodesChanged reports in flow units is the
        // same move at the zoom the fit chose.
        var zoom = (await ViewportAsync(page)).Zoom;
        var before = await NodeBoxAsync(page, "2");
        var grab = before.Centre.Snapped();
        await DragAsync(page, grab, grab.Offset(-60, 30));

        var after = await NodeBoxAsync(page, "2");
        AssertNear(before.X - 60, after.X, 0.5, "left of the dragged node");
        AssertNear(before.Y + 30, after.Y, 0.5, "top of the dragged node");
        await Expect(position).Not.ToHaveTextAsync("100, 120");
        var moved = await ReadPositionAsync(position);
        AssertNear(new Point(100 - 60 / zoom, 120 + 30 / zoom), moved, 1, "the position the page shows for the dragged node");

        // A connection: released over the target handle, which the browser layer finds under the pointer. The new edge
        // is drawn between the two handles and EdgesChanged updates the page's count.
        var source = await HandleCentreAsync(page, "2", "source");
        var target = await HandleCentreAsync(page, "3", "target");
        await DragAsync(page, source, target);

        await Expect(connections).ToHaveTextAsync("2");
        var (start, end) = await EdgeEndsAsync(page, "bit-blow-2--3");
        AssertNear(source, start, 1, "start of the new edge");
        AssertNear(target, end, 1, "end of the new edge");
    }

    private static async Task<Point> ReadPositionAsync(ILocator readout)
    {
        var parts = (await readout.TextContentAsync() ?? "").Split(", ");
        Assert.HasCount(2, parts, $"the position readout is not 'x, y': {string.Join(", ", parts)}");
        return new Point(double.Parse(parts[0], CultureInfo.InvariantCulture), double.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// A page in a fresh browser context, with everything it reports going wrong: console errors, unhandled
    /// exceptions, failed requests and error responses from the sample.
    /// </summary>
    private async Task<(IPage Page, List<string> Problems)> NewPageAsync()
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        return (page, TrackProblems(page, BaseUrl));
    }

    private static void AssertNoProblems(List<string> problems)
    {
        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, "the page reported:" + Environment.NewLine + string.Join(Environment.NewLine, found));
    }
}
