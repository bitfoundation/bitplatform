using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Pointer drags in the browser: a touch that rests on a source starts a drag (and a swipe still scrolls), the mouse
/// drags a <c>PointerDrag="All"</c> source through pointer events with no native drag at all, a copy of the source
/// follows the pointer, Escape cancels, a press in a no-drag region drags nothing, and the tray under the pointer
/// scrolls near its edge.
/// </summary>
[TestClass]
public sealed class PointerTests : BdndTest
{
    private const string Preview = "[data-bit-bdnd-preview]";

    [TestMethod]
    public async Task A_touch_that_rests_on_a_box_drags_it_onto_the_dustbin()
    {
        RequireChromium("touches are synthesized through the DevTools protocol");
        var page = await OpenAsync("/dustbin/single", hasTouch: true);
        await WaitForBdndAsync(page.MainFrame);
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box:not([data-bit-bdnd-preview] *)", new() { HasText = "Glass" });
        var from = (await BoxAsync(glass)).Centre;
        var to = (await BoxAsync(bin)).Centre;
        var cdp = await TouchScreenAsync(page);

        await TouchAsync(cdp, "touchStart", from);
        await Expect(page.Locator("[data-bit-bdnd-preview]")).ToHaveCountAsync(1); // it rested for the TouchDelay: the drag began
        await TouchMoveAsync(cdp, from, to);
        await Expect(bin).ToHaveTextAsync("Release to drop");
        await Expect(glass).ToHaveCSSAsync("opacity", "0.4");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(1);
        await TouchAsync(cdp, "touchEnd");

        await Expect(bin).ToHaveTextAsync("Last dropped: Glass (total 1)");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await Expect(glass).ToHaveCSSAsync("opacity", "1");
        Assert.IsEmpty(await DragStartsAsync(page.MainFrame), "no native drag was involved");
    }

    // Pointer events of type touch, made by the page itself: what a touch screen raises, on every engine - the module
    // works from pointer events alone, so Firefox and WebKit run the touch path here too (layout, hit testing, the
    // copy in the top layer). Real devices add only what raises the events.
    private const string SyntheticTouch =
        """
        ([type, x, y, buttons]) => {
          const target = type === 'pointerdown' ? document.elementFromPoint(x, y) : (window.__touchTarget ?? document);
          if (type === 'pointerdown') window.__touchTarget = target;
          target.dispatchEvent(new PointerEvent(type, {
            pointerId: 7, pointerType: 'touch', isPrimary: true, bubbles: true, cancelable: true, composed: true,
            clientX: x, clientY: y, button: type === 'pointermove' ? -1 : 0, buttons, width: 20, height: 20,
          }));
        }
        """;

    [TestMethod]
    public async Task A_touch_drag_through_pointer_events_drops_on_every_engine()
    {
        var page = await OpenAsync("/dustbin/single", hasTouch: true);
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box:not([data-bit-bdnd-preview] *)", new() { HasText = "Glass" });
        var from = (await BoxAsync(glass)).Centre;
        var to = (await BoxAsync(bin)).Centre;
        Task TouchAsync(string type, Point at, int buttons) => page.EvaluateAsync(SyntheticTouch, new object[] { type, at.X, at.Y, buttons });

        await TouchAsync("pointerdown", from, 1);
        await Expect(page.Locator(Preview)).ToHaveCountAsync(1); // it rested for the TouchDelay: the drag began
        for (var i = 1; i <= 8; i++)
        {
            await TouchAsync("pointermove", new Point(from.X + (to.X - from.X) * i / 8, from.Y + (to.Y - from.Y) * i / 8), 1);
        }
        await Expect(bin).ToHaveTextAsync("Release to drop");
        await Expect(glass).ToHaveCSSAsync("opacity", "0.4");
        await TouchAsync("pointerup", to, 0);

        await Expect(bin).ToHaveTextAsync("Last dropped: Glass (total 1)");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await Expect(glass).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task A_quick_swipe_over_a_box_drags_nothing()
    {
        RequireChromium("touches are synthesized through the DevTools protocol");
        var page = await OpenAsync("/dustbin/single", hasTouch: true);
        await WaitForBdndAsync(page.MainFrame);
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box:not([data-bit-bdnd-preview] *)", new() { HasText = "Glass" });
        var from = (await BoxAsync(glass)).Centre;
        var cdp = await TouchScreenAsync(page);

        await TouchAsync(cdp, "touchStart", from);
        await TouchMoveAsync(cdp, from, (await BoxAsync(bin)).Centre, steps: 4);
        await TouchAsync(cdp, "touchEnd");
        await WaitForQuietAsync(page);

        await Expect(bin).ToHaveTextAsync("Drag a box here");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await Expect(glass).ToHaveCSSAsync("opacity", "1");
    }

    // ---- the mouse, on sources that drag through pointer events ----

    private static ILocator Slot(IPage page, int slot) =>
        page.Locator(".pointer-slot", new() { Has = page.Locator("span", new() { HasTextRegex = new($"^Slot {slot}$") }) });

    [TestMethod]
    public async Task The_mouse_drags_a_pointer_source_with_a_preview_and_no_native_drag()
    {
        var page = await OpenAsync("/customize/pointer");
        var glass = page.Locator(".pointer-box:not([data-bit-bdnd-preview])", new() { HasText = "Glass" });
        var slot = Slot(page, 2);
        var from = (await BoxAsync(glass)).Centre;
        var to = (await BoxAsync(slot)).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, from.Offset(40, 30));
        var preview = page.Locator(Preview);
        await Expect(preview).ToHaveCountAsync(1);
        // shown in the top layer: above every stacking context, clipped by no ancestor
        Assert.IsTrue(await preview.EvaluateAsync<bool>("el => el.matches(':popover-open')"), "the preview is an open popover");
        await Expect(glass).ToHaveAttributeAsync("data-bit-bdnd-dragging", "");
        // the copy sits where the box would be: moved by the distance the pointer travelled (centres compared: the
        // demo tilts the copy, which widens its bounding box)
        var original = (await BoxAsync(glass)).Centre;
        await EventuallyAsync(async () =>
        {
            var b = await preview.BoundingBoxAsync();
            return b is not null && Math.Abs(b.X + b.Width / 2 - original.X - 40) < 2 && Math.Abs(b.Y + b.Height / 2 - original.Y - 30) < 2;
        }, "the preview follows the pointer");

        await MoveAsync(page, to);
        await Expect(slot).ToHaveAttributeAsync("data-bit-bdnd-over", "");
        await page.Mouse.UpAsync();

        await Expect(page.Locator(".pointer-status")).ToHaveTextAsync("Glass put in slot 2.");
        await Expect(slot.Locator(".pointer-slot__content")).ToHaveTextAsync("Glass");
        await Expect(preview).ToHaveCountAsync(0);
        Assert.IsEmpty(await DragStartsAsync(page.MainFrame), "no native dragstart");
    }

    [TestMethod]
    public async Task Escape_cancels_a_pointer_drag()
    {
        var page = await OpenAsync("/customize/pointer");
        var glass = page.Locator(".pointer-box:not([data-bit-bdnd-preview])", new() { HasText = "Glass" });
        var slot = Slot(page, 1);
        var from = (await BoxAsync(glass)).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, (await BoxAsync(slot)).Centre);
        await Expect(slot).ToHaveAttributeAsync("data-bit-bdnd-over", "");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);

        await Expect(page.Locator(".pointer-status")).ToHaveTextAsync("Nothing put anywhere yet.");
        await Expect(glass).Not.ToHaveAttributeAsync("data-bit-bdnd-dragging", "");
        await Expect(slot).Not.ToHaveAttributeAsync("data-bit-bdnd-over", "");
    }

    [TestMethod]
    [DataRow("window.dispatchEvent(new Event('blur'))", DisplayName = "the window loses the focus")]
    [DataRow("Object.defineProperty(document, 'visibilityState', { value: 'hidden', configurable: true }); document.dispatchEvent(new Event('visibilitychange'))",
        DisplayName = "the page is hidden")]
    public async Task A_pointer_drag_is_cancelled_when(string happening)
    {
        var page = await OpenAsync("/customize/pointer");
        var glass = page.Locator(".pointer-box:not([data-bit-bdnd-preview])", new() { HasText = "Glass" });
        var slot = Slot(page, 1);
        var from = (await BoxAsync(glass)).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, (await BoxAsync(slot)).Centre);
        await Expect(slot).ToHaveAttributeAsync("data-bit-bdnd-over", "");
        await page.EvaluateAsync($"() => {{ {happening}; }}");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);

        await Expect(page.Locator(".pointer-status")).ToHaveTextAsync("Nothing put anywhere yet.");
        await Expect(glass).Not.ToHaveAttributeAsync("data-bit-bdnd-dragging", "");
        await Expect(slot).Not.ToHaveAttributeAsync("data-bit-bdnd-over", "");
    }

    [TestMethod]
    public async Task A_pen_that_rests_on_a_box_drags_it_onto_the_dustbin()
    {
        RequireChromium("pen input is synthesized through the DevTools protocol");
        var page = await OpenAsync("/dustbin/single");
        await WaitForBdndAsync(page.MainFrame);
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box:not([data-bit-bdnd-preview] *)", new() { HasText = "Glass" });
        var from = (await BoxAsync(glass)).Centre;
        var to = (await BoxAsync(bin)).Centre;
        var cdp = await page.Context.NewCDPSessionAsync(page);
        Task PenAsync(string type, Point at, int buttons) => cdp.SendAsync("Input.dispatchMouseEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["x"] = at.X,
            ["y"] = at.Y,
            ["button"] = type == "mouseMoved" ? "none" : "left",
            ["buttons"] = buttons,
            ["clickCount"] = type == "mouseMoved" ? 0 : 1,
            ["pointerType"] = "pen",
        });

        await PenAsync("mousePressed", from, 1);
        await Expect(page.Locator("[data-bit-bdnd-preview]")).ToHaveCountAsync(1); // a pen rests for the TouchDelay like a finger
        for (var i = 1; i <= 8; i++)
        {
            await PenAsync("mouseMoved", new Point(from.X + (to.X - from.X) * i / 8, from.Y + (to.Y - from.Y) * i / 8), 1);
        }
        await Expect(bin).ToHaveTextAsync("Release to drop");
        await PenAsync("mouseReleased", to, 0);

        await Expect(bin).ToHaveTextAsync("Last dropped: Glass (total 1)");
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        Assert.IsEmpty(await DragStartsAsync(page.MainFrame), "no native drag was involved");
    }

    [TestMethod]
    public async Task The_release_of_a_drag_cancelled_with_Escape_clicks_nothing()
    {
        var page = await OpenAsync("/customize/pointer");
        var glass = page.Locator(".pointer-box:not([data-bit-bdnd-preview])", new() { HasText = "Glass" });
        var info = page.GetByRole(AriaRole.Button, new() { Name = "About Banana" });
        var from = (await BoxAsync(glass)).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, (await BoxAsync(info)).Centre);
        await Expect(page.Locator(Preview)).ToHaveCountAsync(1);
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForTimeoutAsync(300); // longer than the click suppression after a release lasts
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);

        await Expect(page.Locator(".pointer-status")).ToHaveTextAsync("Nothing put anywhere yet.");
    }

    [TestMethod]
    public async Task A_press_in_a_no_drag_region_drags_nothing_and_its_button_still_clicks()
    {
        var page = await OpenAsync("/customize/pointer");
        var info = page.GetByRole(AriaRole.Button, new() { Name = "About Glass" });
        var from = (await BoxAsync(info)).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, (await BoxAsync(Slot(page, 1))).Centre);
        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await page.Mouse.UpAsync();

        await info.ClickAsync();
        await Expect(page.Locator(".pointer-status")).ToHaveTextAsync("Glass is a box.");
    }

    [TestMethod]
    public async Task A_pointer_drag_near_the_bottom_of_the_tray_scrolls_it()
    {
        var page = await OpenAsync("/customize/pointer");
        var tray = page.Locator(".pointer-tray");
        var glass = page.Locator(".pointer-box:not([data-bit-bdnd-preview])", new() { HasText = "Paper" });
        var trayBox = await BoxAsync(tray);
        var from = (await BoxAsync(glass)).Centre;

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, new Point(trayBox.Centre.X, trayBox.Bottom - 6));
        await EventuallyAsync(async () => await tray.EvaluateAsync<double>("el => el.scrollTop") > 100, "the tray scrolls down");

        // the drag goes on over what scrolled into view, and drops there
        await MoveAsync(page, new Point(trayBox.Centre.X, trayBox.Centre.Y));
        await WaitForQuietAsync(page, 100);
        var over = page.Locator(".pointer-slot[data-bit-bdnd-over]");
        await Expect(over).ToHaveCountAsync(1);
        var label = await over.Locator("span").First.TextContentAsync();
        await page.Mouse.UpAsync();

        Assert.IsNotNull(label);
        Assert.AreNotEqual("Slot 1", label, "the slot under the pointer is one the tray scrolled to");
        await Expect(page.Locator(".pointer-status")).ToHaveTextAsync($"Paper put in slot {label!["Slot ".Length..]}.");
    }

    [TestMethod]
    public async Task A_click_on_a_pointer_source_is_not_a_drag()
    {
        var page = await OpenAsync("/customize/pointer");
        var glass = page.Locator(".pointer-box:not([data-bit-bdnd-preview])", new() { HasText = "Glass" });

        await glass.ClickAsync(new() { Position = new() { X = 8, Y = 8 } });
        await WaitForQuietAsync(page);

        await Expect(page.Locator(Preview)).ToHaveCountAsync(0);
        await Expect(glass).Not.ToHaveAttributeAsync("data-bit-bdnd-dragging", "");
    }
}
