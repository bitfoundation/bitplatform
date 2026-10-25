using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitSwipeTrap on a phone (/e2e/swipe-trap at a phone's viewport with touch): a touch swipe reported with its
/// pointer type, TouchOnly taking the touch it refuses from the mouse, the orientation lock leaving the free axis to
/// the page's own scroll while the trapped one does not scroll it, a disabled trap the page scrolls over, and the
/// mouse still swiping on a touch device. The touches are dispatched through the Chromium DevTools protocol, which
/// drives the browser's real touch pipeline (scrolling included); the other engines have no such input and skip.
/// </summary>
[TestClass]
public class BitSwipeTrapTouchE2ETests : E2ETestBase
{
    private const string Route = "/e2e/swipe-trap";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        IsMobile = IsFirefox is false
    };

    [TestMethod]
    public async Task TouchSwipe_Right_IsReportedAsATouch_AndTriggers()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-basic");
        await TouchDragAsync(x, y, 100, 0);

        await Expect(Page.Locator("#basic-ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-starts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-start")).ToHaveTextAsync($"{x},{y}|0,0|00|touch|False|nodur");
        await Expect(Page.Locator("#basic-end")).ToHaveTextAsync($"{x},{y}|100,0|+0|touch|False|dur");
        await Expect(Page.Locator("#basic-trigger")).ToHaveTextAsync("Right|100,0|+0|touch|dur");
    }

    [TestMethod]
    public async Task TouchSwipe_Down_TriggersBottom_WithoutScrollingThePage()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-lock-Unset");
        var scrollY = await Page.EvaluateAsync<double>("() => window.scrollY");

        // Upward: the content would scroll down if the page had it.
        await TouchDragAsync(x, y, 0, -80);

        await Expect(Page.Locator("#lock-Unset-trigger")).ToHaveTextAsync(new Regex("^Top\\|0,-80\\|"));
        Assert.AreEqual(scrollY, await Page.EvaluateAsync<double>("() => window.scrollY"), 1, "A trap with no lock let the page scroll.");
    }

    [TestMethod]
    public async Task TouchOnly_ATouchSwipeIsTrapped()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-touchonly");
        await TouchDragAsync(x, y, -100, 0);

        await Expect(Page.Locator("#touchonly-starts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#touchonly-trigger")).ToHaveTextAsync(new Regex("^Left\\|-100,0\\|.*\\|touch\\|"));
    }

    [TestMethod]
    public async Task HorizontalLock_AVerticalTouchDragScrollsThePage_AndTriggersNothing()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-lock-Horizontal");
        var scrollY = await Page.EvaluateAsync<double>("() => window.scrollY");

        await TouchDragAsync(x, y, 0, -120);

        await Page.WaitForFunctionAsync("y => window.scrollY > y + 20", scrollY);
        await Expect(Page.Locator("#lock-Horizontal-triggers")).ToHaveTextAsync("0");

        // Sideways it is the trap's.
        (x, y) = await StartPointAsync("#stp-lock-Horizontal");
        await TouchDragAsync(x, y, 100, 0);
        await Expect(Page.Locator("#lock-Horizontal-trigger")).ToHaveTextAsync(new Regex("^Right\\|100,0\\|"));
    }

    [TestMethod]
    public async Task Disabled_ThePageScrollsOverIt()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-disabled");
        var scrollY = await Page.EvaluateAsync<double>("() => window.scrollY");

        await TouchDragAsync(x, y, 0, -120);

        await Page.WaitForFunctionAsync("y => window.scrollY > y + 20", scrollY);
        await Expect(Page.Locator("#disabled-starts")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Mouse_StillSwipesOnATouchDevice()
    {
        await OpenAsync(Route);

        var (x, y) = await StartPointAsync("#stp-basic");
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 100, y, new() { Steps = 10 });
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#basic-trigger")).ToHaveTextAsync(new Regex("^Right\\|100,0\\|.*\\|mouse\\|"));
    }

    private static void RequireChromium()
    {
        if (IsChromium is false) Assert.Inconclusive("Real touch input is dispatched through the Chromium DevTools protocol, which this engine does not have.");
    }

    private async Task<(float X, float Y)> StartPointAsync(string selector)
    {
        var trap = Page.Locator(selector);
        await trap.ScrollIntoViewIfNeededAsync();
        var box = (await trap.BoundingBoxAsync())!;

        return (MathF.Round(box.X + box.Width / 2), MathF.Round(box.Y + box.Height / 2));
    }

    // A one-finger drag through the browser's touch pipeline, a frame between moves as a finger would.
    private async Task TouchDragAsync(float x, float y, float dx, float dy, int steps = 10)
    {
        var cdp = await Page.Context.NewCDPSessionAsync(Page);

        await cdp.SendAsync("Input.dispatchTouchEvent", Touch("touchStart", x, y));
        for (var i = 1; i <= steps; i++)
        {
            await cdp.SendAsync("Input.dispatchTouchEvent", Touch("touchMove", x + dx * i / steps, y + dy * i / steps));
        }
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>() });

        await cdp.DetachAsync();
    }

    private static Dictionary<string, object> Touch(string type, float x, float y) => new()
    {
        ["type"] = type,
        ["touchPoints"] = new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y, ["id"] = 1 } }
    };
}
