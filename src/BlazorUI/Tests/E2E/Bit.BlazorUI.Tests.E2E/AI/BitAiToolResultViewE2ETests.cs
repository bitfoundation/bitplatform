namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiToolResultView at /e2e/aitoolresultview in a real browser: a result that came without a call, shown as
/// text the way it was written (line breaks kept, a long token wrapping), as a JSON tree or as a compact error with its
/// detail behind a disclosure, titled by its tool or "Result", its glyph from the AI font, the part view picking it for
/// a result part, the theme tokens and every --bit-AiToolResultView-* variable, Styles and Classes, and RTL.
/// </summary>
[TestClass]
public class BitAiToolResultViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aitoolresultview";

    private ILocator View(string id) => Page.Locator($"#{id}");

    [TestMethod]
    public async Task Text_AResultWrittenAsText_IsShownTheWayItWasWritten_UnderItsToolsName()
    {
        await OpenAsync(Route);

        var view = View("trv-text");
        await Expect(view.Locator(".bit-aitrv-ttl")).ToHaveTextAsync("web_search");
        await view.Locator(".bit-aitrv-ico").AssertAiGlyphAsync("Repair");
        await Expect(view.Locator(".bit-aitrv-ico")).ToHaveAttributeAsync("aria-hidden", "true");

        var text = view.Locator(".bit-aitrv-txt");
        await Expect(text).ToHaveCSSAsync("white-space", "pre-wrap");
        await Expect(text).ToHaveCSSAsync("font-family", await Page.ResolveFontFamilyAsync("var(--bit-tpg-font-family-mono)"));
        Assert.AreEqual("2 results\n1. bitplatform.dev\n2. github.com/bitfoundation", (await text.InnerTextAsync()).Replace("\r", ""));

        // Three lines, as written.
        var lineHeight = AiE2EHelpers.ParsePx(await text.EvaluateAsync<string>("e => getComputedStyle(e).lineHeight"));
        var height = await text.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        Assert.AreEqual(3 * lineHeight, height, lineHeight / 2);
    }

    [TestMethod]
    public async Task Untitled_AResultWithoutAToolName_IsTitledResult()
    {
        await OpenAsync(Route);

        await Expect(View("trv-untitled").Locator(".bit-aitrv-ttl")).ToHaveTextAsync("Result");
        await Expect(View("trv-untitled").Locator(".bit-aitrv-txt")).ToHaveTextAsync("done");
    }

    [TestMethod]
    public async Task Json_AStructuredResult_IsShownAsAJsonTree()
    {
        await OpenAsync(Route);

        var tree = View("trv-json").Locator(".bit-aitrv-bdy .bit-aijsv");
        await Expect(tree).ToBeVisibleAsync();
        await Expect(tree.Locator(".bit-aijsv-key")).ToContainTextAsync(["hits", "top", "sources"]);
        await Expect(tree).ToContainTextAsync("bitplatform.dev");
        await Expect(tree.Locator(".bit-aijsv-num").First).ToHaveTextAsync("2");
        await Expect(View("trv-json").Locator(".bit-aitrv-txt")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Failed_AFailedResult_IsACompactErrorTitledByItsMessage_WithItsDetailBehindADisclosure()
    {
        await OpenAsync(Route);

        var error = View("trv-failed").Locator(".bit-aitrv-bdy .bit-aierv");
        await Expect(error.Locator("[role=alert]")).ToContainTextAsync("The search provider timed out.");
        await Expect(error).Not.ToContainTextAsync("Something went wrong");

        var toggle = error.GetByRole(AriaRole.Button, new() { Name = "Show details", Exact = true });
        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(error.Locator(".bit-aierv-dtl")).ToHaveTextAsync("timeout after 30s");
    }

    [TestMethod]
    public async Task Dispatch_ThePartViewDrawsAResultPartWithThisView()
    {
        await OpenAsync(Route);

        var view = Page.Locator("#trv-part > .bit-aitrv");
        await Expect(view).ToBeVisibleAsync();
        await Expect(view.Locator(".bit-aitrv-ttl")).ToHaveTextAsync("web_search");
        await Expect(view.Locator(".bit-aitrv-txt")).ToHaveTextAsync("dispatched");
    }

    [TestMethod]
    public async Task Narrow_ATokenLongerThanTheCard_WrapsInsideIt()
    {
        await OpenAsync(Route);

        var view = View("trv-narrow");
        var (left, _, right, _) = await view.RectAsync();
        var (boxLeft, _, boxRight, _) = await Page.Locator("#trv-narrow-box").RectAsync();
        Assert.IsTrue(left >= boxLeft - 0.5 && right <= boxRight + 0.5, "The card stays inside its box.");
        Assert.IsTrue(await view.EvaluateAsync<bool>("e => e.scrollWidth <= e.clientWidth + 1"), "Nothing runs off the side of the card.");
        Assert.IsTrue(await view.Locator(".bit-aitrv-txt").EvaluateAsync<bool>("e => e.getClientRects().length > 0 && e.getBoundingClientRect().height > parseFloat(getComputedStyle(e).lineHeight) * 1.5"),
                      "The long token wraps onto more lines.");
    }

    [TestMethod]
    public async Task Style_TheThemeTokens_PaintTheCardItsHeaderAndItsBody()
    {
        await OpenAsync(Route);

        var view = View("trv-text");
        await Expect(view).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(view).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
        await Expect(view).ToHaveCSSAsync("border-top-left-radius", await view.ResolveLengthInAsync("var(--bit-shp-radius-surface)"));
        await Expect(view.Locator(".bit-aitrv-hdr")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(view.Locator(".bit-aitrv-bdy")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri)"));
        await Expect(view.Locator(".bit-aitrv-bdy")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
    }

    [TestMethod]
    public async Task Style_ThePublicVariables_RestyleEveryPart_AndStylesAndClassesReachThem()
    {
        await OpenAsync(Route);

        var view = View("trv-vars");
        await Expect(view).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(view).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(view).ToHaveCSSAsync("border-top-left-radius", "7px");
        await Expect(view.Locator(".bit-aitrv-hdr")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(view.Locator(".bit-aitrv-bdy")).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");
        await Expect(view.Locator(".bit-aitrv-bdy")).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");

        await Expect(View("trv-instance").Locator(".bit-aitrv-hdr")).ToHaveCSSAsync("color", "rgb(20, 21, 22)");

        var styled = View("trv-styles");
        await Expect(styled.Locator(".bit-aitrv-ttl")).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(styled.Locator(".bit-aitrv-txt")).ToHaveCSSAsync("font-style", "italic");
        await Expect(styled).ToHaveClassAsync(HasClass("e2e-trv-root"));
        await Expect(styled.Locator(".bit-aitrv-bdy")).ToHaveClassAsync(HasClass("e2e-trv-body"));
    }

    [TestMethod]
    public async Task Rtl_TheGlyphLeadsTheTitleFromTheRight_InAnExplicitAndAnInheritedDirection()
    {
        await OpenAsync(Route);

        var (iconLeft, _, _, _) = await View("trv-text").Locator(".bit-aitrv-ico").RectAsync();
        var (titleLeft, _, _, _) = await View("trv-text").Locator(".bit-aitrv-ttl").RectAsync();
        Assert.IsTrue(iconLeft < titleLeft, "Left to right, the glyph leads the title from the left.");

        foreach (var id in new[] { "trv-rtl", "trv-rtl-inherited" })
        {
            var (_, _, iconRight, _) = await View(id).Locator(".bit-aitrv-ico").RectAsync();
            var (_, _, titleRight, _) = await View(id).Locator(".bit-aitrv-ttl").RectAsync();
            Assert.IsTrue(iconRight > titleRight, $"{id}: right to left, the glyph leads the title from the right.");
        }

        await Expect(View("trv-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(View("trv-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
    }
}
