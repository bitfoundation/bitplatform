namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives BitCircularTimePicker (/e2e/circular-time-picker) on a phone: a touch device at a phone's viewport, where
/// the dial is dragged with a finger (touch input turned into pointer events by the browser), a number is tapped, and a
/// Responsive picker opens its callout as a sheet across the top of the screen that a swipe up dismisses.
/// </summary>
[TestClass]
public class BitCircularTimePickerTouchE2ETests : E2ETestBase
{
    private const string Url = "/e2e/circular-time-picker";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        IsMobile = IsChromium ? true : null
    };

    private ILocator Field(string scope) => Page.Locator($"#{scope} .bit-ctp-inp").First;

    private ILocator StandaloneCallout(string scope) => Page.Locator($"#{scope} .bit-ctp-cal").First;

    private static Task SettleAsync(ILocator element)
    {
        return element.EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished))");
    }

    private static async Task<(double X, double Y)> PointAsync(ILocator callout, double degrees, double distance)
    {
        var box = (await callout.Locator(".bit-ctp-clf").BoundingBoxAsync())!;
        var radius = Math.Min(box.Width, box.Height) / 2;
        var radians = degrees * Math.PI / 180;

        return (box.X + box.Width / 2 + Math.Sin(radians) * distance * radius,
                box.Y + box.Height / 2 - Math.Cos(radians) * distance * radius);
    }

    private static Task TouchAsync(ICDPSession cdp, string type, params (double X, double Y)[] points)
    {
        return cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = points.Select(p => new Dictionary<string, object> { ["x"] = p.X, ["y"] = p.Y }).ToArray()
        });
    }

    private async Task<ILocator> OpenCalloutAsync(string scope)
    {
        var field = Field(scope);
        await field.ScrollIntoViewIfNeededAsync();
        await field.TapAsync();
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");

        var callout = Page.Locator($"[id='{await field.GetAttributeAsync("aria-controls")}']");
        await Expect(callout).ToBeVisibleAsync();

        return callout;
    }

    [TestMethod]
    public async Task Drag_WithAFinger_MovesTheHandAndPicksWhereItIsLifted()
    {
        if (IsChromium is false) Assert.Inconclusive("Real touch input is dispatched through the Chrome DevTools Protocol, which only Chromium speaks.");

        await OpenAsync(Url);

        var callout = StandaloneCallout("drag");
        await callout.ScrollIntoViewIfNeededAsync();
        var cdp = await Page.Context.NewCDPSessionAsync(Page);

        await TouchAsync(cdp, "touchStart", await PointAsync(callout, 30, 0.85));
        await Expect(Page.Locator("#drag-value")).ToHaveTextAsync("13:00:00");

        for (var degrees = 45; degrees <= 120; degrees += 15)
        {
            await TouchAsync(cdp, "touchMove", await PointAsync(callout, degrees, 0.85));
        }

        await Expect(Page.Locator("#drag-value")).ToHaveTextAsync("16:00:00");
        await TouchAsync(cdp, "touchEnd");

        await Expect(callout.Locator(".bit-ctp-hmc button").Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");

        // The minutes, dragged on the inner part of the dial: any distance picks a minute.
        await TouchAsync(cdp, "touchStart", await PointAsync(callout, 0, 0.6));
        await TouchAsync(cdp, "touchMove", await PointAsync(callout, 90, 0.6));
        await TouchAsync(cdp, "touchMove", await PointAsync(callout, 180, 0.6));
        await TouchAsync(cdp, "touchEnd");
        await Expect(Page.Locator("#drag-value")).ToHaveTextAsync("16:30:00");

        // The finger dragged the hand, not the page.
        Assert.AreEqual("none", await callout.Locator(".bit-ctp-clk").EvaluateAsync<string>("el => getComputedStyle(el).touchAction"));
    }

    [TestMethod]
    public async Task Tap_OnANumber_PicksIt()
    {
        await OpenAsync(Url);

        var callout = StandaloneCallout("drag-12");
        await callout.ScrollIntoViewIfNeededAsync();
        await callout.GetByRole(AriaRole.Option, new() { Name = "11", Exact = true }).TapAsync();
        await Expect(Page.Locator("#drag-12-value")).ToHaveTextAsync("23:00:00");
    }

    [TestMethod]
    public async Task Responsive_OnAPhone_OpensAsASheet_ThatASwipeUpDismisses()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-ctp-res"));
        await Expect(callout).ToHaveCSSAsync("transform", new System.Text.RegularExpressions.Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
        var box = (await callout.BoundingBoxAsync())!;
        Assert.AreEqual(0, box.Y, 1, "The sheet is pinned to the top of the screen.");
        Assert.AreEqual(390, box.Width, 1, "The sheet spans the width of the screen.");

        await callout.GetByRole(AriaRole.Option, new() { Name = "14", Exact = true }).TapAsync();
        await Expect(Page.Locator("#responsive-value")).ToHaveTextAsync("14:00:00");

        if (IsChromium is false) Assert.Inconclusive("Real touch input is dispatched through the Chrome DevTools Protocol, which only Chromium speaks.");

        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        var toolbar = (await callout.Locator(".bit-ctp-tlb").BoundingBoxAsync())!;
        var x = toolbar.X + 20;
        var y = toolbar.Y + toolbar.Height - 5;
        await TouchAsync(cdp, "touchStart", (x, y));
        for (var step = 1; step <= 10; step++)
        {
            await TouchAsync(cdp, "touchMove", (x, y - step * 30));
        }

        await TouchAsync(cdp, "touchEnd");

        await Expect(Field("responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#responsive-close-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task NotResponsive_OnAPhone_StillOpensACalloutAgainstItsField()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("basic");
        await SettleAsync(callout);
        await Expect(callout).Not.ToHaveClassAsync(HasClass("bit-ctp-res"));

        var field = (await Page.Locator("#basic .bit-ctp-icn").BoundingBoxAsync())!;
        var box = (await callout.BoundingBoxAsync())!;
        Assert.IsTrue(box.Y >= field.Y + field.Height - 2 || box.Y + box.Height <= field.Y + 2, "The callout opens against the field.");
    }
}
