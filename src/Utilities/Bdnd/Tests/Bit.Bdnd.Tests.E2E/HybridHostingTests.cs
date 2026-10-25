using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// The library in Blazor Hybrid: a WinForms BlazorWebView (<c>Tests/Bit.Bdnd.Tests.Harness.Hybrid</c>) whose WebView2
/// the suite drives over the DevTools protocol. The renderer runs in the app's process, out of the browser's, so every
/// report of the module and every callback crosses the WebView's interop bridge - and the drags work: native mouse
/// drags, keyboard drags, mouse and touch pointer drags, a drag layer, insertion edges and file contents read into
/// .NET. Windows only, and once per run (with the Chromium pass): the WebView is Chromium whichever engine the run
/// drives.
/// </summary>
[TestClass]
public sealed class HybridHostingTests : BdndTest
{
    private static List<string>? _problems;

    // No demo and no downloaded browser: the WebView2 Runtime is the browser.
    protected override bool UsesDemo => false;

    private static ILocator Bin(IPage page) => page.Locator(".hybrid-bin");

    // The app page, loaded afresh for each test with the suite's probe in it (the module's registrations are what
    // WaitForBdndAsync and the drag helpers wait on).
    private static async Task<IPage> OpenHybridAsync()
    {
        if (OperatingSystem.IsWindows() is false) Assert.Inconclusive("The BlazorWebView harness runs on WebView2, which needs Windows.");
        if (E2EEnvironment.BrowserEngine != "chromium") Assert.Inconclusive("The hybrid harness runs with the Chromium pass: its WebView is Chromium whatever the run drives.");

        var host = await HybridSession.GetAsync();
        var page = host.Page;
        var app = page.Locator(".hybrid-check");
        if (_problems is null)
        {
            // the app's first start finishes before the first reload: a reload in the middle of it loses the WebView
            await Expect(app).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 60_000 });
            await page.Context.AddInitScriptAsync(BrowserProbe);
            _problems = TrackProblems(page, HybridHarnessHost.AppOrigin);
        }
        // what happened before this test is its own; what the reload raises (the old page's renderer going) is this one's
        lock (_problems) _problems.Clear();
        await page.ReloadAsync();
        await Expect(app).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 60_000 });
        await WaitForBdndAsync(page.MainFrame);
        return page;
    }

    [TestCleanup]
    public void NoProblemsInTheWebView()
    {
        // a test that failed already reports its own failure, not the problems it left behind
        if (_problems is null || TestContext.CurrentTestOutcome != UnitTestOutcome.Passed) return;
        lock (_problems)
        {
            Assert.IsEmpty(_problems, "the WebView reported:" + Environment.NewLine + string.Join(Environment.NewLine, _problems));
        }
    }

    [TestMethod]
    public async Task The_page_runs_in_the_webview_renderer()
    {
        var page = await OpenHybridAsync();

        await Expect(page.Locator(".hybrid-renderer")).ToHaveTextAsync("WebView");
    }

    [TestMethod]
    public async Task A_native_mouse_drag_drops_in_the_webview()
    {
        var page = await OpenHybridAsync();

        await DragAsync(page, page.Locator(".hybrid-box", new() { HasText = "Glass" }), Bin(page),
            () => Expect(Bin(page)).ToHaveTextAsync("Release"));

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Glass (Native)");
    }

    [TestMethod]
    public async Task A_keyboard_drag_drops_in_the_webview_and_keeps_the_focus()
    {
        var page = await OpenHybridAsync();
        var banana = page.Locator(".hybrid-box", new() { HasText = "Banana" });
        await banana.FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await Expect(page.Locator("#bit-bdnd-live-region")).ToContainTextAsync("Picked up Banana.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.Locator("#bit-bdnd-live-region")).ToHaveTextAsync("Banana is over the bin.");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Banana (Keyboard)");
        await EventuallyAsync(() => banana.EvaluateAsync<bool>("el => document.activeElement === el"), "the focus stays on the box");
    }

    [TestMethod]
    public async Task A_mouse_pointer_drag_drops_in_the_webview()
    {
        var page = await OpenHybridAsync();
        var from = (await BoxAsync(page.Locator(".hybrid-pointer:not([data-bit-bdnd-preview])"))).Centre;
        var to = (await BoxAsync(Bin(page))).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, to);
        await Expect(Bin(page)).ToHaveTextAsync("Release");
        await page.Mouse.UpAsync();

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Pointer (Pointer)");
        await Expect(page.Locator("[data-bit-bdnd-preview]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_touch_drag_drops_in_the_webview()
    {
        var page = await OpenHybridAsync();
        var from = (await BoxAsync(page.Locator(".hybrid-box:not([data-bit-bdnd-preview])", new() { HasText = "Glass" }))).Centre;
        var to = (await BoxAsync(Bin(page))).Centre;
        var cdp = await TouchScreenAsync(page);

        await TouchAsync(cdp, "touchStart", from);
        await Expect(page.Locator("[data-bit-bdnd-preview]")).ToHaveCountAsync(1); // it rested for the TouchDelay: the drag began
        await TouchMoveAsync(cdp, from, to);
        await Expect(Bin(page)).ToHaveTextAsync("Release");
        await TouchAsync(cdp, "touchEnd");

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Glass (Pointer)");
    }

    [TestMethod]
    public async Task A_drag_layer_follows_the_pointer_in_the_webview()
    {
        var page = await OpenHybridAsync();
        var layered = page.Locator(".hybrid-layered");
        var box = await BoxAsync(layered);

        await StartDragAsync(page, layered);
        await MoveAsync(page, box.Centre.Offset(60, 40));
        var preview = page.Locator(".hybrid-preview");
        await Expect(preview).ToBeVisibleAsync();
        await EventuallyAsync(async () =>
        {
            var b = await preview.BoundingBoxAsync();
            return b is not null && Math.Abs(b.X - box.X - 60) <= 2 && Math.Abs(b.Y - box.Y - 40) <= 2;
        }, "the preview follows the pointer");
        await DragToAsync(page, (await BoxAsync(Bin(page))).Centre);

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Layered (Native)");
        await Expect(preview).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Insertion_edges_reorder_in_the_webview()
    {
        var page = await OpenHybridAsync();
        var three = await BoxAsync(page.Locator(".hybrid-row-item", new() { HasText = "Three" }));

        await StartDragAsync(page, page.Locator(".hybrid-row-item", new() { HasText = "One" }));
        await DragToAsync(page, new Point(three.Centre.X, three.Y + three.Height * 0.8),
            () => Expect(page.Locator(".hybrid-slot", new() { Has = page.Locator(".hybrid-row-item", new() { HasText = "Three" }) }))
                .ToHaveAttributeAsync("data-bit-bdnd-edge", "bottom"));

        await Expect(page.Locator(".hybrid-order")).ToHaveTextAsync("Order: Two, Three, One");
    }

    [TestMethod]
    public async Task Dropped_file_contents_reach_dotnet_in_the_webview()
    {
        var page = await OpenHybridAsync();
        var zone = page.Locator(".hybrid-files");
        var files = await NativeDataTransferAsync(page, "dt.items.add(new File(['hello from the web view'], 'note.txt', { type: 'text/plain' }))");

        await DispatchDragAsync(zone, "dragenter", files);
        await DispatchDragAsync(zone, "dragover", files);
        await Expect(zone).ToHaveTextAsync("Release the files");
        await DispatchDragAsync(zone, "drop", files);

        await Expect(page.Locator(".hybrid-file-contents")).ToHaveTextAsync("note.txt: hello from the web view");
    }
}
