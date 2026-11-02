using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// Drags made with pointer events rather than the browser's own drag and drop: a finger (real touch events, sent
/// through the Chrome DevTools Protocol), a pen, and the mouse on a list with <c>ForceFallback</c>. The item stays in
/// its list, dimmed, while a preview of it follows the pointer; a long press picks an item up on a touch screen while
/// a swipe does not; Escape puts the item back.
/// </summary>
[TestClass]
public sealed class PointerTests : BortableTest
{
    private static readonly string[] LongPress = ["Apples", "Bread", "Coffee", "Dates", "Eggs"];
    private static readonly string[] Handle = ["Flour", "Grapes", "Honey", "Ice", "Jam"];
    private static readonly string[] Fallback = ["Kale", "Lemons", "Milk", "Nuts", "Oats"];

    private const string Preview = ".bit-bortable-drag";

    private async Task<(IPage Page, ICDPSession Cdp)> OpenTouchAsync()
    {
        if (E2EEnvironment.IsChromium is false)
            Assert.Inconclusive($"Touch and pen input is sent through the Chrome DevTools Protocol, which {E2EEnvironment.Browser} does not speak.");

        var page = await OpenAsync("/touch", new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 1400 },
            DeviceScaleFactor = 1,
            HasTouch = true,
        });
        return (page, await page.Context.NewCDPSessionAsync(page));
    }

    private static Task TouchAsync(ICDPSession cdp, string type, Point at) =>
        cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = type == "touchEnd"
                ? Array.Empty<object>()
                : new object[] { new Dictionary<string, object> { ["x"] = at.X, ["y"] = at.Y, ["id"] = 1 } },
        });

    private static Task PenAsync(ICDPSession cdp, string type, Point at) =>
        cdp.SendAsync("Input.dispatchMouseEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["x"] = at.X,
            ["y"] = at.Y,
            ["button"] = "left",
            ["buttons"] = type == "mouseReleased" ? 0 : 1,
            ["clickCount"] = 1,
            ["pointerType"] = "pen",
        });

    /// <summary>Moves a touch (or a pen) from one point to another in small steps, as a hand would, then lets the page settle.</summary>
    private static async Task GlideAsync(IPage page, Func<Point, Task> move, Point from, Point to, int steps = 12)
    {
        for (int i = 1; i <= steps; i++)
        {
            await move(new Point(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps));
            await Task.Delay(16);
        }
        await WaitForQuietAsync(page);
    }

    [TestMethod]
    public async Task A_long_press_picks_an_item_up_and_a_finger_drags_it()
    {
        var (page, cdp) = await OpenTouchAsync();
        var from = (await BoxAsync(Item(page, "touch-list", "Apples"))).Centre;
        var to = (await BoxAsync(Item(page, "touch-list", "Dates"))).At(0.5, 0.7);

        await TouchAsync(cdp, "touchStart", from);
        await Task.Delay(400); // longer than TouchDelay
        await GlideAsync(page, p => TouchAsync(cdp, "touchMove", p), from, to);

        // Mid-drag: the preview follows the finger and the item, dimmed, has moved in its list.
        await Expect(page.Locator(Preview)).ToHaveCountAsync(1);
        await Expect(Item(page, "touch-list", "Apples")).ToHaveClassAsync(HasClass("demo-ghost"));
        await AssertLabelsAsync(page, "touch-list", ["Bread", "Coffee", "Dates", "Apples", "Eggs"]);

        await TouchAsync(cdp, "touchEnd", to);
        await WaitForQuietAsync(page);

        await AssertOrderAsync(page, "touch-list", "touch-order", "Bread", "Coffee", "Dates", "Apples", "Eggs");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await Expect(page.Locator(".demo-ghost")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_swipe_without_the_long_press_drags_nothing()
    {
        var (page, cdp) = await OpenTouchAsync();
        var from = (await BoxAsync(Item(page, "touch-list", "Apples"))).Centre;
        var to = (await BoxAsync(Item(page, "touch-list", "Dates"))).Centre;

        await TouchAsync(cdp, "touchStart", from);
        await GlideAsync(page, p => TouchAsync(cdp, "touchMove", p), from, to, steps: 4);
        await TouchAsync(cdp, "touchEnd", to);
        await WaitForQuietAsync(page);

        await AssertOrderAsync(page, "touch-list", "touch-order", LongPress);
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_handle_without_a_touch_delay_drags_at_once()
    {
        var (page, cdp) = await OpenTouchAsync();
        var from = (await BoxAsync(Item(page, "touch-handle-list", "Flour").Locator(".handle"))).Centre;
        var to = (await BoxAsync(Item(page, "touch-handle-list", "Ice"))).At(0.5, 0.7);

        await TouchAsync(cdp, "touchStart", from);
        await GlideAsync(page, p => TouchAsync(cdp, "touchMove", p), from, to);
        await TouchAsync(cdp, "touchEnd", to);
        await WaitForQuietAsync(page);

        await AssertOrderAsync(page, "touch-handle-list", "touch-handle-order", "Grapes", "Honey", "Ice", "Flour", "Jam");
    }

    [TestMethod]
    public async Task A_finger_on_the_row_outside_the_handle_drags_nothing()
    {
        var (page, cdp) = await OpenTouchAsync();
        var row = await BoxAsync(Item(page, "touch-handle-list", "Flour"));
        var from = row.At(0.8, 0.5);
        var to = (await BoxAsync(Item(page, "touch-handle-list", "Ice"))).At(0.8, 0.7);

        await TouchAsync(cdp, "touchStart", from);
        await Task.Delay(400);
        await GlideAsync(page, p => TouchAsync(cdp, "touchMove", p), from, to);
        await TouchAsync(cdp, "touchEnd", to);
        await WaitForQuietAsync(page);

        await AssertOrderAsync(page, "touch-handle-list", "touch-handle-order", Handle);
    }

    [TestMethod]
    public async Task A_pen_drags_without_a_long_press()
    {
        var (page, cdp) = await OpenTouchAsync();
        var from = (await BoxAsync(Item(page, "touch-list", "Eggs"))).Centre;
        var to = (await BoxAsync(Item(page, "touch-list", "Bread"))).At(0.5, 0.3);

        await PenAsync(cdp, "mousePressed", from);
        await GlideAsync(page, p => PenAsync(cdp, "mouseMoved", p), from, to);
        await Expect(page.Locator(Preview)).ToHaveCountAsync(1);
        await PenAsync(cdp, "mouseReleased", to);
        await WaitForQuietAsync(page);

        await AssertOrderAsync(page, "touch-list", "touch-order", "Apples", "Eggs", "Bread", "Coffee", "Dates");
    }

    [TestMethod]
    public async Task With_ForceFallback_the_mouse_drags_a_preview_instead_of_a_native_drag()
    {
        var page = await OpenAsync("/touch");
        await page.EvaluateAsync("() => { window.__nativeDrags = 0; document.addEventListener('dragstart', () => window.__nativeDrags++, true); }");

        await PressAsync(page, (await BoxAsync(Item(page, "touch-fallback-list", "Kale"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "touch-fallback-list", "Nuts"))).At(0.5, 0.7));
        await Expect(page.Locator(Preview)).ToHaveCountAsync(1);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "touch-fallback-list", "touch-fallback-order", "Lemons", "Milk", "Nuts", "Kale", "Oats");
        Assert.AreEqual(0, await page.EvaluateAsync<int>("() => window.__nativeDrags"), "No native drag ran.");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Escape_puts_a_pointer_drag_back()
    {
        var page = await OpenAsync("/touch");

        await PressAsync(page, (await BoxAsync(Item(page, "touch-fallback-list", "Kale"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "touch-fallback-list", "Nuts"))).At(0.5, 0.7));
        await AssertLabelsAsync(page, "touch-fallback-list", ["Lemons", "Milk", "Nuts", "Kale", "Oats"]);

        await page.Keyboard.PressAsync("Escape");
        await WaitForQuietAsync(page);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "touch-fallback-list", "touch-fallback-order", Fallback);
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Space_during_a_pointer_drag_starts_nothing_and_leaves_nothing_behind()
    {
        var page = await OpenAsync("/touch");
        var kale = Item(page, "touch-fallback-list", "Kale");
        await kale.FocusAsync();

        await PressAsync(page, (await BoxAsync(kale)).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "touch-fallback-list", "Milk"))).At(0.5, 0.7));
        await page.Keyboard.PressAsync(" ");
        await WaitForQuietAsync(page);
        await Expect(page.Locator(Preview)).ToHaveCountAsync(1);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "touch-fallback-list", "touch-fallback-order", "Lemons", "Milk", "Kale", "Nuts", "Oats");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-bortable-accepts], [data-bortable-over]")).ToHaveCountAsync(0);
        Assert.AreEqual("", await page.EvaluateAsync<string>("() => document.documentElement.style.userSelect"));
    }

    [TestMethod]
    public async Task A_finger_carries_an_item_into_another_list()
    {
        var (page, cdp) = await OpenTouchAsync();
        await page.GotoAsync(BaseUrl + "/shared", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForListsAsync(page);

        var from = (await BoxAsync(Item(page, "shared-a", "A1"))).Centre;
        var to = (await BoxAsync(Item(page, "shared-b", "B3"))).At(0.5, 0.3);

        await TouchAsync(cdp, "touchStart", from);
        await Task.Delay(400);
        await GlideAsync(page, p => TouchAsync(cdp, "touchMove", p), from, to);
        await AssertLabelsAsync(page, "shared-b", ["B1", "B2", "[placeholder: A1]", "B3", "B4", "B5"], withPlaceholder: true);
        await TouchAsync(cdp, "touchEnd", to);
        await WaitForQuietAsync(page);

        await AssertOrderAsync(page, "shared-a", "shared-a-order", "A2", "A3", "A4", "A5");
        await AssertOrderAsync(page, "shared-b", "shared-b-order", "B1", "B2", "A1", "B3", "B4", "B5");
    }
}
