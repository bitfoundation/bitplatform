namespace Bit.BlazorUI.Tests.E2E.Notifications;

/// <summary>
/// BitSnackBar on a phone (/e2e/snack-bar at a 375x667 touch viewport): every item takes the full width of the stack,
/// a touch swipe throws an item away, and a tap - which raises a pointer that enters and leaves at once - does not
/// leave the countdown held.
/// </summary>
[TestClass]
public class BitSnackBarMobileE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 375, Height = 667 },
        HasTouch = true,
        IsMobile = IsFirefox is false,
    };

    [TestMethod]
    public async Task Mobile_EveryItem_TakesTheFullWidthOfTheStack()
    {
        await OpenAsync("/e2e/snack-bar");

        await Page.TapAsync("#btn-title-only");
        var item = Page.Locator("#snb .bit-snb-itm", new() { HasText = "Only title" });
        await Expect(item).ToBeVisibleAsync();
        await item.EvaluateAsync("el => Promise.all(el.getAnimations().map(a => a.finished.catch(() => null)))");

        var width = await Page.EvaluateAsync<double>("() => document.documentElement.clientWidth");
        var box = await item.BoxAsync();
        Assert.AreEqual(width - 16, box.Width, 1, "A short item still spans the stack on a phone.");
        Assert.AreEqual(8, box.Left, 1);
    }

    [TestMethod]
    public async Task Mobile_ATouchSwipe_ThrowsTheItemAway()
    {
        if (IsChromium is false) Assert.Inconclusive("The touch drag is dispatched through the Chromium DevTools protocol, which this engine does not offer.");

        await OpenAsync("/e2e/snack-bar?sw=true&th=60");

        await Page.TapAsync("#btn-info");
        var item = Page.Locator("#snb .bit-snb-itm", new() { HasText = "Info 1" });
        await Expect(item).ToBeVisibleAsync();
        await item.EvaluateAsync("el => Promise.all(el.getAnimations().map(a => a.finished.catch(() => null)))");
        var box = await item.BoxAsync();

        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        async Task TouchAsync(string type, double x)
        {
            await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
            {
                ["type"] = type,
                ["touchPoints"] = type == "touchEnd" ? Array.Empty<object>() : new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = box.CenterY } },
            });
        }

        await TouchAsync("touchStart", box.CenterX);
        for (var dx = 10; dx <= 140; dx += 10)
        {
            await TouchAsync("touchMove", box.CenterX + dx);
        }

        await TouchAsync("touchEnd", box.CenterX + 140);

        await Expect(item).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:Swipe;");
    }

    [TestMethod]
    public async Task Mobile_ATapOnAnItem_DoesNotHoldItsCountdown()
    {
        await OpenAsync("/e2e/snack-bar?ad=true&t=2000");

        await Page.TapAsync("#btn-info");
        var item = Page.Locator("#snb .bit-snb-itm", new() { HasText = "Info 1" });
        await Expect(item).ToBeVisibleAsync();

        // Tapped where it is drawn, at once: the tap is the subject, not the actionability of a part of the item.
        var body = await item.Locator(".bit-snb-bdy").BoxAsync();
        await Page.Touchscreen.TapAsync((float)body.CenterX, (float)body.CenterY);

        await Expect(item).ToHaveCountAsync(0, new() { Timeout = 10000 });
        await Expect(Page.Locator("#dismissed-log")).ToHaveTextAsync("Info 1:Timeout;");
    }
}
