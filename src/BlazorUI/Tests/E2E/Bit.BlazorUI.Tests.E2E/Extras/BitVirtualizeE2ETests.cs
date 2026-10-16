using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitVirtualize at /e2e/virtualize in a real browser: only a window of a million-item list in the DOM while
/// it is scrolled, ItemSize and OverscanCount, the ItemsProvider with its requests, LoadingTemplate and
/// PlaceholderTemplate, Dynamic measuring, Horizontal, Lanes / MinLaneSize / Gap, InitialIndex and every scroll
/// method and alignment with OnVisibleRangeChanged / OnAtStartChanged / OnAtEndChanged, ScrollerSelector, the roles,
/// aria-posinset / setsize, ItemAttributes and the roving keyboard focus, the header / footer / empty / indexed
/// templates, OnEndReached, sticky items, ItemKey, Reversed with OnStartReached and AlignToEnd, the BitParams cascade,
/// Style / Class / Styles / Classes, every --bit-Virtualize-* variable, RTL, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitVirtualizeE2ETests : E2ETestBase
{
    private const string Route = "/e2e/virtualize";

    private ILocator List(string id) => Page.Locator($"#{id}");

    private ILocator Rendered(string id) => Page.Locator($"#{id} > .bit-vir-spc > .bit-vir-blk > .bit-vir-itm");

    private ILocator ItemAt(string id, int index) => Page.Locator($"#{id} .bit-vir-blk > .bit-vir-itm[data-bit-vir-index='{index}']");

    private async Task ShowAsync(string id) => await List(id).ScrollIntoViewIfNeededAsync();

    private Task<int[]> RenderedIndicesAsync(string id)
    {
        return Rendered(id).EvaluateAllAsync<int[]>("els => els.map(e => +e.dataset.bitVirIndex).sort((a, b) => a - b)");
    }

    private Task SetScrollAsync(string id, double offset, bool horizontal = false)
    {
        return List(id).EvaluateAsync(horizontal ? "(e, v) => e.scrollLeft = v" : "(e, v) => e.scrollTop = v", offset);
    }

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_AMillionItems_OnlyTheWindowInViewPlusTheOverscanIsInTheDom()
    {
        await OpenAsync(Route);

        var list = List("vir-basic");
        await Expect(list).ToHaveClassAsync(HasClass("bit-vir"));
        // Fifty million pixels of rows are more than a browser lays out, so the spacer is capped and scaled.
        Assert.AreEqual(10_000_000, await list.EvaluateAsync<double>("e => e.scrollHeight"), 2, "The spacer stands for every item, capped.");

        // 300px of 50px rows: 6 in view, plus 3 after them.
        await Expect(Rendered("vir-basic")).ToHaveCountAsync(9);
        CollectionAssert.AreEqual(Enumerable.Range(0, 9).ToArray(), await RenderedIndicesAsync("vir-basic"));
        await Expect(ItemAt("vir-basic", 0)).ToHaveCSSAsync("height", "50px");
        await Expect(ItemAt("vir-basic", 1)).ToHaveTextAsync("Item 1");
        var a = await ItemAt("vir-basic", 0).BoundingBoxAsync();
        var b = await ItemAt("vir-basic", 1).BoundingBoxAsync();
        Assert.AreEqual(50, b!.Y - a!.Y, 0.5);

        // Scrolled to the middle, the window follows to the middle of the million and stays small.
        await SetScrollAsync("vir-basic", 5_000_000);
        await Expect(ItemAt("vir-basic", 0)).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => [...document.querySelectorAll('#vir-basic .bit-vir-itm')].some(e => Math.abs(+e.dataset.bitVirIndex - 500000) < 20)");
        var indices = await RenderedIndicesAsync("vir-basic");
        Assert.IsTrue(indices.Length <= 13, $"Only a window is rendered, was {indices.Length}.");
        Assert.IsTrue(indices.Min() >= 499_950 && indices.Max() <= 500_050, string.Join(",", indices));
        await Expect(ItemAt("vir-basic", indices[4])).ToHaveTextAsync($"Item {indices[4]}");
        await Expect(ItemAt("vir-basic", indices[4])).ToBeInViewportAsync();

        // The last of the million is reachable.
        await SetScrollAsync("vir-basic", 20_000_000);
        await Expect(ItemAt("vir-basic", 999_999)).ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task Basic_RealWheelScrolling_MovesTheWindow()
    {
        await OpenAsync(Route);

        await List("vir-basic").HoverAsync();
        await Page.Mouse.WheelAsync(0, 1000);
        await Page.WaitForFunctionAsync("e => e.scrollTop >= 900", await List("vir-basic").ElementHandleAsync());
        await Expect(ItemAt("vir-basic", 0)).ToHaveCountAsync(0);
        var indices = await RenderedIndicesAsync("vir-basic");
        Assert.IsTrue(indices.Length is > 0 and <= 13, string.Join(",", indices));
        await Expect(ItemAt("vir-basic", indices[indices.Length / 2])).ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task OverscanCount_RendersThatManyExtraItemsOnEachSide()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-overscan");

        await Expect(Rendered("vir-overscan")).ToHaveCountAsync(16);
        await SetScrollAsync("vir-overscan", 5000);
        await Expect(ItemAt("vir-overscan", 100)).ToBeVisibleAsync();
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('#vir-overscan .bit-vir-itm').length === 26");
        var indices = await RenderedIndicesAsync("vir-overscan");
        Assert.AreEqual(90, indices.Min());
        Assert.AreEqual(115, indices.Max());
    }

    // ---------- provider ----------

    [TestMethod]
    public async Task ItemsProvider_LoadsWindowByWindowAfterItsLoadingTemplate()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-provider");

        await Expect(Page.Locator("#prov-loading")).ToHaveTextAsync("Loading the list");
        await Expect(List("vir-provider")).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(List("vir-provider")).ToHaveAttributeAsync("role", "group");
        await Page.Locator("#btn-release-provider").ClickAsync();

        await Expect(ItemAt("vir-provider", 0)).ToHaveTextAsync("Record 0");
        await Expect(List("vir-provider")).ToHaveAttributeAsync("role", "list");
        await Expect(ItemAt("vir-provider", 0)).ToHaveAttributeAsync("aria-setsize", "10000");
        await Expect(List("vir-provider").Locator("> .bit-vir-spc")).ToHaveCSSAsync("height", "400000px");
        await Expect(Page.Locator("#prov-requests")).ToHaveTextAsync(new Regex(@"^0:\d+"));

        await SetScrollAsync("vir-provider", 200_000);
        await Expect(ItemAt("vir-provider", 5000)).ToHaveTextAsync("Record 5000");
        var requests = await Page.Locator("#prov-requests").TextContentAsync();
        StringAssert.Matches(requests!, new Regex(@"\|4\d{3}:\d+"));
        await Expect(List("vir-provider")).Not.ToHaveAttributeAsync("aria-busy", "true");
    }

    [TestMethod]
    public async Task PlaceholderTemplate_FillsEverySlotStillLoading()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-provider-ph");

        await Expect(ItemAt("vir-provider-ph", 0)).ToHaveTextAsync("Gated 0");
        await SetScrollAsync("vir-provider-ph", 40 * 2000);

        var slot = ItemAt("vir-provider-ph", 2000);
        await Expect(slot.Locator(".ph-only")).ToHaveTextAsync("Placeholder 2000 (40)");
        await Expect(slot).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(List("vir-provider-ph")).ToHaveAttributeAsync("aria-busy", "true");

        await Page.Locator("#btn-release-gated").ClickAsync();
        await Expect(slot).ToHaveTextAsync("Gated 2000");
        await Expect(slot).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
    }

    // ---------- dynamic ----------

    [TestMethod]
    public async Task Dynamic_EveryRenderedItemIsMeasuredAndPlacedByItsRealSize()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-dynamic");

        await Expect(ItemAt("vir-dynamic", 0)).ToHaveTextAsync("Post 0");
        await Expect(ItemAt("vir-dynamic", 0)).Not.ToHaveClassAsync(HasClass("bit-vir-fix"));

        // The posts are 30, 60, 90 and 120 px tall: each starts where the one before it ends.
        await Page.WaitForFunctionAsync(
            """
            () => {
                const items = [0, 1, 2, 3].map(i => document.querySelector(`#vir-dynamic .bit-vir-itm[data-bit-vir-index='${i}']`));
                if (items.some(i => !i)) return false;
                const tops = items.map(i => i.getBoundingClientRect().top);
                return Math.abs(tops[1] - tops[0] - 30) < 1 && Math.abs(tops[2] - tops[1] - 60) < 1 && Math.abs(tops[3] - tops[2] - 90) < 1;
            }
            """);

        await SetScrollAsync("vir-dynamic", 6000);
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('#vir-dynamic .bit-vir-itm').length > 0 && !document.querySelector(\"#vir-dynamic .bit-vir-itm[data-bit-vir-index='0']\")");
    }

    // ---------- horizontal ----------

    [TestMethod]
    public async Task Horizontal_TheItemsSitInARowAndScrollSideways()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-horizontal");

        var list = List("vir-horizontal");
        await Expect(list).ToHaveClassAsync(HasClass("bit-vir-hor"));
        await Expect(list.Locator("> .bit-vir-spc")).ToHaveCSSAsync("width", "100000px");
        await Expect(ItemAt("vir-horizontal", 0)).ToHaveCSSAsync("width", "100px");
        var a = await ItemAt("vir-horizontal", 0).BoundingBoxAsync();
        var b = await ItemAt("vir-horizontal", 1).BoundingBoxAsync();
        Assert.AreEqual(100, b!.X - a!.X, 0.5);
        Assert.AreEqual(a.Y, b.Y, 0.5);

        await SetScrollAsync("vir-horizontal", 50_000, horizontal: true);
        await Expect(ItemAt("vir-horizontal", 500)).ToBeVisibleAsync();
        await Expect(ItemAt("vir-horizontal", 0)).ToHaveCountAsync(0);
    }

    // ---------- grid ----------

    [TestMethod]
    public async Task Lanes_LayTheItemsOutInAGridWithTheGap()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-grid");

        var a = await ItemAt("vir-grid", 0).BoundingBoxAsync();
        var b = await ItemAt("vir-grid", 1).BoundingBoxAsync();
        var c = await ItemAt("vir-grid", 2).BoundingBoxAsync();
        var d = await ItemAt("vir-grid", 3).BoundingBoxAsync();
        Assert.AreEqual(a!.Y, b!.Y, 0.5);
        Assert.AreEqual(a.Y, c!.Y, 0.5);
        Assert.AreEqual(a.X, d!.X, 0.5, "The fourth item starts the second row.");
        Assert.AreEqual(68, d.Y - a.Y, 0.5, "A row is the item size and the gap.");
        Assert.AreEqual(8, b.X - (a.X + a.Width), 1, "The gap separates the lanes.");
        Assert.AreEqual(a.Width, b.Width, 0.5);

        await Page.Locator("#btn-lanes").ClickAsync();
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector(\"#vir-grid .bit-vir-itm[data-bit-vir-index='3']\"); const f = document.querySelector(\"#vir-grid .bit-vir-itm[data-bit-vir-index='0']\"); return i && f && Math.abs(i.getBoundingClientRect().top - f.getBoundingClientRect().top) < 1; }");

        // The arrows move across the lanes and along them.
        await ItemAt("vir-grid", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt("vir-grid", 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemAt("vir-grid", 5)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task MinLaneSize_FitsAsManyLanesAsTheWidthAllows()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-grid-min");

        // 600px (less the border) at 190px a lane: three lanes.
        await Page.WaitForFunctionAsync(
            """
            () => {
                const at = i => document.querySelector(`#vir-grid-min .bit-vir-itm[data-bit-vir-index='${i}']`)?.getBoundingClientRect();
                const a = at(0), c = at(2), d = at(3);
                return a && c && d && Math.abs(a.top - c.top) < 1 && d.top > a.top;
            }
            """);
    }

    // ---------- scrolling ----------

    [TestMethod]
    public async Task InitialIndex_OpensTheListAtThatItem()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-scroll");

        await Expect(ItemAt("vir-scroll", 200)).ToBeVisibleAsync();
        Assert.AreEqual(200 * 48, await List("vir-scroll").EvaluateAsync<double>("e => e.scrollTop"), 1);
        await Expect(Page.Locator("#vir-range")).ToHaveTextAsync(new Regex(@"^200-20[67]$"));
        await Expect(Page.Locator("#vir-at-start")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#vir-at-end")).ToHaveTextAsync("False");
    }

    [DataTestMethod]
    [DataRow("#btn-to-500-start", 500 * 48)]
    [DataRow("#btn-to-500-center", 500 * 48 + 24 - 150)]
    [DataRow("#btn-to-500-end", 501 * 48 - 300)]
    [DataRow("#btn-to-offset", 4800)]
    public async Task ScrollToIndexAndOffset_PlaceTheTargetAsAligned(string button, int offset)
    {
        await OpenAsync(Route);
        await ShowAsync("vir-scroll");

        await Page.Locator(button).ClickAsync();
        await Page.WaitForFunctionAsync("([e, v]) => Math.abs(e.scrollTop - v) <= 2", new object[] { await List("vir-scroll").ElementHandleAsync(), offset });
        if (button.Contains("500")) await Expect(ItemAt("vir-scroll", 500)).ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task ScrollToIndexNearest_MovesAsLittleAsItCan()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-scroll");

        await Page.Locator("#btn-to-500-start").ClickAsync();
        await Page.WaitForFunctionAsync("e => Math.abs(e.scrollTop - 24000) <= 2", await List("vir-scroll").ElementHandleAsync());

        // 503 is already in view: nothing moves.
        await Page.Locator("#btn-to-503-nearest").ClickAsync();
        await NextFramesAsync();
        Assert.AreEqual(24000, await List("vir-scroll").EvaluateAsync<double>("e => e.scrollTop"), 2);
    }

    [TestMethod]
    public async Task ScrollToStartEndAndBy_ReachTheEdgesAndReportThem()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-scroll");

        await Page.Locator("#btn-to-end").ClickAsync();
        await Expect(ItemAt("vir-scroll", 999)).ToBeInViewportAsync();
        await Expect(Page.Locator("#vir-at-end")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#vir-range")).ToHaveTextAsync(new Regex(@"-1000$"));

        await Page.Locator("#btn-to-start").ClickAsync();
        await Expect(ItemAt("vir-scroll", 0)).ToBeInViewportAsync();
        await Expect(Page.Locator("#vir-at-start")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#vir-at-end")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#vir-range")).ToHaveTextAsync(new Regex("^0-"));

        await Page.Locator("#btn-by-plus").ClickAsync();
        await Page.WaitForFunctionAsync("e => Math.abs(e.scrollTop - 480) <= 2", await List("vir-scroll").ElementHandleAsync());
        await Expect(Page.Locator("#vir-range")).ToHaveTextAsync(new Regex("^10-"));
        await Expect(Page.Locator("#vir-at-start")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task ScrollSmooth_AnimatesToTheTarget()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-scroll");

        await Page.Locator("#btn-smooth").ClickAsync();
        await Page.WaitForFunctionAsync("e => Math.abs(e.scrollTop - 900 * 48) <= 2", await List("vir-scroll").ElementHandleAsync());
        await Expect(ItemAt("vir-scroll", 900)).ToBeInViewportAsync();

        await Page.Locator("#btn-refresh").ClickAsync();
        await Expect(ItemAt("vir-scroll", 900)).ToBeInViewportAsync();
    }

    // ---------- scroller ----------

    [TestMethod]
    public async Task ScrollerSelector_AnAncestorScrollsAndTheListStillVirtualizes()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-scroller");

        var list = List("vir-scroller");
        await Expect(list).ToHaveClassAsync(HasClass("bit-vir-ext"));
        Assert.AreEqual(50_000, (await list.BoundingBoxAsync())!.Height, 2, "The list grows to the full size of its items.");
        var count = await Rendered("vir-scroller").CountAsync();
        Assert.IsTrue(count < 15, $"Only a window is rendered, was {count}.");

        await Page.Locator("#vir-outer").EvaluateAsync("e => e.scrollTop = 100 + 300 * 50");
        await Expect(ItemAt("vir-scroller", 300)).ToBeVisibleAsync();
        await Expect(ItemAt("vir-scroller", 300)).ToBeInViewportAsync();
        await Expect(ItemAt("vir-scroller", 0)).ToHaveCountAsync(0);
    }

    // ---------- keyboard & accessibility ----------

    [TestMethod]
    public async Task Accessibility_RolesPositionsAndItemAttributes()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-feed");

        var feed = List("vir-feed");
        await Expect(feed).ToHaveAttributeAsync("role", "feed");
        await Expect(feed).ToHaveAttributeAsync("aria-label", "News feed");
        var first = ItemAt("vir-feed", 0);
        await Expect(first).ToHaveAttributeAsync("role", "article");
        await Expect(first).ToHaveAttributeAsync("aria-posinset", "1");
        await Expect(first).ToHaveAttributeAsync("aria-setsize", "1000");
        await Expect(first).ToHaveAttributeAsync("aria-label", "Article 0");
        await Expect(first).ToHaveAttributeAsync("data-article", "0");
        await Expect(first).ToHaveClassAsync(HasClass("art-class"));
        await Expect(first).ToHaveClassAsync(HasClass("bit-vir-itm"));
        await Expect(Page.GetByRole(AriaRole.Article, new() { Name = "Article 3" })).ToHaveCountAsync(1);

        await Expect(List("vir-default-roles")).ToHaveAttributeAsync("role", "list");
        await Expect(ItemAt("vir-default-roles", 0)).ToHaveAttributeAsync("role", "listitem");
    }

    [TestMethod]
    public async Task Keyboard_ArrowsPageKeysHomeAndEnd_MoveARovingFocus()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-feed");

        // The list is the tab stop until an item is active.
        var feed = List("vir-feed");
        await Expect(feed).ToHaveAttributeAsync("tabindex", "0");
        await Page.Locator("#before-feed").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(feed).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemAt("vir-feed", 0)).ToBeFocusedAsync();
        await Expect(ItemAt("vir-feed", 0)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(feed).ToHaveAttributeAsync("tabindex", "-1");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemAt("vir-feed", 1)).ToBeFocusedAsync();
        await Expect(ItemAt("vir-feed", 0)).ToHaveAttributeAsync("tabindex", "-1");

        await Page.Keyboard.PressAsync("PageDown");
        await Expect(ItemAt("vir-feed", 6)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(ItemAt("vir-feed", 1)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(ItemAt("vir-feed", 999)).ToBeFocusedAsync();
        await Expect(ItemAt("vir-feed", 999)).ToBeInViewportAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(ItemAt("vir-feed", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(ItemAt("vir-feed", 0)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_TheFocusedItem_StaysFocusedWhenScrolledAwayAndKeysInItsControlsAreLeftAlone()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-feed");

        await ItemAt("vir-feed", 2).ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemAt("vir-feed", 3)).ToBeFocusedAsync();

        await SetScrollAsync("vir-feed", 30_000);
        await Expect(ItemAt("vir-feed", 600)).ToBeVisibleAsync();
        await Expect(ItemAt("vir-feed", 3)).ToBeFocusedAsync();

        var input = ItemAt("vir-feed", 600).Locator(".art-input");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("End");
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_TheFocusRing_IsDrawnInsideTheItem()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-vars");

        await List("vir-vars").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemAt("vir-vars", 0)).ToBeFocusedAsync();
        var ring = await ComputedStyleAsync("#vir-vars .bit-vir-itm:focus", "outline-color") + await ComputedStyleAsync("#vir-vars .bit-vir-itm:focus", "box-shadow");
        StringAssert.Contains(ring, "rgb(0, 200, 0)");
    }

    // ---------- templates ----------

    [TestMethod]
    public async Task Templates_HeaderFooterIndexedAndEmpty()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-templates");

        await Expect(List("vir-templates").Locator("> .bit-vir-hdr .tpl-header")).ToHaveTextAsync("Header · 1000 items");
        await Expect(ItemAt("vir-templates", 0)).ToHaveTextAsync("1. Item 0");
        await Expect(ItemAt("vir-templates", 1).Locator(".tpl-row")).ToHaveClassAsync(HasClass("odd"));
        await Expect(List("vir-templates").Locator("> .bit-vir-ftr .tpl-footer")).ToHaveTextAsync("Footer · end of the list");

        // The header scrolls with the items, and the footer is reached at the end.
        await SetScrollAsync("vir-templates", 10_000_000);
        await Expect(List("vir-templates").Locator(".tpl-footer")).ToBeInViewportAsync();
        await Expect(List("vir-templates").Locator(".tpl-header")).Not.ToBeInViewportAsync();

        await Page.Locator("#btn-empty").ClickAsync();
        await Expect(List("vir-templates").Locator("> .bit-vir-emp .tpl-empty")).ToHaveTextAsync("No items to show");
        await Expect(Rendered("vir-templates")).ToHaveCountAsync(0);
        await Expect(List("vir-templates")).ToHaveAttributeAsync("role", "group");

        await Page.Locator("#btn-refill").ClickAsync();
        await Expect(List("vir-templates")).ToHaveAttributeAsync("role", "list");
        await Expect(Rendered("vir-templates").First).ToBeVisibleAsync();
    }

    // ---------- infinite ----------

    [TestMethod]
    public async Task OnEndReached_FiresNearTheEndOncePerCount()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-infinite");

        await Expect(Page.Locator("#vir-log")).ToHaveTextAsync("");
        await SetScrollAsync("vir-infinite", 30 * 50);
        await Expect(Page.Locator("#feed-count")).ToHaveTextAsync("60");
        await Expect(Page.Locator("#vir-log")).ToHaveTextAsync("end-reached:30");
        await Expect(ItemAt("vir-infinite", 29)).ToHaveTextAsync("Feed 29");
        await Expect(ItemAt("vir-infinite", 31)).ToHaveTextAsync("Feed 31");

        await SetScrollAsync("vir-infinite", 60 * 50);
        await Expect(Page.Locator("#feed-count")).ToHaveTextAsync("90");
        await Expect(Page.Locator("#vir-log")).ToHaveTextAsync("end-reached:30,end-reached:60");
    }

    // ---------- sticky ----------

    [TestMethod]
    public async Task Sticky_TheCurrentGroupHeaderIsPinnedThroughItsStickyTemplate()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-sticky");

        // 13 rows a group of 40px: scrolled into the middle of C's group.
        await SetScrollAsync("vir-sticky", 26 * 40 + 200);
        var pinned = List("vir-sticky").Locator(".bit-vir-stk");
        await Expect(pinned).ToHaveTextAsync("Pinned C");
        await Expect(pinned).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(pinned).ToHaveCSSAsync("position", "sticky");
        var listBox = await List("vir-sticky").BoundingBoxAsync();
        var pinnedBox = await pinned.BoundingBoxAsync();
        Assert.AreEqual(listBox!.Y + 1, pinnedBox!.Y, 1.5, "The pinned header sits at the top of the viewport.");

        await SetScrollAsync("vir-sticky", 52 * 40 + 200);
        await Expect(pinned).ToHaveTextAsync("Pinned E");
    }

    // ---------- item key ----------

    [TestMethod]
    public async Task ItemKey_KeepsEachRowsStateWithItsItemAcrossAnInsertion()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-keyed");

        await ItemAt("vir-keyed", 1).Locator(".task-check").CheckAsync();
        await ItemAt("vir-unkeyed", 1).Locator(".task-check").CheckAsync();

        await Page.Locator("#btn-add-tasks").ClickAsync();
        await Expect(ItemAt("vir-keyed", 0)).ToHaveTextAsync("Task -5");

        // Keyed: the tick stays on Task 1, now the 7th row. Unkeyed: it stays on the 2nd row, another task now.
        await SetScrollAsync("vir-keyed", 0);
        var keyedChecked = await Page.Locator("#vir-keyed .bit-vir-itm").Filter(new() { HasText = "Task 1" }).First.Locator(".task-check").IsCheckedAsync();
        Assert.IsTrue(keyedChecked);
        Assert.IsFalse(await ItemAt("vir-keyed", 1).Locator(".task-check").IsCheckedAsync());
        Assert.IsTrue(await ItemAt("vir-unkeyed", 1).Locator(".task-check").IsCheckedAsync());
        await Expect(ItemAt("vir-unkeyed", 1)).ToHaveTextAsync("Task -4");
    }

    // ---------- chat ----------

    [TestMethod]
    public async Task Reversed_StartsAtTheNewestAndStaysPinnedAsMessagesArrive()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-chat");

        await Expect(Page.Locator("#vir-chat .msg").Filter(new() { HasText = "Message 129" })).ToBeInViewportAsync();
        await Expect(Page.Locator("#chat-at-end")).ToHaveTextAsync("True");

        await Page.Locator("#btn-send").ClickAsync();
        await Expect(Page.Locator("#vir-chat .msg").Filter(new() { HasText = "Message 130" })).ToBeInViewportAsync();

        // Scrolled up to the start, older history is prepended without moving the view.
        await SetScrollAsync("vir-chat", 0);
        await Expect(Page.Locator("#vir-log")).ToHaveTextAsync(new Regex("start-reached:100"));
        await Expect(Page.Locator("#vir-chat .msg").Filter(new() { HasText = "Message 100" })).ToBeInViewportAsync();
        await Expect(Page.Locator("#chat-at-end")).ToHaveTextAsync("False");
        await Page.WaitForFunctionAsync("e => e.scrollTop > 100", await List("vir-chat").ElementHandleAsync());

        await SetScrollAsync("vir-chat", 0);
        await Expect(Page.Locator("#vir-log")).ToHaveTextAsync(new Regex("start-reached:80"));
        await Expect(Page.Locator("#vir-chat .msg").Filter(new() { HasText = "Message 80" })).ToBeInViewportAsync();

        await Page.Locator("#btn-jump").ClickAsync();
        await Expect(Page.Locator("#chat-at-end")).ToHaveTextAsync("True");
        await Expect(Page.Locator("#vir-chat .msg").Filter(new() { HasText = "Message 130" })).ToBeInViewportAsync();
    }

    [TestMethod]
    public async Task AlignToEnd_AShortConversationSitsAtTheBottom()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-short");

        await Expect(List("vir-short")).ToHaveClassAsync(HasClass("bit-vir-ate"));
        var list = await List("vir-short").BoundingBoxAsync();
        var last = await Page.Locator("#vir-short .short-msg").Last.BoundingBoxAsync();
        Assert.AreEqual(list!.Y + list.Height, last!.Y + last.Height, 3, "The last message touches the bottom.");
    }

    // ---------- cascading ----------

    [TestMethod]
    public async Task Cascading_BitParams_SetTheDefaultsAndTheListsOwnWin()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-cas");

        await Expect(List("vir-cas")).ToHaveAttributeAsync("role", "grid");
        await Expect(ItemAt("vir-cas", 0)).ToHaveAttributeAsync("role", "row");
        await Expect(ItemAt("vir-cas", 0)).ToHaveCSSAsync("height", "30px");
        await Expect(Rendered("vir-cas")).ToHaveCountAsync(11);
        await Expect(ItemAt("vir-cas-own", 0)).ToHaveCSSAsync("height", "64px");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task StylesAndClasses_EveryPart_TakesItsOwn()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-styled");

        var list = List("vir-styled");
        await Expect(list).ToHaveClassAsync(HasClass("vir-root-class"));
        await Expect(list).ToHaveClassAsync(HasClass("c-root"));
        await Expect(list).ToHaveCSSAsync("height", "240px");
        await Expect(list).ToHaveCSSAsync("border-top-color", "rgb(30, 144, 255)");
        await Expect(list).ToHaveCSSAsync("padding-top", "1px");
        await Expect(list.Locator("> .bit-vir-hdr")).ToHaveClassAsync(HasClass("c-header"));
        await Expect(list.Locator("> .bit-vir-hdr")).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(ItemAt("vir-styled", 1)).ToHaveClassAsync(HasClass("c-item"));
        await Expect(ItemAt("vir-styled", 1)).ToHaveCSSAsync("padding-left", "7px");

        await SetScrollAsync("vir-styled", 48 * 3 + 30);
        var sticky = list.Locator(".bit-vir-stk");
        await Expect(sticky).ToHaveClassAsync(HasClass("c-sticky"));
        await Expect(sticky).ToHaveCSSAsync("color", "rgb(0, 0, 255)");

        await SetScrollAsync("vir-styled", 10_000_000);
        await Expect(list.Locator("> .bit-vir-ftr")).ToHaveClassAsync(HasClass("c-footer"));
        await Expect(list.Locator("> .bit-vir-ftr")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");

        await ShowAsync("vir-styled-empty");
        await Expect(List("vir-styled-empty").Locator("> .bit-vir-emp")).ToHaveClassAsync(HasClass("c-empty"));
        await Expect(List("vir-styled-empty").Locator("> .bit-vir-emp")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(List("vir-styled-loading").Locator("> .bit-vir-ldg")).ToHaveClassAsync(HasClass("c-loading"));
        await Expect(List("vir-styled-loading").Locator("> .bit-vir-ldg")).ToHaveCSSAsync("letter-spacing", "3px");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheStickyItemAndTheStates()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-vars");

        await SetScrollAsync("vir-vars", 48 * 3 + 30);
        var sticky = List("vir-vars").Locator(".bit-vir-stk");
        await Expect(sticky).ToHaveCSSAsync("background-color", "rgb(250, 250, 0)");
        await Expect(sticky).ToHaveCSSAsync("box-shadow", "rgb(1, 1, 1) 0px 2px 4px 0px");

        var empty = List("vir-vars-empty").Locator("> .bit-vir-emp");
        await Expect(empty).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(empty).ToHaveCSSAsync("font-size", "19px");
        await Expect(empty).ToHaveCSSAsync("padding-top", "23px");
    }

    // ---------- RTL ----------

    [TestMethod]
    public async Task Rtl_AHorizontalList_LaysItsItemsOutFromTheRight()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-rtl");

        await Expect(List("vir-rtl")).ToHaveAttributeAsync("dir", "rtl");
        var list = await List("vir-rtl").BoundingBoxAsync();
        var a = await ItemAt("vir-rtl", 0).BoundingBoxAsync();
        var b = await ItemAt("vir-rtl", 1).BoundingBoxAsync();
        Assert.IsTrue(b!.X < a!.X, "The second item sits to the left of the first.");
        Assert.AreEqual(list!.X + list.Width - 1, a.X + a.Width, 2, "The first item starts at the right edge.");

        await SetScrollAsync("vir-rtl", -50_000, horizontal: true);
        await Expect(ItemAt("vir-rtl", 500)).ToBeVisibleAsync();
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_ThePagesHtml_ContainsTheFirstWindow()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "<div id=\"vir-basic\" role=\"list\" class=\"bit-vir vir-box\" tabindex=\"0\">");
        StringAssert.Contains(html, "aria-setsize=\"1000000\" aria-posinset=\"1\"");
        Assert.IsFalse(html.Contains(">Item 999<"), "Only a window is prerendered.");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Unmount_WhileAWindowIsInFlight_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);
        await ShowAsync("vir-mounted");

        await Expect(List("vir-mounted")).ToHaveAttributeAsync("aria-busy", "true");
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(List("vir-mounted")).ToHaveCountAsync(0);
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(List("vir-mounted")).ToHaveCountAsync(1);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
