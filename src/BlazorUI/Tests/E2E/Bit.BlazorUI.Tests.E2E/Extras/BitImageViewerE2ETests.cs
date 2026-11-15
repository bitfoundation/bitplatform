using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitImageViewer at /e2e/image-viewer in a real browser: an 800x400 image fitted in a 400x300 frame and centered;
/// zoomed by the buttons around the center, by Ctrl+wheel, a double-click and a pinch around the point they land on, and
/// by + - 0 from the keyboard; panned by dragging and by the arrow keys, never past an edge; the plain wheel and the
/// arrow keys of a fitted image left to the page; a new image fitted again; a disabled viewer left alone; and axe.
/// </summary>
[TestClass]
public class BitImageViewerE2ETests : E2ETestBase
{
    private const string Route = "/e2e/image-viewer";

    private ILocator Viewport(string id = "imv") => Page.Locator($"#{id} .bit-imv-vpt");

    private ILocator Image(string id = "imv") => Page.Locator($"#{id} .bit-imv-img");

    private ILocator ZoomText(string id = "imv") => Page.Locator($"#{id} .bit-imv-zmt");

    private ILocator Button(string name, string id = "imv") => Page.Locator($"#{id} .bit-imv-btn[aria-label='{name}']");

    private static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) < tolerance, $"{what}: expected about {expected}, got {actual}.");
    }

    private async Task OpenViewerAsync()
    {
        await OpenAsync(Route);
        await Page.WaitForFunctionAsync("() => [...document.querySelectorAll('.bit-imv-img')].every(i => i.complete && i.naturalWidth > 0)");
    }

    private async Task<(LocatorBoundingBoxResult Viewport, LocatorBoundingBoxResult Image)> BoxesAsync(string id = "imv")
    {
        return ((await Viewport(id).BoundingBoxAsync())!, (await Image(id).BoundingBoxAsync())!);
    }

    // Waits for the transform to stop changing, which it does at once: the zoom has no transition.
    private async Task SettledAsync() => await NextFramesAsync();

    // A zoom set from .NET - a button, the page's binding - reaches the script on the round trip after the render that
    // shows its percentage, so the image is waited for rather than measured a frame later.
    private async Task ImageWidthAsync(double width, string id = "imv")
    {
        await Page.WaitForFunctionAsync("({ selector, width }) => Math.abs(document.querySelector(selector).getBoundingClientRect().width - width) < 1.5",
                                        new { selector = $"#{id} .bit-imv-img", width });
    }

    [TestMethod]
    public async Task TheImage_IsFittedInTheFrame_Centered_AndThePagePassesAxe()
    {
        await OpenViewerAsync();

        var (viewport, image) = await BoxesAsync();

        Near(400, viewport.Width, "frame width");
        Near(300, viewport.Height, "frame height");
        Near(400, image.Width, "fitted width");
        Near(200, image.Height, "fitted height");
        Near(viewport.X, image.X, "fitted left");
        Near(viewport.Y + 50, image.Y, "centered top");

        await Expect(ZoomText()).ToHaveTextAsync("100%");
        await Expect(Viewport()).ToHaveAccessibleNameAsync("Four colored quadrants");
        await Expect(Viewport()).ToHaveAccessibleDescriptionAsync(new Regex("Plus and minus zoom"));

        await AssertNoAxeViolationsAsync("#imv");
        await AssertNoAxeViolationsAsync("#imv-dis");
    }

    [TestMethod]
    public async Task TheButtons_ZoomAroundTheCenter_AndFitTheImageAgain()
    {
        await OpenViewerAsync();

        await Button("Zoom in").ClickAsync();
        await Button("Zoom in").ClickAsync();

        await Expect(ZoomText()).ToHaveTextAsync("156%");
        await Expect(Page.Locator("#imv-zoom")).ToHaveTextAsync("1.5625");
        await ImageWidthAsync(625);

        var (viewport, image) = await BoxesAsync();
        Near(625, image.Width, "zoomed width");
        Near(viewport.X + viewport.Width / 2, image.X + image.Width / 2, "still centered across");
        Near(viewport.Y + viewport.Height / 2, image.Y + image.Height / 2, "still centered down");

        await Button("Fit to view").ClickAsync();

        await Expect(ZoomText()).ToHaveTextAsync("100%");
        await Expect(Button("Fit to view")).ToBeDisabledAsync();
        await ImageWidthAsync(400);
    }

    [TestMethod]
    public async Task TheKeyboard_Zooms_PansWithinTheEdges_AndFits()
    {
        await OpenViewerAsync();

        await Viewport().FocusAsync();
        await Page.Keyboard.PressAsync("+");
        await Expect(ZoomText()).ToHaveTextAsync("125%");
        await Page.Keyboard.PressAsync("=");
        await Expect(ZoomText()).ToHaveTextAsync("156%");
        await Page.Keyboard.PressAsync("-");
        await Expect(ZoomText()).ToHaveTextAsync("125%");
        await Page.Keyboard.PressAsync("+");
        await Page.Keyboard.PressAsync("+");
        await Page.Keyboard.PressAsync("+");
        await Expect(ZoomText()).ToHaveTextAsync("244%");

        // The arrow keys move the image the way the content of a scrolled frame moves: ArrowRight shows more of its
        // right, and no amount of pressing takes an edge of it inside the frame.
        for (var i = 0; i < 30; i++) await Page.Keyboard.PressAsync("ArrowRight");
        await SettledAsync();
        var (viewport, image) = await BoxesAsync();
        Near(viewport.X + viewport.Width, image.X + image.Width, "the right edge stops at the frame");

        for (var i = 0; i < 30; i++) await Page.Keyboard.PressAsync("ArrowDown");
        await SettledAsync();
        (viewport, image) = await BoxesAsync();
        Near(viewport.Y + viewport.Height, image.Y + image.Height, "the bottom edge stops at the frame");

        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollY"), "the keys moved the image, not the page");

        await Page.Keyboard.PressAsync("0");
        await Expect(ZoomText()).ToHaveTextAsync("100%");
        await SettledAsync();
        (viewport, image) = await BoxesAsync();
        Near(viewport.Y + 50, image.Y, "fitted and centered again");

        // A fitted image has nowhere to go, and leaves the arrow keys to the page.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForFunctionAsync("() => window.scrollY > 0");
    }

    [TestMethod]
    public async Task ThePlainWheel_IsThePages_AndCtrlWheelZoomsAroundThePointer()
    {
        await OpenViewerAsync();

        var (viewport, image) = await BoxesAsync();
        var x = viewport.X + 100;
        var y = viewport.Y + 120;
        await Page.Mouse.MoveAsync(x, y);

        await Page.Mouse.WheelAsync(0, 100);
        await Page.WaitForFunctionAsync("() => window.scrollY > 0");
        await Expect(Page.Locator("#imv-zoom")).ToHaveTextAsync("1");

        await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await Page.WaitForFunctionAsync("() => window.scrollY === 0");

        (_, image) = await BoxesAsync();
        var fx = (x - image.X) / image.Width;
        var fy = (y - image.Y) / image.Height;

        await Page.Keyboard.DownAsync("Control");
        await Page.Mouse.WheelAsync(0, -400);
        await Page.Keyboard.UpAsync("Control");

        await Expect(Page.Locator("#imv-zoom")).Not.ToHaveTextAsync("1");
        await SettledAsync();

        // The point under the pointer is still under it - zoomed far enough for the image to be taller than the frame, which
        // keeps one shorter than it centered instead.
        (_, image) = await BoxesAsync();
        Assert.IsTrue(image.Width > 400, "it zoomed in.");
        Near(x, image.X + fx * image.Width, "the pointer's point across");
        Near(y, image.Y + fy * image.Height, "the pointer's point down");
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollY"), "Ctrl+wheel zoomed rather than scrolled");
    }

    [TestMethod]
    public async Task AViewerWithWheelZoom_ZoomsWithThePlainWheel()
    {
        await OpenViewerAsync();

        var (viewport, _) = await BoxesAsync("imv-wheel");
        await Page.Mouse.MoveAsync(viewport.X + viewport.Width / 2, viewport.Y + viewport.Height / 2);
        var scrolled = await Page.EvaluateAsync<double>("() => window.scrollY");

        await Page.Mouse.WheelAsync(0, -200);
        await SettledAsync();

        Assert.IsTrue((await Image("imv-wheel").BoundingBoxAsync())!.Width > 400, "it zoomed in.");
        Assert.AreEqual(scrolled, await Page.EvaluateAsync<double>("() => window.scrollY"), "the wheel was the viewer's");
    }

    [TestMethod]
    public async Task ADoubleClick_ZoomsAtItsPoint_ADrag_PansWithinTheEdges_AndASecondOneFits()
    {
        await OpenViewerAsync();

        var (viewport, image) = await BoxesAsync();
        var x = viewport.X + 300;
        var y = viewport.Y + 150;
        var fx = (x - image.X) / image.Width;

        await Page.Mouse.DblClickAsync(x, y);
        await Expect(ZoomText()).ToHaveTextAsync("200%");
        await SettledAsync();

        (viewport, image) = await BoxesAsync();
        Near(800, image.Width, "doubled");
        Near(x, image.X + fx * image.Width, "the clicked point stays under the pointer");
        await Expect(Viewport()).ToHaveClassAsync(new Regex("bit-imv-pan"));

        // Dragged by the pointer, by as much as it moves.
        var before = image;
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x - 40, y - 20, new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        await SettledAsync();

        (viewport, image) = await BoxesAsync();
        Near(before.X - 40, image.X, "dragged across");
        Near(before.Y - 20, image.Y, "dragged down");

        // And never past an edge: at 200% the image runs 200px past the frame on each side and 50px above and below.
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 400, y + 200, new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        await SettledAsync();

        (viewport, image) = await BoxesAsync();
        Near(viewport.X, image.X, "the left edge stops at the frame");
        Near(viewport.Y, image.Y, "the top edge stops at the frame");

        await Page.Mouse.DblClickAsync(x, y);
        await Expect(ZoomText()).ToHaveTextAsync("100%");
    }

    [TestMethod]
    public async Task APinch_ZoomsAroundItsMiddle()
    {
        await OpenViewerAsync();

        var (viewport, image) = await BoxesAsync();
        // As doubles: the evaluation serializes the floats of a bounding box as empty objects.
        var cx = (double)viewport.X + 200;
        var cy = (double)viewport.Y + 150;

        // Two touch contacts moving apart from the middle of the frame, twice as far as they started, and then 30px to the
        // right together, one after the other - which a pinch follows without drifting.
        await Viewport().EvaluateAsync(@"(el, { cx, cy }) => {
            const fire = (type, id, x) => el.dispatchEvent(new PointerEvent(type, { pointerId: id, pointerType: 'touch', isPrimary: id === 1, clientX: x, clientY: cy, bubbles: true }));
            fire('pointerdown', 1, cx - 40);
            fire('pointerdown', 2, cx + 40);
            for (let i = 1; i <= 4; i++) {
                fire('pointermove', 1, cx - 40 - i * 10);
                fire('pointermove', 2, cx + 40 + i * 10);
            }
            fire('pointermove', 1, cx - 50);
            fire('pointermove', 2, cx + 110);
            fire('pointerup', 1, cx - 50);
            fire('pointerup', 2, cx + 110);
        }", new { cx, cy });

        await Expect(ZoomText()).ToHaveTextAsync("200%");
        await SettledAsync();

        (viewport, image) = await BoxesAsync();
        Near(cx + 30, image.X + image.Width / 2, "around its middle, carried along with the fingers");
    }

    [TestMethod]
    public async Task ANewImage_IsFittedAgain_AndTheBoundZoomFollows()
    {
        await OpenViewerAsync();

        await Page.Locator("#btn-zoom-3").ClickAsync();
        await Expect(ZoomText()).ToHaveTextAsync("300%");
        await ImageWidthAsync(1200);

        await Page.Locator("#btn-src").ClickAsync();

        await Expect(ZoomText()).ToHaveTextAsync("100%");
        await Expect(Page.Locator("#imv-zoom")).ToHaveTextAsync("1");
        await ImageWidthAsync(400);
    }

    [TestMethod]
    public async Task ADisabledViewer_NeitherZoomsNorPans_NorTakesTheFocus()
    {
        await OpenViewerAsync();

        var viewport = Viewport("imv-dis");
        await Expect(viewport).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Button("Zoom in", "imv-dis")).ToBeDisabledAsync();

        var (box, _) = await BoxesAsync("imv-dis");
        await viewport.ScrollIntoViewIfNeededAsync();
        (box, _) = await BoxesAsync("imv-dis");

        // The disabled viewer leaves the Ctrl+wheel to the page, whose default is the browser's own zoom - which would
        // zoom the page itself (Firefox keeps that zoom per site, for every test the browser runs afterwards). The page
        // takes the default away once the press has passed the viewer, noting whether anything claimed it before.
        await Page.EvaluateAsync("""
            () => {
                window.__imvCtrlWheelClaimed = null;
                window.addEventListener('wheel', e => {
                    if (!e.ctrlKey) return;
                    window.__imvCtrlWheelClaimed = e.defaultPrevented;
                    e.preventDefault();
                }, { passive: false });
            }
            """);
        var pixelRatio = await Page.EvaluateAsync<double>("() => window.devicePixelRatio");

        await Page.Mouse.DblClickAsync(box.X + 200, box.Y + 150);
        await Page.Keyboard.DownAsync("Control");
        await Page.Mouse.WheelAsync(0, -200);
        await Page.Keyboard.UpAsync("Control");
        await SettledAsync();

        await Expect(ZoomText("imv-dis")).ToHaveTextAsync("100%");
        Near(400, (await Image("imv-dis").BoundingBoxAsync())!.Width, "still fitted");
        Assert.IsFalse(await Page.EvaluateAsync<bool?>("() => window.__imvCtrlWheelClaimed"), "the viewer left the Ctrl+wheel to the page");
        // A browser's page zoom shows as a change of the device pixel ratio.
        Assert.AreEqual(pixelRatio, await Page.EvaluateAsync<double>("() => window.devicePixelRatio"), "the page is not zoomed");
    }
}
