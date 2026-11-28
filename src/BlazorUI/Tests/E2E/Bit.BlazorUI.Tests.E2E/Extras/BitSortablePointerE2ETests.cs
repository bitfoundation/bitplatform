namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitSortable drags made with pointer events rather than the browser's own drag and drop, on /e2e/sortable/touch, in a
/// touch-capable context: a finger, a pen, and the mouse on a list with ForceFallback. The item stays in its list,
/// dimmed, while a preview of it follows the pointer; a long press picks an item up on a touch screen while a swipe does
/// not; a handle with no TouchDelay drags at once and the row outside it does not; Escape puts the item back; a finger
/// dragging a multi-drag selection shows the count on its preview.
/// </summary>
/// <remarks>
/// Chromium is driven with real touch and pen input through the DevTools Protocol; Firefox and WebKit, which speak no
/// such protocol, with the pointer events a finger or a pen raises, dispatched on the element under the point (see
/// <see cref="BitSortableE2ETestBase.PointerStepAsync"/>).
/// </remarks>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortablePointerE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/touch";
    private static readonly string[] LongPress = ["Apples", "Bread", "Coffee", "Dates", "Eggs"];
    private static readonly string[] Handle = ["Flour", "Grapes", "Honey", "Ice", "Jam"];
    private static readonly string[] Fallback = ["Kale", "Lemons", "Milk", "Nuts", "Oats"];

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 1280, Height = 1400 },
        DeviceScaleFactor = 1,
        HasTouch = true,
    };

    [TestMethod]
    public async Task Touch_ALongPressPicksAnItemUp_AndAFingerDragsIt()
    {
        await OpenAsync(Url);
        var from = await CentreOfAsync("touch-list", "Apples");
        var to = (await BoxAsync(Item("touch-list", "Dates"))).At(0.5, 0.7);

        await PointerStepAsync("touch", "start", from);
        await Task.Delay(400); // longer than TouchDelay
        await GlideAsync("touch", from, to);

        // Mid-drag: the preview follows the finger and the item, dimmed, has moved in its list.
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(1);
        await Expect(Item("touch-list", "Apples")).ToHaveClassAsync(HasClass("bit-srt-gst"));
        await AssertLabelsAsync("touch-list", ["Bread", "Coffee", "Dates", "Apples", "Eggs"]);

        await PointerStepAsync("touch", "end", to);
        await WaitForQuietAsync();

        await AssertOrderAsync("touch-list", "touch-order", "Bread", "Coffee", "Dates", "Apples", "Eggs");
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(0);
        await Expect(Page.Locator(".bit-srt-gst")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Touch_ASwipeWithoutTheLongPress_DragsNothing()
    {
        await OpenAsync(Url);
        var from = await CentreOfAsync("touch-list", "Apples");
        var to = await CentreOfAsync("touch-list", "Dates");

        await PointerStepAsync("touch", "start", from);
        await GlideAsync("touch", from, to, steps: 4);
        await PointerStepAsync("touch", "end", to);
        await WaitForQuietAsync();

        await AssertOrderAsync("touch-list", "touch-order", LongPress);
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Touch_AHandleWithoutATouchDelay_DragsAtOnce()
    {
        await OpenAsync(Url);
        var from = (await BoxAsync(Item("touch-handle-list", "Flour").Locator(".handle"))).Centre;
        var to = (await BoxAsync(Item("touch-handle-list", "Ice"))).At(0.5, 0.7);

        await PointerStepAsync("touch", "start", from);
        await GlideAsync("touch", from, to);
        await PointerStepAsync("touch", "end", to);
        await WaitForQuietAsync();

        await AssertOrderAsync("touch-handle-list", "touch-handle-order", "Grapes", "Honey", "Ice", "Flour", "Jam");
    }

    [TestMethod]
    public async Task Touch_AFingerOnTheRowOutsideTheHandle_DragsNothing()
    {
        await OpenAsync(Url);
        var from = (await BoxAsync(Item("touch-handle-list", "Flour"))).At(0.8, 0.5);
        var to = (await BoxAsync(Item("touch-handle-list", "Ice"))).At(0.8, 0.7);

        await PointerStepAsync("touch", "start", from);
        await Task.Delay(400);
        await GlideAsync("touch", from, to);
        await PointerStepAsync("touch", "end", to);
        await WaitForQuietAsync();

        await AssertOrderAsync("touch-handle-list", "touch-handle-order", Handle);
    }

    [TestMethod]
    public async Task Pen_DragsWithoutALongPress()
    {
        await OpenAsync(Url);
        var from = await CentreOfAsync("touch-list", "Eggs");
        var to = (await BoxAsync(Item("touch-list", "Bread"))).At(0.5, 0.3);

        await PointerStepAsync("pen", "start", from);
        await GlideAsync("pen", from, to);
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(1);
        await PointerStepAsync("pen", "end", to);
        await WaitForQuietAsync();

        await AssertOrderAsync("touch-list", "touch-order", "Apples", "Eggs", "Bread", "Coffee", "Dates");
    }

    [TestMethod]
    public async Task ForceFallback_TheMouseDragsAPreview_InsteadOfANativeDrag()
    {
        await OpenAsync(Url);
        await Page.EvaluateAsync("() => { window.__nativeDrags = 0; document.addEventListener('dragstart', () => window.__nativeDrags++, true); }");

        await PressAsync(await CentreOfAsync("touch-fallback-list", "Kale"));
        await MoveAsync((await BoxAsync(Item("touch-fallback-list", "Nuts"))).At(0.5, 0.7));
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(1);
        await ReleaseAsync();

        await AssertOrderAsync("touch-fallback-list", "touch-fallback-order", "Lemons", "Milk", "Nuts", "Kale", "Oats");
        Assert.AreEqual(0, await Page.EvaluateAsync<int>("() => window.__nativeDrags"), "No native drag ran.");
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ForceFallback_Escape_PutsAPointerDragBack()
    {
        await OpenAsync(Url);

        await PressAsync(await CentreOfAsync("touch-fallback-list", "Kale"));
        await MoveAsync((await BoxAsync(Item("touch-fallback-list", "Nuts"))).At(0.5, 0.7));
        await AssertLabelsAsync("touch-fallback-list", ["Lemons", "Milk", "Nuts", "Kale", "Oats"]);

        await Page.Keyboard.PressAsync("Escape");
        await WaitForQuietAsync();
        await ReleaseAsync();

        await AssertOrderAsync("touch-fallback-list", "touch-fallback-order", Fallback);
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ForceFallback_SpaceDuringAPointerDrag_StartsNothingAndLeavesNothingBehind()
    {
        await OpenAsync(Url);
        var kale = Item("touch-fallback-list", "Kale");
        await kale.FocusAsync();

        await PressAsync((await BoxAsync(kale)).Centre);
        await MoveAsync((await BoxAsync(Item("touch-fallback-list", "Milk"))).At(0.5, 0.7));
        await Page.Keyboard.PressAsync(" ");
        await WaitForQuietAsync();
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(1);
        await ReleaseAsync();

        await AssertOrderAsync("touch-fallback-list", "touch-fallback-order", "Lemons", "Milk", "Kale", "Nuts", "Oats");
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-bit-srt-accepts], [data-bit-srt-over]")).ToHaveCountAsync(0);
        Assert.AreEqual("", await Page.EvaluateAsync<string>("() => document.documentElement.style.userSelect"));
    }

    [TestMethod]
    public async Task Touch_AFingerCarriesAnItemIntoAnotherList()
    {
        await OpenAsync(Url);
        var from = await CentreOfAsync("shared-a", "A1");
        var to = (await BoxAsync(Item("shared-b", "B3"))).At(0.5, 0.3);

        await PointerStepAsync("touch", "start", from);
        await Task.Delay(400);
        await GlideAsync("touch", from, to);
        await AssertLabelsAsync("shared-b", ["B1", "B2", "[placeholder: A1]", "B3", "B4", "B5"], withPlaceholder: true);
        await PointerStepAsync("touch", "end", to);
        await WaitForQuietAsync();

        await AssertOrderAsync("shared-a", "shared-a-order", "A2", "A3", "A4", "A5");
        await AssertOrderAsync("shared-b", "shared-b-order", "B1", "B2", "A1", "B3", "B4", "B5");
    }

    [TestMethod]
    public async Task Touch_AFingerDraggingASelection_ShowsTheCountOnItsPreview()
    {
        await OpenAsync("/e2e/sortable/multi-drag");
        await Item("multi-list", "Item 1").ClickAsync();
        await Item("multi-list", "Item 3").ClickAsync();
        await Expect(Page.Locator("#multi-list > [data-bit-srt-selected]")).ToHaveCountAsync(2);

        var from = await CentreOfAsync("multi-list", "Item 1");
        var to = (await BoxAsync(Item("multi-list", "Item 5"))).At(0.5, 0.7);
        await PointerStepAsync("touch", "start", from);
        await Task.Delay(400); // longer than TouchDelay
        await GlideAsync("touch", from, to);

        await Expect(Page.Locator(Preview)).ToHaveAttributeAsync("data-bit-srt-count", "2");
        await Expect(Page.Locator($"{Preview} .bit-srt-cnt")).ToHaveTextAsync("2");
        // The preview is a picture: it carries no state of the item's own.
        await Expect(Page.Locator(Preview)).Not.ToHaveAttributeAsync("data-bit-srt-selected", "true");

        await PointerStepAsync("touch", "end", to);
        await WaitForQuietAsync();
        await Expect(Page.Locator(Preview)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Axe_EachSection_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#touch");
        await AssertNoAxeViolationsAsync("#touch-shared");
    }
}
