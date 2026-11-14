namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitLongPress on a phone (/e2e/long-press at a phone's viewport with touch): a held touch raising OnLongPress as a
/// touch, the tap of a short touch left to the content, the click a long touch would make swallowed, a touch that
/// scrolls the page calling the press off (and the page still scrolling), TouchOnly taking the touch, and the browser's
/// own menu kept out from under a held touch. The touches are dispatched through the Chromium DevTools protocol, which
/// drives the browser's real touch pipeline (scrolling included); the other engines have no such input and skip.
/// </summary>
[TestClass]
public class BitLongPressTouchE2ETests : E2ETestBase
{
    private const string Route = "/e2e/long-press";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        IsMobile = IsFirefox is false
    };

    private void RequireChromium()
    {
        if (IsChromium is false) Assert.Inconclusive("Real touch input is dispatched through the Chromium DevTools protocol, which this engine does not have.");
    }

    private async Task<(float X, float Y)> CenterAsync(string selector)
    {
        var element = Page.Locator(selector);
        await element.ScrollIntoViewIfNeededAsync();
        var box = (await element.BoundingBoxAsync())!;

        return (MathF.Round(box.X + box.Width / 2), MathF.Round(box.Y + box.Height / 2));
    }

    private static Dictionary<string, object> Touch(string type, float x, float y) => new()
    {
        ["type"] = type,
        ["touchPoints"] = new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y, ["id"] = 1 } }
    };

    private static Dictionary<string, object> TouchEnd() => new() { ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>() };

    private async Task TouchHoldAsync(float x, float y, int milliseconds)
    {
        var cdp = await Page.Context.NewCDPSessionAsync(Page);

        await cdp.SendAsync("Input.dispatchTouchEvent", Touch("touchStart", x, y));
        await Page.WaitForTimeoutAsync(milliseconds);
        await cdp.SendAsync("Input.dispatchTouchEvent", TouchEnd());

        await cdp.DetachAsync();
    }

    [TestMethod]
    public async Task ATouchHeld_RaisesOnLongPressAsATouch()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-basic");
        await TouchHoldAsync(x, y, 800);

        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-last")).ToHaveTextAsync($"touch|{x},{y}");
    }

    [TestMethod]
    public async Task AShortTouch_IsATapOfTheContent_AndALongOneIsNot()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#basic-child");

        await TouchHoldAsync(x, y, 50);
        await Expect(Page.Locator("#basic-child-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("0");

        await TouchHoldAsync(x, y, 800);
        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("1");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#basic-child-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ATouchThatScrollsThePage_CallsThePressOff_AndThePageStillScrolls()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-basic");
        var scrollY = await Page.EvaluateAsync<double>("() => window.scrollY");

        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await cdp.SendAsync("Input.dispatchTouchEvent", Touch("touchStart", x, y));
        for (var i = 1; i <= 10; i++)
        {
            await cdp.SendAsync("Input.dispatchTouchEvent", Touch("touchMove", x, y - 12 * i));
        }
        await Page.WaitForTimeoutAsync(800);
        await cdp.SendAsync("Input.dispatchTouchEvent", TouchEnd());
        await cdp.DetachAsync();

        await Page.WaitForFunctionAsync("y => window.scrollY > y + 20", scrollY);
        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task TouchOnly_TakesTheTouch()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-touchonly");
        await TouchHoldAsync(x, y, 800);

        await Expect(Page.Locator("#touchonly-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#touchonly-last")).ToHaveTextAsync($"touch|{x},{y}");
    }

    [TestMethod]
    public async Task UnderAHeldTouch_TheBrowsersOwnMenuIsKeptOut()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-basic");

        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await cdp.SendAsync("Input.dispatchTouchEvent", Touch("touchStart", x, y));
        await Page.WaitForTimeoutAsync(200);

        // The contextmenu a held touch raises on some browsers, dispatched while the touch is down.
        var prevented = await Page.Locator("#lpr-basic .e2e-lpr-box").EvaluateAsync<bool>(
            "el => { const e = new MouseEvent('contextmenu', { bubbles: true, cancelable: true }); el.dispatchEvent(e); return e.defaultPrevented; }");

        await cdp.SendAsync("Input.dispatchTouchEvent", TouchEnd());
        await cdp.DetachAsync();

        Assert.IsTrue(prevented, "the browser's menu came up over a held touch.");
    }
}
