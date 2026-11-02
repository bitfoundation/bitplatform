using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiSourceList at /e2e/aisourcelist in a real browser: one card per source under the lowest number it is cited
/// with (an unnumbered one last), the heading cased as an overline, nothing at all while there is nothing to list,
/// cards that are links and cards that are buttons (pointer and keyboard), a list growing as citations arrive, the grid
/// in columns and in one column on a narrow screen, Disabled, every --bit-AiSourceList-* variable, Styles and Classes,
/// and RTL.
/// </summary>
[TestClass]
public class BitAiSourceListE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aisourcelist";

    private ILocator List(string id) => Page.Locator($"#{id}");

    private ILocator Cards(string id) => Page.Locator($"#{id} > .bit-aisrl-lst > .bit-aisrc");

    [TestMethod]
    public async Task Dedupe_ASourceCitedTwice_IsListedOnceUnderItsLowestNumber()
    {
        await OpenAsync(Route);

        var cards = Cards("srl-dedupe");
        await Expect(cards).ToHaveCountAsync(3);
        await Expect(cards.Locator(".bit-aisrc-ttl")).ToHaveTextAsync(["Blazor render modes", "SignalR circuit lifetime", "The internal runbook"]);
        await Expect(cards.Nth(0).Locator(".bit-aisrc-idx")).ToHaveTextAsync("1");
        await Expect(cards.Nth(1).Locator(".bit-aisrc-idx")).ToHaveTextAsync("2");

        // A source cited without a number goes last, and shows none.
        await Expect(cards.Nth(2).Locator(".bit-aisrc-idx")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Heading_TheList_IsHeadedSourcesInTheOverlineStyle()
    {
        await OpenAsync(Route);

        var heading = List("srl-dedupe").Locator("> .bit-aisrl-hdr");
        await Expect(heading).ToHaveTextAsync("Sources");
        await Expect(heading).ToHaveCSSAsync("text-transform", await TextTransformAsync("var(--bit-tpg-overline-text-transform)"));
        await Expect(heading).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(heading).ToHaveCSSAsync("font-size", await List("srl-dedupe").ResolveLengthInAsync("var(--bit-tpg-fs-xs)"));

        // The heading comes first, the cards under it.
        var top = await heading.RectAsync();
        var first = await Cards("srl-dedupe").First.RectAsync();
        Assert.IsTrue(first.Top >= top.Bottom, "The cards are under the heading.");

        await Expect(List("srl-no-heading").Locator(".bit-aisrl-hdr")).ToHaveCountAsync(0);
        await Expect(Cards("srl-no-heading")).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task Empty_NothingToList_RendersNothingAtAll()
    {
        await OpenAsync(Route);

        await Expect(List("srl-empty")).ToHaveCountAsync(0);
        await Expect(List("srl-null")).ToHaveCountAsync(0);
        foreach (var host in new[] { "#srl-empty-host", "#srl-null-host" })
        {
            await Expect(Page.Locator($"{host} > *")).ToHaveCountAsync(0);
            await Expect(Page.Locator(host)).ToHaveTextAsync("");
        }
    }

    [TestMethod]
    public async Task Links_WithoutOnSourceClick_TheCardsLeadToTheirSourcesInANewTab()
    {
        await OpenAsync(Route);

        var cards = Cards("srl-dedupe");
        await Expect(cards.Nth(0).Locator("a.bit-crd-lnk")).ToHaveAttributeAsync("href", "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes");
        await Expect(cards.Nth(0).Locator("a.bit-crd-lnk")).ToHaveAttributeAsync("target", "_blank");
        await Expect(cards.Nth(1).Locator("a.bit-crd-lnk")).ToHaveAttributeAsync("href", "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr");
        await Expect(cards.Nth(2).Locator("a, button")).ToHaveCountAsync(0);
        await Expect(List("srl-dedupe").Locator("button")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Click_OnSourceClick_MakesTheCardsButtonsThatHandTheirSourceOver()
    {
        await OpenAsync(Route);

        var cards = Cards("srl-click");
        await Expect(List("srl-click").Locator("a")).ToHaveCountAsync(0);
        await Expect(cards.Locator("button.bit-crd-lnk")).ToHaveCountAsync(3);

        await cards.Nth(1).Locator(".bit-aisrc-ttl").ClickAsync();
        await Expect(Page.Locator("#srl-clicked")).ToHaveTextAsync("src-2");
        await Expect(Page.Locator("#srl-clicks")).ToHaveTextAsync("1");

        // The cards are reached in order by the keyboard and answer Enter.
        await cards.Nth(1).Locator("button").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(cards.Nth(2).Locator("button")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#srl-clicked")).ToHaveTextAsync("src-3");
        await Expect(Page.Locator("#srl-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Growing_AsCitationsArrive_TheListAppearsAndGrows()
    {
        await OpenAsync(Route);

        await Expect(List("srl-growing")).ToHaveCountAsync(0);

        await Page.Locator("#btn-growing-add").ClickAsync();
        await Expect(Cards("srl-growing")).ToHaveCountAsync(1);
        await Expect(List("srl-growing").Locator(".bit-aisrl-hdr")).ToHaveTextAsync("Sources");

        await Page.Locator("#btn-growing-add").ClickAsync();
        await Page.Locator("#btn-growing-add").ClickAsync();
        await Expect(Cards("srl-growing").Locator(".bit-aisrc-idx")).ToHaveTextAsync(["1", "2", "3"]);

        // A fourth citation of the first source adds no card.
        await Page.Locator("#btn-growing-add").ClickAsync();
        await Page.Locator("#btn-growing-add").ClickAsync();
        await Expect(Cards("srl-growing")).ToHaveCountAsync(3);
        await Expect(Cards("srl-growing").Locator(".bit-aisrc-idx")).ToHaveTextAsync(["1", "2", "3"]);

        await Page.Locator("#btn-growing-clear").ClickAsync();
        await Expect(List("srl-growing")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Grid_AWideList_LaysTheCardsOutInColumnsAndANarrowScreenInOne()
    {
        await OpenAsync(Route);

        var cards = Cards("srl-grid");
        await Expect(List("srl-grid").Locator("> .bit-aisrl-lst")).ToHaveCSSAsync("display", "grid");
        var first = await cards.Nth(0).RectAsync();
        var second = await cards.Nth(1).RectAsync();
        Assert.AreEqual(first.Top, second.Top, 1, "Side by side on a wide list.");
        Assert.IsTrue(second.Left >= first.Right, "The second card is right of the first.");

        // Below the small breakpoint a card has the whole width.
        await Page.SetViewportSizeAsync(480, 900);
        await Expect(List("srl-grid").Locator("> .bit-aisrl-lst")).ToHaveCSSAsync("grid-template-columns", new Regex(@"^\d+(\.\d+)?px$"));
        first = await cards.Nth(0).RectAsync();
        second = await cards.Nth(1).RectAsync();
        Assert.IsTrue(second.Top >= first.Bottom, "One card to a row on a narrow screen.");
        Assert.AreEqual(first.Left, second.Left, 1);
    }

    [TestMethod]
    public async Task Disabled_TheList_IsDrawnDisabledAndHandsNothingOver()
    {
        await OpenAsync(Route);

        await Expect(List("srl-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(List("srl-disabled").Locator("> .bit-aisrl-hdr")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));

        foreach (var button in await Cards("srl-disabled").Locator("button.bit-crd-lnk").AllAsync())
        {
            await Expect(button).ToBeDisabledAsync();
        }

        await Cards("srl-disabled").Nth(0).ClickAsync(new() { Force = true });
        await NextFramesAsync();
        await Expect(Page.Locator("#srl-clicks")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheList()
    {
        await OpenAsync(Route);

        var list = List("srl-vars");
        await Expect(list).ToHaveCSSAsync("row-gap", "13px");
        await Expect(list.Locator("> .bit-aisrl-lst")).ToHaveCSSAsync("column-gap", "13px");
        await Expect(list.Locator("> .bit-aisrl-hdr")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(list.Locator("> .bit-aisrl-hdr")).ToHaveCSSAsync("font-size", "17px");

        // Columns no narrower than 400px: two fit in 900px.
        var columns = await ComputedStyleAsync("#srl-vars > .bit-aisrl-lst", "grid-template-columns");
        Assert.AreEqual(2, columns.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, columns);
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheListAndItsCards()
    {
        await OpenAsync(Route);

        var list = List("srl-styles");
        await Expect(list.Locator("> .bit-aisrl-hdr")).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(list.Locator("> .bit-aisrl-lst")).ToHaveClassAsync(HasClass("e2e-srl-list"));
        await Expect(list.Locator(".bit-aisrc.e2e-srl-card")).ToHaveCountAsync(3);
        await Expect(list.Locator(".bit-aisrc-ttl").First).ToHaveCSSAsync("font-style", "italic");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrAnInheritedDirection_LaysTheCardsOutFromTheRight()
    {
        await OpenAsync(Route);

        await Expect(List("srl-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "srl-rtl", "srl-rtl-inherited" })
        {
            var first = await Cards(id).Nth(0).RectAsync();
            var second = await Cards(id).Nth(1).RectAsync();
            Assert.IsTrue(first.Left > second.Left, $"{id}: the first card is on the right.");

            // The words of the heading start on the right.
            var words = await List(id).Locator("> .bit-aisrl-hdr").EvaluateAsync<double[]>(
                "e => { const r = document.createRange(); r.selectNodeContents(e); const b = r.getBoundingClientRect(); return [b.left, b.right]; }");
            var list = await List(id).RectAsync();
            Assert.AreEqual(list.Right, words[1], 1.5, $"{id}: the heading starts on the right.");
            Assert.IsTrue(words[0] > list.Left + 20, $"{id}: the heading does not reach the left.");
        }
    }

    // ---------- helpers ----------

    private Task<string> TextTransformAsync(string cssValue)
    {
        return Page.EvaluateAsync<string>(
            """
            value => {
                const probe = document.createElement('div');
                probe.style.textTransform = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).textTransform;
                probe.remove();
                return resolved;
            }
            """, cssValue);
    }
}
