namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// BitPanel as a bottom sheet with snap points on a phone (/e2e/panel-sheet at a phone's viewport with touch): a finger
/// dragging the sheet by its header grows it to the snap point it is let go nearest to, and shrinks it back down, and a
/// long drag below the smallest dismisses it. The touches are dispatched through the Chromium DevTools protocol, which
/// drives the browser's real touch pipeline; the other engines have no such input and skip.
/// </summary>
[TestClass]
public class BitPanelSheetTouchE2ETests : E2ETestBase
{
    private const string Route = "/e2e/panel-sheet";

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

    private ILocator Container => Page.Locator("#sheet .bit-pnl-cnt");

    private static Dictionary<string, object> Touch(string type, double x, double y) => new()
    {
        ["type"] = type,
        ["touchPoints"] = new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y, ["id"] = 1 } }
    };

    private async Task OpenSheetAsync()
    {
        await OpenAsync(Route);
        await Page.Locator("#open-sheet").ClickAsync();
        await Page.WaitForFunctionAsync("() => { const r = document.querySelector('#sheet .bit-pnl-cnt').getBoundingClientRect(); return Math.abs(r.bottom - window.innerHeight) < 1; }");
        await SettledHeightAsync();
    }

    private async Task<double> SettledHeightAsync()
    {
        double previous = -1;
        for (var i = 0; i < 40; i++)
        {
            var height = await Container.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
            if (Math.Abs(height - previous) < 0.5) return height;
            previous = height;
            await Page.WaitForTimeoutAsync(100);
        }
        return previous;
    }

    // A one-finger drag on the header of the sheet, a frame between moves as a finger would.
    private async Task TouchDragHeaderAsync(double dy)
    {
        var box = (await Page.Locator("#sheet .bit-pnl-hdr").BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await cdp.SendAsync("Input.dispatchTouchEvent", Touch("touchStart", x, y));
        for (var i = 1; i <= 12; i++)
        {
            await cdp.SendAsync("Input.dispatchTouchEvent", Touch("touchMove", x, y + dy * i / 12));
        }
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>() });
        await cdp.DetachAsync();
    }

    [TestMethod]
    public async Task AFingerDraggingTheSheet_GrowsAndShrinksItBetweenItsSnapPoints()
    {
        RequireChromium();
        await OpenSheetAsync();

        var vh = await Page.EvaluateAsync<double>("() => window.innerHeight");
        Assert.AreEqual(vh * 0.3, await SettledHeightAsync(), 3);

        await TouchDragHeaderAsync(-vh * 0.55);
        Assert.AreEqual(vh * 0.9, await SettledHeightAsync(), 3, "dragged up past six tenths, it settles at nine");
        await Expect(Page.Locator("#sheet-snap")).ToHaveTextAsync("2");

        await TouchDragHeaderAsync(vh * 0.35);
        Assert.AreEqual(vh * 0.6, await SettledHeightAsync(), 3, "dragged down by a third, it settles at six tenths");
        await Expect(Page.Locator("#sheet-snap")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task AFingerDraggingTheSheetFarBelowItsSmallest_DismissesIt()
    {
        RequireChromium();
        await OpenSheetAsync();

        var vh = await Page.EvaluateAsync<double>("() => window.innerHeight");
        await TouchDragHeaderAsync(vh * 0.3 * 0.7);

        await Expect(Page.Locator("#sheet-open")).ToHaveTextAsync("False");
    }
}
