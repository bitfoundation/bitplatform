using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Buttons;

/// <summary>
/// End-to-end tests of a Responsive BitMenuButton (/e2e/menu-button/responsive) on a phone: a touch screen at a phone's
/// viewport, where the menu is a sheet that slides up from the bottom edge and runs the full width of the screen, an
/// item tapped in it runs and closes it, a tap outside it closes it and hands the focus back to the button, the items
/// of a menu taller than the screen scroll inside it under the finger, and a submenu opens over it, inside the screen.
/// The callout it is on a wider screen is covered by BitMenuButtonE2ETests.
/// </summary>
[TestClass]
public class BitMenuButtonMobileE2ETests : E2ETestBase
{
    private const string Url = "/e2e/menu-button/responsive";

    private static readonly Regex Identity = new(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$");

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        // Firefox has no mobile emulation; the touch screen and the viewport are what the sheet reads anyway.
        IsMobile = IsFirefox is false
    };

    private ILocator Opener(string id, string half = "opb") => Page.Locator($"#{id}-{half}");

    // Taps the button open and returns the sheet once it has slid all the way in.
    private async Task<ILocator> OpenSheetAsync(string id, string half = "opb")
    {
        await OpenAsync(Url);

        var opener = Opener(id, half);
        await opener.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        await NextFramesAsync();

        await opener.TapAsync();
        await Expect(opener).ToHaveAttributeAsync("aria-expanded", "true");

        var sheet = Page.Locator($"[id='{await opener.GetAttributeAsync("aria-controls")}']").Locator("xpath=..");
        await Expect(sheet).ToHaveClassAsync(HasClass("bit-mnb-res"));
        await Expect(sheet).ToHaveCSSAsync("opacity", "1");
        await Expect(sheet).ToHaveCSSAsync("transform", Identity);

        return sheet;
    }

    // The edges of the sheet and of the layout viewport it is fixed to, which a mobile browser sizes itself.
    private static Task<double[]> EdgesAsync(ILocator sheet)
    {
        return sheet.EvaluateAsync<double[]>(
            "el => { const r = el.getBoundingClientRect(); return [r.left, r.right, r.top, r.bottom, window.innerWidth, window.innerHeight]; }");
    }

    [TestMethod]
    [DataRow("mnb-responsive", "opb")]
    [DataRow("mnb-responsive-split", "chb")]
    public async Task Responsive_ATapOpensTheMenuAsASheetAlongTheBottomEdge(string id, string half)
    {
        var sheet = await OpenSheetAsync(id, half);

        var edges = await EdgesAsync(sheet);
        Assert.AreEqual(0, edges[0], 1, "From the left edge of the screen.");
        Assert.AreEqual(edges[4], edges[1], 1, "To its right edge, whatever the width of the button.");
        Assert.AreEqual(edges[5], edges[3], 1, "Against its bottom edge.");
        Assert.IsGreaterThan(edges[5] / 2, edges[2], $"As tall as its three items rather than the screen ({edges[3] - edges[2]}px).");

        await Expect(sheet.Locator(".bit-mnb-itm")).ToHaveCountAsync(3);
        await Expect(sheet.Locator(".bit-mnb-itm").Last).ToBeInViewportAsync(new() { Ratio = 1 });
    }

    [TestMethod]
    public async Task Responsive_ATapOnAnItemRunsItAndClosesTheSheet()
    {
        var sheet = await OpenSheetAsync("mnb-responsive");

        await sheet.Locator(".bit-mnb-itm").Nth(1).TapAsync();

        await Expect(Page.Locator("#responsive-clicked")).ToHaveTextAsync("edit");
        await Expect(Page.Locator("#responsive-open")).ToHaveTextAsync("False");
        await Expect(sheet).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Responsive_ATapOutsideTheSheetClosesIt_AndTheClosedSheetIsOutOfTheAccessibilityTree()
    {
        var sheet = await OpenSheetAsync("mnb-responsive");
        var item = sheet.Locator(".bit-mnb-itm").First;

        // The overlay, above the sheet.
        await Page.Touchscreen.TapAsync(10, 100);

        await Expect(Opener("mnb-responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        // The tap on the button that opened it, and no item.
        await Expect(Page.Locator("#responsive-clicked")).ToHaveTextAsync("header");
        // A closed sheet is laid out below the screen for the slide that brings it in, and it is out of reach of a
        // screen reader and of the keyboard there, not only out of sight.
        await Expect(sheet).ToHaveCSSAsync("visibility", "hidden");
        await Expect(item).ToBeHiddenAsync();
        await Expect(Opener("mnb-responsive")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Responsive_TheItemsOfALongMenuScrollInsideTheSheetUnderTheFinger()
    {
        if (IsChromium is false) Assert.Inconclusive("Real touch input is dispatched through the Chromium DevTools protocol, which this engine does not have.");

        var sheet = await OpenSheetAsync("mnb-responsive-long");

        var edges = await EdgesAsync(sheet);
        Assert.IsGreaterThanOrEqualTo(0, edges[2], "The sheet of a menu taller than the screen stays on the screen.");
        Assert.AreEqual(edges[5], edges[3], 1, "Against the bottom edge.");
        Assert.IsTrue(await sheet.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"), "The items that do not fit scroll.");

        var box = (await sheet.BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        var handle = await sheet.ElementHandleAsync();

        // A finger dragged up scrolls the items rather than the sheet.
        await TouchDragAsync(x, y, 0, -300);
        await Page.WaitForFunctionAsync("el => el.scrollTop > 100", handle);
        await Expect(Opener("mnb-responsive-long")).ToHaveAttributeAsync("aria-expanded", "true");

        var scrolled = await sheet.EvaluateAsync<double>("el => el.scrollTop");

        // And dragged back down, it scrolls them back, the sheet staying where it is.
        await TouchDragAsync(x, y, 0, 150);
        await Page.WaitForFunctionAsync("([el, before]) => el.scrollTop < before", new object[] { handle, scrolled });
        await NextFramesAsync();
        await Expect(Opener("mnb-responsive-long")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(sheet).ToHaveCSSAsync("transform", Identity);
    }

    [TestMethod]
    public async Task Responsive_ASubmenuOpensOverTheSheetInsideTheScreen()
    {
        var sheet = await OpenSheetAsync("mnb-responsive-submenu");

        var row = sheet.Locator(".bit-mnb-itm", new() { HasText = "Share" });
        await row.TapAsync();

        await Expect(row).ToHaveAttributeAsync("aria-controls", new Regex(".+"));
        var submenu = Page.Locator($"[id='{await row.GetAttributeAsync("aria-controls")}']");
        var email = submenu.Locator(".bit-mnb-itm", new() { HasText = "Email" });
        await Expect(email).ToBeVisibleAsync();
        await Expect(submenu.Locator("xpath=..")).Not.ToHaveClassAsync(HasClass("bit-mnb-res"));

        var edges = await EdgesAsync(submenu.Locator("xpath=.."));
        Assert.IsGreaterThanOrEqualTo(0, edges[0], "Inside the left edge of the screen.");
        Assert.IsLessThanOrEqualTo(edges[4], edges[1], "Inside its right edge.");
        Assert.IsGreaterThanOrEqualTo(0, edges[2], "Inside its top edge.");
        Assert.IsLessThanOrEqualTo(edges[5], edges[3], "Inside its bottom edge.");
        // Painted over the sheet it was opened from, so the tap meant for it lands on it.
        Assert.IsTrue(await email.EvaluateAsync<bool>(
            "el => { const r = el.getBoundingClientRect(); return el.contains(document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2)); }"),
            "The submenu is under the sheet.");

        await email.TapAsync();

        await Expect(Page.Locator("#submenu-clicked")).ToHaveTextAsync("email");
        await Expect(Opener("mnb-responsive-submenu")).ToHaveAttributeAsync("aria-expanded", "false");
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
