using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// The library over a Blazor Server circuit: the demo serves <c>/hosting/server-check</c> with InteractiveServer, so every
/// report of the module, every callback and every file read crosses SignalR - and the drags still work: native mouse
/// drags, keyboard drags, mouse and touch pointer drags, a drag layer, insertion edges and file contents streamed to the
/// server. Moving between that page and the WebAssembly ones loads the page afresh, in the other render mode.
/// </summary>
[TestClass]
public sealed class ServerHostingTests : BdndTest
{
    private const string Path = "/hosting/server-check";

    private static ILocator Bin(IPage page) => page.Locator(".server-check-bin");

    private async Task<IPage> OpenServerPageAsync(bool hasTouch = false)
    {
        var page = await OpenAsync(Path, hasTouch: hasTouch);
        await Expect(page.Locator(".server-check-renderer")).ToHaveTextAsync("Server");
        return page;
    }

    [TestMethod]
    public async Task A_native_mouse_drag_drops_over_the_circuit()
    {
        var page = await OpenServerPageAsync();

        await DragAsync(page, page.Locator(".server-check-box", new() { HasText = "Glass" }), Bin(page),
            () => Expect(Bin(page)).ToHaveTextAsync("Release"));

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Glass (Native)");
    }

    [TestMethod]
    public async Task A_keyboard_drag_drops_over_the_circuit()
    {
        var page = await OpenServerPageAsync();
        var banana = page.Locator(".server-check-box", new() { HasText = "Banana" });
        await WaitForBdndAsync(page.MainFrame);
        await banana.FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await Expect(page.Locator("#bit-bdnd-live-region")).ToContainTextAsync("Picked up Banana.");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.Locator("#bit-bdnd-live-region")).ToHaveTextAsync("Banana is over the bin.");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Banana (Keyboard)");
        Assert.IsTrue(await banana.EvaluateAsync<bool>("el => document.activeElement === el"), "the focus stays on the box");
    }

    [TestMethod]
    public async Task A_pointer_drag_drops_over_the_circuit()
    {
        var page = await OpenServerPageAsync();
        var pointer = page.Locator(".server-check-pointer:not([data-bit-bdnd-preview])");
        var from = (await BoxAsync(pointer)).Centre;
        var to = (await BoxAsync(Bin(page))).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, to);
        await Expect(Bin(page)).ToHaveTextAsync("Release");
        await page.Mouse.UpAsync();

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Pointer (Pointer)");
    }

    [TestMethod]
    public async Task A_touch_drag_drops_over_the_circuit()
    {
        RequireChromium("touches are synthesized through the DevTools protocol");
        var page = await OpenServerPageAsync(hasTouch: true);
        await WaitForBdndAsync(page.MainFrame);
        var glass = page.Locator(".server-check-box:not([data-bit-bdnd-preview])", new() { HasText = "Glass" });
        var from = (await BoxAsync(glass)).Centre;
        var to = (await BoxAsync(Bin(page))).Centre;
        var cdp = await TouchScreenAsync(page);

        await TouchAsync(cdp, "touchStart", from);
        await Expect(page.Locator("[data-bit-bdnd-preview]")).ToHaveCountAsync(1); // it rested for the TouchDelay: the drag began
        await TouchMoveAsync(cdp, from, to);
        await Expect(Bin(page)).ToHaveTextAsync("Release");
        await TouchAsync(cdp, "touchEnd");

        await Expect(Bin(page)).ToHaveTextAsync("Dropped: Glass (Pointer)");
        await Expect(page.Locator("[data-bit-bdnd-preview]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Moving_between_the_server_page_and_the_others_switches_the_render_mode()
    {
        var page = await OpenAsync("/dustbin/single");
        await page.EvaluateAsync("() => window.bdndMarker = 1");

        await page.Locator($"a[href='{Path}']").First.ClickAsync();

        await Expect(page.Locator(".server-check-renderer")).ToHaveTextAsync("Server");
        Assert.IsFalse(await page.EvaluateAsync<bool>("() => window.bdndMarker === 1"), "the server page was loaded afresh");
        await WaitForInteractiveAsync(page.MainFrame);
        await page.EvaluateAsync("() => window.bdndMarker = 2");

        await page.Locator("a[href='/dustbin/single']").First.ClickAsync();

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/dustbin/single$"));
        await EventuallyAsync(() => page.EvaluateAsync<bool>("() => window.bdndMarker === undefined"), "the WebAssembly page was loaded afresh");
        await WaitForBdndAsync(page.MainFrame);
        await DragAsync(page, page.Locator(".box", new() { HasText = "Glass" }), page.Locator(".dustbin"));
        await Expect(page.Locator(".dustbin")).ToHaveTextAsync("Last dropped: Glass (total 1)");
    }

    [TestMethod]
    [DataRow(false, DisplayName = "a native mouse drag")]
    [DataRow(true, DisplayName = "a pointer drag")]
    public async Task A_drag_sends_a_few_messages_over_the_circuit_however_long_it_moves(bool pointer)
    {
        // every frame the browser sends on the circuit's socket, counted from before the page opens
        var page = await NewPageAsync();
        var sent = 0;
        page.WebSocket += (_, socket) => socket.FrameSent += (_, _) => Interlocked.Increment(ref sent);
        await GotoAsync(page, Path);
        await WaitForInteractiveAsync(page.MainFrame);
        await WaitForBdndAsync(page.MainFrame);
        await Expect(page.Locator(".server-check-renderer")).ToHaveTextAsync("Server");
        var source = pointer
            ? page.Locator(".server-check-pointer:not([data-bit-bdnd-preview])")
            : page.Locator(".server-check-box", new() { HasText = "Glass" });
        var bin = await BoxAsync(Bin(page));
        await WaitForQuietAsync(page, 300);
        var before = Volatile.Read(ref sent);

        if (pointer)
        {
            var from = (await BoxAsync(source)).Centre;
            await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
            await page.Mouse.DownAsync();
        }
        else
        {
            await StartDragAsync(page, source);
        }
        // 120 moves - over the page, into the bin and around inside it - each of them a dragover in the browser
        for (var i = 0; i < 4; i++)
        {
            await MoveAsync(page, new Point(bin.X + 10 + i * 20, bin.Y + 10), steps: 15);
            await MoveAsync(page, new Point(bin.Right - 10 - i * 20, bin.Bottom - 10), steps: 15);
        }
        await Expect(Bin(page)).ToHaveTextAsync("Release");
        await page.Mouse.UpAsync();
        await Expect(Bin(page)).ToHaveTextAsync(pointer ? "Dropped: Pointer (Pointer)" : "Dropped: Glass (Native)");
        await WaitForQuietAsync(page, 300);
        var messages = Volatile.Read(ref sent) - before;

        // The begin, the hover of the bin, the drop and the end, with the render acknowledgements they cause (16 when
        // measured) - not one message per move: hovers are reported when they change, and moves only to a drag layer
        // that draws the dragged type (the page's layer draws "layered" only; with no Accept it took 250).
        TestContext.WriteLine($"{messages} messages over the circuit for a {(pointer ? "pointer" : "native")} drag of 120 moves");
        Assert.IsLessThan(30, messages, $"{messages} messages for one drag: the moves are reaching .NET");
    }

    [TestMethod]
    public async Task A_drag_layer_follows_the_pointer_over_the_circuit()
    {
        var page = await OpenServerPageAsync();
        var layered = page.Locator(".server-check-layered");
        var box = await BoxAsync(layered);

        await StartDragAsync(page, layered);
        await MoveAsync(page, box.Centre.Offset(60, 40));
        var preview = page.Locator(".server-check-preview");
        await Expect(preview).ToBeVisibleAsync();
        // drawn at the source's corner plus the distance travelled (the measured start rectangle arrives a round trip
        // later on the server, so the check waits for it)
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
    public async Task Insertion_edges_reorder_over_the_circuit()
    {
        var page = await OpenServerPageAsync();
        var three = await BoxAsync(page.Locator(".server-check-row", new() { HasText = "Three" }));

        await StartDragAsync(page, page.Locator(".server-check-row", new() { HasText = "One" }));
        await DragToAsync(page, new Point(three.Centre.X, three.Y + three.Height * 0.8),
            () => Expect(page.Locator(".indicator-slot", new() { Has = page.Locator(".server-check-row", new() { HasText = "Three" }) }))
                .ToHaveAttributeAsync("data-bit-bdnd-edge", "bottom"));

        await Expect(page.Locator(".server-check-order")).ToHaveTextAsync("Order: Two, Three, One");
    }

    [TestMethod]
    public async Task Dropped_file_contents_stream_to_the_server()
    {
        var page = await OpenServerPageAsync();
        var zone = page.Locator(".server-check-files");
        var files = await NativeDataTransferAsync(page, "dt.items.add(new File(['hello from the browser'], 'note.txt', { type: 'text/plain' }))");

        await DispatchDragAsync(zone, "dragenter", files);
        await DispatchDragAsync(zone, "dragover", files);
        await Expect(zone).ToHaveTextAsync("Release the files");
        await DispatchDragAsync(zone, "drop", files);

        await Expect(page.Locator(".server-check-file-contents")).ToHaveTextAsync("note.txt: hello from the browser");
    }
}
