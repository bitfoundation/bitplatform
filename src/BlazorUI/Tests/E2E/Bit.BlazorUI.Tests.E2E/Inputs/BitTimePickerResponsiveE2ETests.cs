namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// Drives the responsive mode of BitTimePicker (/e2e/time-picker) on a phone: a touch device at a phone's viewport,
/// where a Responsive picker opens its callout as a sheet sliding in from the top of the screen that a swipe up
/// dismisses, while a picker that is not responsive still opens an ordinary callout against its field.
/// </summary>
[TestClass]
public class BitTimePickerResponsiveE2ETests : E2ETestBase
{
    private const string Url = "/e2e/time-picker";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        IsMobile = IsChromium ? true : null
    };

    private ILocator Field(string scope) => Page.Locator($"#{scope} .bit-tpc-inp").First;

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
    public async Task Responsive_OnAPhone_OpensAsASheetAcrossTheTopOfTheScreen()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("responsive");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-tpc-res"));
        await Expect(callout).ToHaveCSSAsync("opacity", "1");

        // The sheet slides in from above; it is in place once its transform has run out.
        await Expect(callout).ToHaveCSSAsync("transform", new System.Text.RegularExpressions.Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
        var box = (await callout.BoundingBoxAsync())!;
        Assert.AreEqual(0, box.Y, 1, "The sheet is pinned to the top of the screen.");
        Assert.AreEqual(0, box.X, 1);
        Assert.AreEqual(390, box.Width, 1, "The sheet spans the width of the screen.");

        // Its parts grow to finger size.
        var spin = callout.Locator("button[aria-label='Increase hour']");
        Assert.IsTrue((await spin.BoundingBoxAsync())!.Height >= 40, "The spin buttons of the sheet are finger sized.");

        await spin.TapAsync();
        await Expect(Page.Locator("#responsive-value")).ToHaveTextAsync("01:00:00");

        await callout.Locator(".bit-tpc-cbn").TapAsync();
        await Expect(Field("responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#responsive-close-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Responsive_SwipeTheSheetUp_DismissesIt()
    {
        if (IsChromium is false) Assert.Inconclusive("Real touch input is dispatched through the Chrome DevTools Protocol, which only Chromium speaks.");

        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("responsive");
        await Expect(callout).ToHaveCSSAsync("transform", new System.Text.RegularExpressions.Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
        var box = (await callout.BoundingBoxAsync())!;

        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        var x = box.X + box.Width / 2;
        var startY = box.Y + box.Height - 10;

        // Started on the frame of the sheet rather than on a control, so nothing takes the gesture for itself.
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchStart",
            ["touchPoints"] = new[] { new Dictionary<string, object> { ["x"] = x, ["y"] = startY } }
        });

        for (var step = 1; step <= 10; step++)
        {
            await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
            {
                ["type"] = "touchMove",
                ["touchPoints"] = new[] { new Dictionary<string, object> { ["x"] = x, ["y"] = startY - step * 25 } }
            });
        }

        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchEnd",
            ["touchPoints"] = Array.Empty<object>()
        });

        await Expect(Field("responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#responsive-close-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task NotResponsive_OnAPhone_StillOpensACalloutAgainstItsField()
    {
        await OpenAsync(Url);

        var callout = await OpenCalloutAsync("basic");
        await Expect(callout).Not.ToHaveClassAsync(HasClass("bit-tpc-res"));
        // The callout slides in; its box is read once that entry has run out.
        await callout.EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished))");

        var field = (await Page.Locator("#basic .bit-tpc-icn").BoundingBoxAsync())!;
        var box = (await callout.BoundingBoxAsync())!;
        Assert.IsTrue(box.Y >= field.Y + field.Height - 2 || box.Y + box.Height <= field.Y + 2, "The callout opens against the field.");
        Assert.IsTrue(box.Width < 390, "A callout that is not responsive is not a full-width sheet.");

        await callout.Locator("button[aria-label='Increase hour']").TapAsync();
        await Expect(Page.Locator("#basic-value")).ToHaveTextAsync("01:00:00");
    }
}
