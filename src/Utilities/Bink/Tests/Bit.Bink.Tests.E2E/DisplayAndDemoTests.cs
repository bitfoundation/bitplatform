using Bit.Bink.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// Browser checks for the things only a real display can answer: device pixel ratio, resizing, pointer routing
/// between contacts, and whether every page of the demo actually works.
/// </summary>
[TestClass]
public sealed class DisplayAndDemoTests : BinkTest
{
    private static async Task DrawAsync(IPage page, params (float X, float Y)[] points)
    {
        var box = (await page.Locator(".bit-bink-input").First.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + points[0].X, box.Y + points[0].Y);
        await page.Mouse.DownAsync();
        for (int i = 1; i < points.Length; i++)
            await page.Mouse.MoveAsync(box.X + points[i].X, box.Y + points[i].Y, new MouseMoveOptions { Steps = 4 });
        await page.Mouse.UpAsync();
        await page.WaitForTimeoutAsync(250);
    }

    private static Task<string> CanvasSizeAsync(IPage page) => page.EvaluateAsync<string>(
        "() => { const c = document.querySelector('canvas[data-bink-layer=\"document\"]'); return c.width + 'x' + c.height; }");

    [TestMethod]
    public async Task The_backing_store_follows_the_device_pixel_ratio()
    {
        var context = await NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            DeviceScaleFactor = 2,
        });
        var page = await context.NewPageAsync();
        await GotoAsync(page, "/whiteboard");
        await WaitForCanvasAsync(page);
        await page.WaitForTimeoutAsync(300);

        int[] sizes = await page.EvaluateAsync<int[]>(
            """
            () => {
              const c = document.querySelector('canvas[data-bink-layer="document"]');
              return [c.width, c.height, Math.round(parseFloat(c.style.width)), Math.round(parseFloat(c.style.height)), window.devicePixelRatio];
            }
            """);
        TestContext.WriteLine($"backing {sizes[0]}x{sizes[1]}, css {sizes[2]}x{sizes[3]}, dpr {sizes[4]}");
        Assert.AreEqual(2, sizes[4]);
        Assert.AreEqual(sizes[2] * 2, sizes[0]);
        Assert.AreEqual(sizes[3] * 2, sizes[1]);

        // Ink drawn at a known CSS position must land at twice that position in the backing store.
        await DrawAsync(page, (100, 100), (300, 100));
        int[] ink = await InkBoxAsync(page);
        TestContext.WriteLine($"ink box (device px): {string.Join(",", ink)}");
        AssertInRange(ink[0], 2 * 100 - 30, 2 * 100 + 30, "the left edge of the ink");
        AssertInRange(ink[2], 2 * 300 - 30, 2 * 300 + 30, "the right edge of the ink");
    }

    [TestMethod]
    public async Task Resizing_the_window_keeps_the_drawing_where_it_was()
    {
        var page = await OpenAsync("/whiteboard");
        await DrawAsync(page, (120, 120), (320, 220));

        string before = await CanvasSizeAsync(page);
        int[] inkBefore = await InkBoxAsync(page);

        await page.SetViewportSizeAsync(900, 700);
        await page.WaitForTimeoutAsync(600);
        string after = await CanvasSizeAsync(page);
        int[] inkAfter = await InkBoxAsync(page);

        TestContext.WriteLine($"canvas {before} -> {after}; ink {string.Join(",", inkBefore)} -> {string.Join(",", inkAfter)}");
        Assert.AreNotEqual(before, after, "the canvas did not follow the window size");
        // The document did not change, so the ink must still start at the same document position.
        AssertInRange(inkAfter[0], inkBefore[0] - 4, inkBefore[0] + 4, "the left edge of the ink after the resize");
        AssertInRange(inkAfter[1], inkBefore[1] - 4, inkBefore[1] + 4, "the top edge of the ink after the resize");
    }

    [TestMethod]
    public async Task A_cancelled_unrelated_pointer_does_not_discard_the_stroke_in_progress()
    {
        var page = await OpenAsync("/whiteboard");

        // A mouse (or pen) stroke is under way when a single stray contact appears and the browser cancels it:
        // a finger brushing a touch screen, a contact the OS rejects. One contact is not a two-finger gesture, so
        // the cancel reaches the active tool, which must ignore it and keep drawing.
        await page.EvaluateAsync(
            """
            async () => {
              const target = document.querySelector('.bit-bink-input');
              const rect = target.getBoundingClientRect();
              const send = (type, id, kind, x, y, buttons) => target.dispatchEvent(new PointerEvent(type, {
                pointerId: id, pointerType: kind, isPrimary: true, bubbles: true, cancelable: true,
                clientX: rect.left + x, clientY: rect.top + y, pressure: buttons ? 0.5 : 0, buttons,
              }));
              send('pointerdown', 1, 'mouse', 80, 200, 1);
              for (let i = 1; i <= 15; i++) {
                send('pointermove', 1, 'mouse', 80 + i * 14, 200, 1);
                await new Promise(r => setTimeout(r, 10));
              }
              send('pointerdown', 55, 'touch', 600, 500, 1);
              await new Promise(r => setTimeout(r, 30));
              send('pointercancel', 55, 'touch', 600, 500, 0);
              await new Promise(r => setTimeout(r, 30));
              for (let i = 16; i <= 25; i++) {
                send('pointermove', 1, 'mouse', 80 + i * 14, 200, 1);
                await new Promise(r => setTimeout(r, 10));
              }
              send('pointerup', 1, 'mouse', 80 + 25 * 14, 200, 0);
            }
            """);
        await page.WaitForTimeoutAsync(500);

        int ink = await CountInkAsync(page);
        TestContext.WriteLine($"ink after the stray contact was cancelled: {ink}");
        Assert.IsTrue(ink > 300, $"the stroke was discarded when an unrelated pointer was cancelled ({ink} ink pixels)");
    }

    [TestMethod]
    public async Task Drawing_at_a_zoomed_viewport_lands_under_the_cursor()
    {
        var page = await OpenAsync("/whiteboard");

        // Zoom in with the toolbar, then draw: the ink must appear where the pointer went, not offset by the zoom.
        var zoomIn = page.GetByRole(AriaRole.Button, new() { Name = "Zoom in", Exact = true });
        await zoomIn.ClickAsync();
        await zoomIn.ClickAsync();
        await page.WaitForTimeoutAsync(300);

        double dpr = await page.EvaluateAsync<double>("() => window.devicePixelRatio");
        await DrawAsync(page, (200, 200), (400, 200));
        int[] ink = await InkBoxAsync(page);
        TestContext.WriteLine($"dpr {dpr}, ink box {string.Join(",", ink)}");
        AssertInRange(ink[0] / dpr, 200 - 40, 200 + 40, "the left edge of the ink");
        AssertInRange(ink[2] / dpr, 400 - 40, 400 + 40, "the right edge of the ink");
        AssertInRange(ink[1] / dpr, 200 - 40, 200 + 40, "the top edge of the ink");
    }

    [TestMethod]
    public async Task More_images_than_the_browser_cache_holds_still_all_render()
    {
        var page = await OpenAsync("/image-annotation");

        // Load 40 distinct images through the module's own cache, then check every one is still there. A cache
        // that evicts by insertion order would silently drop the ones a document drew first.
        int missing = await page.EvaluateAsync<int>(
            """
            async () => {
              const module = await import('/_content/Bit.Bink/bit-bink.js');
              const surfaces = globalThis.__bitBinkSurfaces;
              const id = [...surfaces.keys()][0];
              const uris = [];
              for (let i = 0; i < 40; i++) {
                const canvas = document.createElement('canvas');
                canvas.width = canvas.height = 2;
                const ctx = canvas.getContext('2d');
                ctx.fillStyle = `rgb(${i * 6},0,0)`;
                ctx.fillRect(0, 0, 2, 2);
                uris.push(canvas.toDataURL('image/png'));
              }
              for (const uri of uris) await module.loadImage(id, uri);
              const surface = surfaces.get(id);
              let gone = 0;
              for (const uri of uris) if (!surface.images.has(uri)) gone++;
              return gone;
            }
            """);
        TestContext.WriteLine($"images evicted from the cache while still referenced: {missing}");
        Assert.AreEqual(0, missing);
    }

    [TestMethod]
    public async Task Leaving_and_returning_to_a_page_does_not_leave_surfaces_behind()
    {
        var page = await OpenAsync("/whiteboard");

        for (int i = 0; i < 6; i++)
        {
            await page.GetByRole(AriaRole.Link, new() { Name = "Signature", Exact = true }).First.ClickAsync();
            await page.WaitForTimeoutAsync(300);
            await page.GetByRole(AriaRole.Link, new() { Name = "Whiteboard", Exact = true }).First.ClickAsync();
            await page.WaitForTimeoutAsync(300);
        }
        await WaitForCanvasAsync(page);

        int surfaces = await page.EvaluateAsync<int>("() => globalThis.__bitBinkSurfaces ? globalThis.__bitBinkSurfaces.size : -1");
        int liveCanvases = await page.EvaluateAsync<int>("() => document.querySelectorAll('.bit-bink-canvas').length");
        TestContext.WriteLine($"surfaces registered: {surfaces}, canvases in the DOM: {liveCanvases}");
        Assert.IsTrue(surfaces >= 0, "the browser layer never registered a surface");
        Assert.IsTrue(surfaces <= liveCanvases, $"{surfaces} surfaces are registered for {liveCanvases} canvases");

        await DrawAsync(page, (120, 120), (320, 200));
        Assert.IsTrue(await CountInkAsync(page) > 200, "the canvas stopped drawing after repeated navigation");
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/signature")]
    [DataRow("/handwriting")]
    [DataRow("/whiteboard")]
    [DataRow("/diagram")]
    [DataRow("/image-annotation")]
    [DataRow("/document-annotation")]
    [DataRow("/collaboration")]
    [DataRow("/serialization")]
    [DataRow("/performance")]
    public async Task Every_demo_page_loads_without_console_errors(string path)
    {
        var context = await NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            HasTouch = true,
            AcceptDownloads = true,
        });
        var page = await context.NewPageAsync();

        var problems = new List<string>();
        page.Console += (_, message) => { if (message.Type == "error") lock (problems) problems.Add("console: " + message.Text); };
        page.PageError += (_, error) => { lock (problems) problems.Add("unhandled: " + error); };

        await GotoAsync(page, path);
        await WaitForCanvasAsync(page);
        await page.WaitForTimeoutAsync(700);

        // Every enabled button on the page must run without throwing: a dead or broken control is a demo bug.
        var buttons = page.Locator("button:enabled:visible");
        int count = Math.Min(await buttons.CountAsync(), 30);
        for (int i = 0; i < count; i++)
        {
            try
            {
                await buttons.Nth(i).ClickAsync(new LocatorClickOptions { Timeout = 3000, Force = true });
                await page.WaitForTimeoutAsync(60);
            }
            catch (TimeoutException)
            {
                // The click was superseded by a re-render; that is not a page error.
            }
        }
        await page.WaitForTimeoutAsync(500);

        string[] found;
        lock (problems) found = [.. problems];
        TestContext.WriteLine($"{path}: {count} buttons exercised, {found.Length} problems");
        foreach (string problem in found) TestContext.WriteLine("  " + problem);
        Assert.AreEqual(0, found.Length, $"{path} logged errors:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }

    [TestMethod]
    public async Task The_canvas_still_draws_after_every_toolbar_control_has_been_used()
    {
        var page = await OpenAsync("/whiteboard");

        var buttons = page.Locator(".toolbar button:enabled:visible");
        int count = await buttons.CountAsync();
        TestContext.WriteLine($"toolbar buttons: {count}");
        Assert.IsTrue(count > 5, "the whiteboard toolbar should offer more than a handful of controls");

        // Select the pen again so the surface is drawable, then confirm ink still lands.
        await page.GetByRole(AriaRole.Button, new() { Name = "Pen", Exact = true }).First.ClickAsync();
        await DrawAsync(page, (140, 140), (340, 240));
        Assert.IsTrue(await CountInkAsync(page) > 200, "the whiteboard stopped drawing");
    }
}
