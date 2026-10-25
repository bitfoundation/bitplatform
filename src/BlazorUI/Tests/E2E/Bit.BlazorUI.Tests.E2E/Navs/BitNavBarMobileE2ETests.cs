using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitNavBar and BitNav on a phone (/e2e/nav-bar and /e2e/nav, a 390x844 touch viewport): a tap
/// selects a bar's item and toggles a nav's branch, a touch swipe scrolls a Scrollable bar, the pressed state of an item
/// under a finger, and a bar pinned to the bottom of the screen keeps its items at the bottom edge.
/// </summary>
[TestClass]
public class BitNavBarMobileE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        // The touch input and the viewport are what these tests need; IsMobile would also zoom the layout viewport out to
        // the widest section of the shared page, which is not what a phone shows an app built for it.
        HasTouch = true,
    };

    private static Regex Exact(string text) => new($"^{Regex.Escape(text)}$");

    private ILocator BarItem(string barId, string text)
    {
        return Page.Locator($"#{barId} .bit-nbr-itm").Filter(new() { Has = Page.Locator(".bit-nbr-txc", new() { HasTextRegex = Exact(text) }) });
    }

    [TestMethod]
    public async Task Tap_SelectsAnItemOfTheBar()
    {
        await OpenAsync("/e2e/nav-bar");

        await BarItem("nbr-manual-bound", "Alerts").TapAsync();

        await Expect(BarItem("nbr-manual-bound", "Alerts")).ToHaveClassAsync(HasClass("bit-nbr-sel"));
        await Expect(Page.Locator("#bound-selected")).ToHaveTextAsync("Alerts");
        await Expect(Page.Locator("#bound-select-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Tap_TogglesABranchOfTheNav()
    {
        await OpenAsync("/e2e/nav");

        var docs = Page.Locator("#nav-basic .bit-nav-ict").Filter(new() { Has = Page.Locator(".bit-nav-itx", new() { HasTextRegex = Exact("Docs") }) });
        await docs.TapAsync();

        await Expect(docs).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#nav-basic .bit-nav-itx", new() { HasTextRegex = Exact("Intro") })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Swipe_ScrollsAScrollableBar()
    {
        if (IsChromium is false) Assert.Inconclusive("The touch swipe is synthesized through the Chromium DevTools protocol.");

        await OpenAsync("/e2e/nav-bar");

        var list = Page.Locator("#nbr-scroll > ul");
        await list.ScrollIntoViewIfNeededAsync();
        var box = (await list.BoundingBoxAsync())!;

        // A finger dragged from right to left across the bar, the way a phone scrolls it.
        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        var y = box.Y + box.Height / 2;
        var x = box.X + box.Width - 20;

        await TouchAsync(cdp, "touchStart", x, y);
        for (var step = 1; step <= 10; step++)
        {
            await TouchAsync(cdp, "touchMove", x - step * 20, y);
        }
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>() });

        await Page.WaitForFunctionAsync("() => document.querySelector('#nbr-scroll > ul').scrollLeft > 50");
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => document.scrollingElement.scrollLeft"), "Only the bar scrolls, not the page.");
    }

    private static Task TouchAsync(ICDPSession cdp, string type, double x, double y)
    {
        return cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y } },
        });
    }

    [TestMethod]
    public async Task ABarFixedToTheBottom_KeepsItsItemsAtTheBottomEdge()
    {
        await OpenAsync("/e2e/nav-bar");

        // Pinned the way an app pins a bottom navigation bar.
        await Page.Locator("#nbr-safearea").EvaluateAsync("el => Object.assign(el.style, { position: 'fixed', left: '0', right: '0', bottom: '0', background: 'white' })");

        var bar = (await Page.Locator("#nbr-safearea").BoundingBoxAsync())!;
        Assert.AreEqual(844, bar.Y + bar.Height, 1, "The bar sits on the bottom edge of the phone's viewport.");
        Assert.AreEqual(390, bar.Width, 1);

        // Its four items share the width of the phone.
        var widths = await Page.Locator("#nbr-safearea .bit-nbr-lit").EvaluateAllAsync<double[]>("els => els.map(e => e.getBoundingClientRect().width)");
        Assert.AreEqual(4, widths.Length);
        Assert.IsTrue(widths.Sum() <= 390 + 1);

        // And a finger on an item lands on that item, on top of the page.
        var item = Page.Locator("#nbr-safearea .bit-nbr-itm").Nth(2);
        var box = (await item.BoundingBoxAsync())!;
        var hit = await Page.EvaluateAsync<bool>(
            "([x, y]) => document.elementFromPoint(x, y)?.closest('.bit-nbr-itm') === document.querySelectorAll('#nbr-safearea .bit-nbr-itm')[2]",
            new object[] { (double)(box.X + box.Width / 2), (double)(box.Y + box.Height / 2) });
        Assert.IsTrue(hit);
    }
}
