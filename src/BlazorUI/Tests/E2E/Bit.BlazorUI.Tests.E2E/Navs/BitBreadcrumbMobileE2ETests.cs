using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of the overflow menu of a Responsive BitBreadcrumb (/e2e/breadcrumb/responsive) on a phone: a touch
/// screen at a phone's viewport, where the menu is a sheet that slides up from the bottom edge and runs the full width of
/// the screen, keeps its last step above the safe area at the bottom of the screen, runs a step tapped in it and closes,
/// closes on a tap outside it and is then out of the accessibility tree, and scrolls the steps of a menu taller than the
/// screen under the finger. The callout it is on a wider screen is covered by BitBreadcrumbE2ETests.
/// </summary>
[TestClass]
public class BitBreadcrumbMobileE2ETests : E2ETestBase
{
    private const string Url = "/e2e/breadcrumb/responsive";

    private static readonly Regex Identity = new(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$");

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        // Firefox has no mobile emulation; the touch screen and the viewport are what the sheet reads anyway.
        IsMobile = IsFirefox is false
    };

    private ILocator OverflowButton(string id) => Page.Locator($"#{id} .bit-brc-obt");

    // Taps the overflow button open and returns the sheet once it has slid all the way in.
    private async Task<ILocator> OpenSheetAsync(string id, string? before = null)
    {
        await OpenAsync(Url);

        if (before is not null)
        {
            await Page.EvaluateAsync(before);
        }

        var button = OverflowButton(id);
        await button.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        await NextFramesAsync();

        await button.TapAsync();
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");

        var sheet = Page.Locator($"[id='{await button.GetAttributeAsync("aria-controls")}']");
        await Expect(sheet).ToHaveClassAsync(HasClass("bit-brc-res"));
        await Expect(sheet).ToHaveCSSAsync("opacity", "1");
        await Expect(sheet).ToHaveCSSAsync("transform", Identity);

        return sheet;
    }

    // The edges of an element and of the layout viewport the sheet is fixed to, which a mobile browser sizes itself.
    private static Task<double[]> EdgesAsync(ILocator element)
    {
        return element.EvaluateAsync<double[]>(
            "el => { const r = el.getBoundingClientRect(); return [r.left, r.right, r.top, r.bottom, window.innerWidth, window.innerHeight]; }");
    }

    [TestMethod]
    public async Task Responsive_ATapOnTheOverflowButtonOpensTheMenuAsASheetAlongTheBottomEdge()
    {
        var sheet = await OpenSheetAsync("bc-responsive");

        var edges = await EdgesAsync(sheet);
        Assert.AreEqual(0, edges[0], 1, "From the left edge of the screen.");
        Assert.AreEqual(edges[4], edges[1], 1, "To its right edge.");
        Assert.AreEqual(edges[5], edges[3], 1, "Against its bottom edge.");
        Assert.IsGreaterThan(edges[5] / 2, edges[2], $"As tall as its three steps rather than the screen ({edges[3] - edges[2]}px).");

        await Expect(sheet.Locator(".bit-brc-ofi")).ToHaveTextAsync(["Step 2", "Step 3", "Step 4"]);
        await Expect(sheet.Locator(".bit-brc-ofi").Last).ToBeInViewportAsync(new() { Ratio = 1 });
        // The keyframe animation of the callout fills its end state, which would hold the sheet where it is.
        await Expect(sheet).ToHaveCSSAsync("animation-name", "none");
    }

    [TestMethod]
    public async Task Responsive_TheLastStepStaysAboveTheSafeAreaAtTheBottomOfTheScreen()
    {
        // The safe area a page gives an app shell, which the sheet makes room for the way it does for a phone's own.
        var sheet = await OpenSheetAsync("bc-responsive", "() => document.documentElement.style.setProperty('--bit-AppShell-safe-area-bottom', '30px')");

        await Expect(sheet).ToHaveCSSAsync("border-bottom-width", "30px");
        var edges = await EdgesAsync(sheet);
        Assert.AreEqual(edges[5], edges[3], 1, "The sheet itself still runs down to the bottom edge.");

        var last = await EdgesAsync(sheet.Locator(".bit-brc-ofi").Last);
        Assert.IsLessThanOrEqualTo(last[5] - 30 + 1, last[3], $"The last step ends above the safe area ({last[3]}px of {last[5]}px).");
    }

    [TestMethod]
    public async Task Responsive_ATapOnAStepRunsItAndClosesTheSheet()
    {
        var sheet = await OpenSheetAsync("bc-responsive");

        await sheet.Locator(".bit-brc-ofi", new() { HasText = "Step 3" }).TapAsync();

        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("Step 3");
        await Expect(OverflowButton("bc-responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(sheet).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Responsive_ATapOutsideTheSheetClosesIt_AndTheClosedSheetIsOutOfTheAccessibilityTree()
    {
        var sheet = await OpenSheetAsync("bc-responsive");
        var step = sheet.Locator(".bit-brc-ofi").First;

        // The overlay, above the sheet.
        await Page.Touchscreen.TapAsync(10, 100);

        await Expect(OverflowButton("bc-responsive")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#clicked")).ToHaveTextAsync("none");
        // A closed sheet is laid out below the screen for the slide that brings it in, and it is out of reach of a
        // screen reader and of the keyboard there, not only out of sight.
        await Expect(sheet).ToHaveCSSAsync("visibility", "hidden");
        await Expect(step).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Responsive_TheStepsOfALongMenuScrollInsideTheSheetUnderTheFinger()
    {
        if (IsChromium is false) Assert.Inconclusive("Real touch input is dispatched through the Chromium DevTools protocol, which this engine does not have.");

        var sheet = await OpenSheetAsync("bc-responsive-long");
        var list = sheet.Locator(".bit-brc-scn");

        var edges = await EdgesAsync(sheet);
        Assert.IsGreaterThanOrEqualTo(0, edges[2], "The sheet of a menu taller than the screen stays on the screen.");
        Assert.AreEqual(edges[5], edges[3], 1, "Against the bottom edge.");
        Assert.IsTrue(await list.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"), "The steps that do not fit scroll.");

        var box = (await list.BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        var handle = await list.ElementHandleAsync();

        // A finger dragged up scrolls the steps rather than the sheet.
        await TouchDragAsync(x, y, 0, -300);
        await Page.WaitForFunctionAsync("el => el.scrollTop > 100", handle);
        await Expect(OverflowButton("bc-responsive-long")).ToHaveAttributeAsync("aria-expanded", "true");

        var scrolled = await list.EvaluateAsync<double>("el => el.scrollTop");

        // And dragged back down, it scrolls them back, the sheet staying where it is.
        await TouchDragAsync(x, y, 0, 150);
        await Page.WaitForFunctionAsync("([el, before]) => el.scrollTop < before", new object[] { handle, scrolled });
        await NextFramesAsync();
        await Expect(OverflowButton("bc-responsive-long")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(sheet).ToHaveCSSAsync("transform", Identity);
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
