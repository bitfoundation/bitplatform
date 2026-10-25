namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// BitPullToRefresh on a phone (/e2e/pull-to-refresh at a phone's viewport with touch): a finger pull that refreshes,
/// one short of the trigger, the touch-action and overscroll-behavior the script hands the browser, NoMouse keeping
/// the pull for touch, a second finger turning the gesture into a pinch, and a pull on a scrolled list left to the
/// list. The touches are dispatched through the Chromium DevTools protocol, which drives the browser's real touch
/// pipeline; the other engines have no such input and skip.
/// </summary>
[TestClass]
public class BitPullToRefreshTouchE2ETests : E2ETestBase
{
    private const string Route = "/e2e/pull-to-refresh";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        IsMobile = IsFirefox is false
    };

    [TestMethod]
    public async Task TouchPull_PastTheTrigger_Refreshes()
    {
        RequireChromium();
        await OpenAsync(Route);

        var root = Page.Locator("#ptr-basic");
        await Expect(root).ToHaveCSSAsync("touch-action", "pan-x pan-down pinch-zoom");
        await Expect(Page.Locator("#basic-anchor")).ToHaveCSSAsync("overscroll-behavior-y", "contain");

        var (x, y) = await PullStartPointAsync("#ptr-basic");
        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await TouchAsync(cdp, "touchStart", x, y);
        for (var i = 1; i <= 15; i++)
        {
            await TouchAsync(cdp, "touchMove", x, y + i * 10);
        }

        await Expect(Page.Locator("#last-move")).ToHaveTextAsync("80");
        await Expect(root.Locator(".bit-ptr-spw")).ToHaveClassAsync(HasClass("bit-ptr-crl"));
        await Expect(root.Locator("[role=status]")).ToHaveTextAsync("Release to refresh");

        await TouchEndAsync(cdp);

        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("1");
        await Expect(root.Locator("[role=status]")).ToHaveTextAsync("Refreshing");
        await Page.Locator("#btn-finish").TapAsync();
        await Expect(root.Locator("[role=status]")).ToHaveTextAsync("");
        await Expect(Page.Locator("#states")).ToHaveTextAsync("Pulling,CanRelease,Refreshing,Idle");
        await cdp.DetachAsync();
    }

    [TestMethod]
    public async Task TouchPull_ShortOfTheTrigger_SnapsBack()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await PullStartPointAsync("#ptr-basic");
        await TouchDragAsync(x, y, 60);

        await Expect(Page.Locator("#ends")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-end")).ToHaveTextAsync("40");
        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#ptr-basic .bit-ptr-lod")).ToHaveCSSAsync("min-height", "0px");
    }

    [TestMethod]
    public async Task NoMouse_AFingerStillPulls()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await PullStartPointAsync("#ptr-nomouse");
        await TouchDragAsync(x, y, 150);

        await Expect(Page.Locator("#nomouse-starts")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#nomouse-refreshes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task SecondFinger_TurnsThePullIntoAPinch_AndCancelsIt()
    {
        RequireChromium();
        await OpenAsync(Route);

        var (x, y) = await PullStartPointAsync("#ptr-basic");
        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await TouchAsync(cdp, "touchStart", x, y);
        for (var i = 1; i <= 6; i++)
        {
            await TouchAsync(cdp, "touchMove", x, y + i * 10);
        }
        await Expect(Page.Locator("#last-move")).ToHaveTextAsync("40");

        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchMove",
            ["touchPoints"] = new object[]
            {
                new Dictionary<string, object> { ["x"] = x, ["y"] = y + 70, ["id"] = 1 },
                new Dictionary<string, object> { ["x"] = x + 50, ["y"] = y + 70, ["id"] = 2 }
            }
        });

        await Expect(Page.Locator("#cancels")).ToHaveTextAsync("1");
        await TouchEndAsync(cdp);
        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("0");
        await cdp.DetachAsync();
    }

    [TestMethod]
    public async Task TouchPull_OnAScrolledList_ScrollsTheList()
    {
        RequireChromium();
        await OpenAsync(Route);

        var anchor = Page.Locator("#basic-anchor");
        await anchor.EvaluateAsync("el => el.scrollTop = 200");
        await NextFramesAsync();
        await Expect(Page.Locator("#ptr-basic")).ToHaveCSSAsync("touch-action", "auto");

        var (x, y) = await PullStartPointAsync("#ptr-basic");
        await TouchDragAsync(x, y, 120);

        await Page.WaitForFunctionAsync("() => document.getElementById('basic-anchor').scrollTop < 150");
        await Expect(Page.Locator("#starts")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#refreshes")).ToHaveTextAsync("0");
    }

    private static void RequireChromium()
    {
        if (IsChromium is false) Assert.Inconclusive("Real touch input is dispatched through the Chromium DevTools protocol, which this engine does not have.");
    }

    private async Task<(float X, float Y)> PullStartPointAsync(string selector)
    {
        var root = Page.Locator(selector);
        await root.EvaluateAsync("el => el.scrollIntoView({ block: 'start' })");
        await NextFramesAsync();
        var box = (await root.BoundingBoxAsync())!;

        return (MathF.Round(box.X + 150), MathF.Round(box.Y + 20));
    }

    private async Task TouchDragAsync(float x, float y, float dy, int steps = 15)
    {
        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await TouchAsync(cdp, "touchStart", x, y);
        for (var i = 1; i <= steps; i++)
        {
            await TouchAsync(cdp, "touchMove", x, y + dy * i / steps);
        }
        await TouchEndAsync(cdp);
        await cdp.DetachAsync();
    }

    private static Task TouchAsync(ICDPSession cdp, string type, float x, float y)
    {
        return cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y, ["id"] = 1 } }
        });
    }

    private static Task TouchEndAsync(ICDPSession cdp)
    {
        return cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>() });
    }
}
